using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiNET.Net;
using MiNET.Utils;
using MiNET.Worlds;
using SkyCore.Entities;
using SkyCore.Player;
using StackExchange.Redis;

namespace SkyCore.Game
{
	public class ExternalGameHandler
	{

		public static readonly ConcurrentDictionary<string, GamePool> GameRegistrations = new ConcurrentDictionary<string, GamePool>();

		private static readonly ConnectionMultiplexer RedisPool;

		public static int TotalPlayers { get; private set; }

		public static string CurrentHostAddress { get; set; }

		static ExternalGameHandler()
		{
			RedisPool = ConnectionMultiplexer.Connect("localhost");

			RedisPool.PreserveAsyncOrder = false; //Allow Concurrency
		}

		public static void Init()
		{
			SkyCoreAPI.Instance.Context.Server.MotdProvider = new SkyMotdProvider();
			new Thread(delegate ()
			{

				while (!SkyCoreAPI.IsDisabled)
				{
					Thread.Sleep(1000); // Update every 1 second

					int totalPlayers = 0;
					foreach (GamePool gamePool in GameRegistrations.Values)
					{
						totalPlayers += gamePool.GetCurrentPlayers();
					}

					TotalPlayers = totalPlayers;
				}

			}).Start();

			RedisPool.GetSubscriber().SubscribeAsync("game_register", (channel, message) =>
			{
				/*
				 * Channel game_register
				 * Format:
				 * {sending-server}:{game-name}:{ip-address}:{port}
				 */

				string[] messageSplit = ((string) message).Split(':');

				if (!ushort.TryParse(messageSplit[3], out var connectingPort))
				{
					SkyUtil.log($"Invalid format received as '{message}'");
					return;
				}

				string ipAddress = messageSplit[2];
				if ((ipAddress + connectingPort).Equals(SkyCoreAPI.Instance.CurrentIp))
				{
					SkyUtil.log("Avoiding registering self (New Game Registration)");
					return;
				}

				//TODO: Game Instance Pooling
				if (GameRegistrations.ContainsKey(messageSplit[0]))
				{
					return;
				}

				SkyUtil.log($"Registering {messageSplit[0]} from {ipAddress}:{connectingPort}");

				RegisterExternalGame(messageSplit[2], connectingPort, messageSplit[0], messageSplit[1]);
			});
		}

		//

		public static void RegisterExternalGame(string connectingAddress, ushort connectingPort, string targetServer, string gameName)
		{
			InstanceInfo instanceInfo = new InstanceInfo()
			{
				CurrentPlayers = 0,
				HostAddress = connectingAddress,
				HostPort = connectingPort
			};
			RegisterGame(gameName, instanceInfo);

			if (!gameName.Equals("hub"))
			{
				SkyCoreAPI.Instance.AddPendingTask(() =>
				{
					Level level = SkyCoreAPI.Instance.GetHubLevel();

					string neatName = gameName;
					PlayerLocation npcLocation = new PlayerLocation(0.5D, 30D, 16.5D, 180F, 180F, 0F);

					switch (gameName)
					{
						case "murder":
						{
							neatName = "§c§lMurder Mystery";
							npcLocation = new PlayerLocation(-1.5D, 30D, 16.5D, 180F, 180F, 0F);
							break;
						}
						case "build-battle":
						{
							neatName = "§e§lBuild Battle";
							npcLocation = new PlayerLocation(2.5D, 30D, 16.5D, 180F, 180F, 0F);
							break;
						}
					}

					PlayerNPC.SpawnNPC(level, $"§e§l{neatName}", npcLocation, $"GID:{gameName}");
				});
			}

			ISubscriber subscriber = RedisPool.GetSubscriber();

			/*
			 * Channel <game>_info
			 * Format:
			 * {current-players}:{available-servers}
			 */
			subscriber.SubscribeAsync($"{gameName}_info", (channel, message) =>
			{
				//SkyUtil.log($"Received update for {channel} > {message}");
				string[] messageSplit = ((string)message).Split(':');

				int.TryParse(messageSplit[0], out var instancePlayers);

				instanceInfo.CurrentPlayers = instancePlayers;

				List<GameInfo> availableGames = new List<GameInfo>();
				int i = 0;
				while (++i < messageSplit.Length)
				{
					string[] gameInfoSplit = messageSplit[i].Split(';');

					int.TryParse(gameInfoSplit[1], out var currentPlayers);
					int.TryParse(gameInfoSplit[2], out var maxPlayers);

					availableGames.Add(new GameInfo(gameInfoSplit[0], currentPlayers, maxPlayers));
				}

				instanceInfo.AvailableGames = availableGames;
			});
		}

		public static void RegisterInternalGame(string gameName)
		{
			new Thread(delegate()
			{

				int threadTick = -1;

				while (!SkyCoreAPI.IsDisabled)
				{
					//Enforce game registration every 15 seconds
					if (++threadTick % 15 == 0)
					{
						//Temp - Sending server is gameName
						RedisPool.GetSubscriber().PublishAsync("game_register",
							$"{gameName}:{gameName}:{Config.GetProperty("ip", SkyCoreAPI.Instance.CurrentIp)}:{Config.GetProperty("port", "19132")}");
					}

					Thread.Sleep(1000); // Update every 1 second

					InstanceInfo instanceInfo = GameRegistrations[gameName].GetLocalInstance();

					string availableGameConcat = "";
					foreach (GameInfo gameInfo in instanceInfo.AvailableGames)
					{
						availableGameConcat += gameInfo.GameId + "," + gameInfo.CurrentPlayers + "," + gameInfo.MaxPlayers + ":";
					}

					//SkyUtil.log($"Sending update on {gameName}_info as " + instanceInfo.CurrentPlayers + ":" + instanceInfo.AvailableGames);
					RedisPool.GetSubscriber().PublishAsync($"{gameName}_info", instanceInfo.CurrentPlayers + ":" + availableGameConcat);
				}

			}).Start();

			RegisterGame(gameName, new InstanceInfo{HostAddress = "local"});

			RedisPool.GetSubscriber().SubscribeAsync($"{gameName}_join", (channel, message) =>
			{
				//TODO:
			});
		}

		public static void AddPlayer(SkyPlayer player, string gameName)
		{
			if (!GameRegistrations.ContainsKey(gameName))
			{
				player.SendMessage($"{ChatColors.Red}No game existed for the name '{gameName}'");
				return;
			}

			if (gameName.Equals("hub"))
			{
				SkyCoreAPI.Instance.GameModes[gameName].QueuePlayer(player);
				return;
			}

			//player.SendMessage($"§e§l(!) §r§eJoining {gameName}...");

			RequeuePlayer(player, gameName);
		}

		public static void RegisterGame(string gameName, InstanceInfo instanceInfo)
		{
			GamePool gamePool;
			if (GameRegistrations.ContainsKey(gameName))
			{
				gamePool = GameRegistrations[gameName];
			}
			else
			{
				GameRegistrations.TryAdd(gameName, gamePool = new GamePool(gameName));
			}

			gamePool.AddInstance(instanceInfo);
		}

		public static void RequeuePlayer(SkyPlayer player, string gameName)
		{
			if (!GameRegistrations.ContainsKey(gameName))
			{
				player.SendMessage($"{ChatColors.Red}No game existed for the name '{gameName}'");
				return;
			}

			GameRegistrations[gameName].AddPlayer(player);
		}

	}

	public class GamePool
	{
		
		public string GameName { get; }

		public PlayerLocation NpcLocation { get; set; }

		//

		private readonly ConcurrentDictionary<string, InstanceInfo> _gameInstances = new ConcurrentDictionary<string, InstanceInfo>();

		public GamePool(string gameName)
		{
			GameName = gameName;
		}

		public int GetCurrentPlayers()
		{
			int playerCount = 0;
			foreach (InstanceInfo instanceInfo in _gameInstances.Values)
			{
				playerCount += instanceInfo.CurrentPlayers;
			}

			return playerCount;
		}

		public InstanceInfo GetLocalInstance()
		{
			foreach (InstanceInfo instanceInfo in _gameInstances.Values)
			{
				if (instanceInfo.HostAddress.Equals("local"))
				{
					return instanceInfo;
				}
			}

			return null;
		}

		public void AddPlayer(SkyPlayer player)
		{
			InstanceInfo bestGameInstance = null;
			GameInfo bestAvailableGame = null;

			//Check current instance first
			if (_gameInstances.ContainsKey(ExternalGameHandler.CurrentHostAddress))
			{
				InstanceInfo instanceInfo = _gameInstances[ExternalGameHandler.CurrentHostAddress];
				foreach (GameInfo gameInfo in instanceInfo.AvailableGames)
				{
					if (bestAvailableGame == null || gameInfo.CurrentPlayers > bestAvailableGame.CurrentPlayers)
					{
						bestGameInstance = instanceInfo;
						bestAvailableGame = gameInfo;
					}
				}
			}

			if (bestAvailableGame == null)
			{
				foreach (InstanceInfo instanceInfo in _gameInstances.Values)
				{
					foreach (GameInfo gameInfo in instanceInfo.AvailableGames)
					{
						if (bestAvailableGame == null || gameInfo.CurrentPlayers > bestAvailableGame.CurrentPlayers)
						{
							bestGameInstance = instanceInfo;
							bestAvailableGame = gameInfo;
						}
					}
				}
			}

			if (bestAvailableGame == null)
			{
				player.SendMessage($"§cWe were unable to move you to another game of {GameName}.");
			}
			else
			{
				if (bestGameInstance.HostAddress.Equals("local"))
				{
					//TODO: Target specific games
					SkyCoreAPI.Instance.GameModes[GameName].InstantQueuePlayer(player);
				}
				else
				{
					//TODO: Target specific games
					McpeTransfer transferPacket = new McpeTransfer
					{
						serverAddress = bestGameInstance.HostAddress,
						port = bestGameInstance.HostPort
					};

					player.SendPackage(transferPacket);
				}
			}
		}

		public List<InstanceInfo> GetAllGames()
		{
			List<InstanceInfo> games = new List<InstanceInfo>();
			games.AddRange(_gameInstances.Values);
			return games;
		}

		public void AddInstance(InstanceInfo instanceInfo)
		{
			string instanceKey = instanceInfo.HostAddress + ":" + instanceInfo.HostPort;

			if (!_gameInstances.ContainsKey(instanceKey))
			{
				_gameInstances.TryAdd(instanceKey, instanceInfo);
			}
			else
			{
				_gameInstances[instanceKey] = instanceInfo; //Update
			}
		}

		public void UpdateInstance(string instanceAddress, ushort instancePort, int currentPlayers, List<GameInfo> availableGames)
		{
			string instanceKey = instanceAddress + ":" + instancePort;

			InstanceInfo instanceInfo;
			if (_gameInstances.ContainsKey(instanceKey))
			{
				instanceInfo = _gameInstances[instanceKey];
			}
			else
			{
				instanceInfo = new InstanceInfo {HostAddress = instanceAddress, HostPort = instancePort};
				_gameInstances.TryAdd(instanceKey, instanceInfo);
			}

			instanceInfo.Update();

			instanceInfo.CurrentPlayers = currentPlayers;
			instanceInfo.AvailableGames = availableGames;
		}

	}

	public class InstanceInfo
	{

		public string HostAddress { get; set; }
		public ushort HostPort { get; set; }

		public int CurrentPlayers { get; set; }

		public long LastUpdate { get; private set; }

		public List<GameInfo> AvailableGames = new List<GameInfo>();

		public void Update()
		{
			LastUpdate = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		}

	}

	public class GameInfo
	{

		public string GameId { get; }

		public int CurrentPlayers { get; set; }

		public int MaxPlayers { get; set; }

		public GameInfo(string gameId, int currentPlayers, int maxPlayers)
		{
			GameId = gameId;
			CurrentPlayers = currentPlayers;
			MaxPlayers = maxPlayers;
		}

	}

}
