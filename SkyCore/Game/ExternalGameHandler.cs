using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using MiNET;
using MiNET.Net;
using MiNET.Utils;
using SkyCore.Entities;
using SkyCore.Game.Level;
using SkyCore.Player;
using SkyCore.Util;
using StackExchange.Redis;

namespace SkyCore.Game
{
	public class ExternalGameHandler
	{

		public static readonly ConcurrentDictionary<string, GamePool> GameRegistrations = new ConcurrentDictionary<string, GamePool>();

		internal static readonly ConnectionMultiplexer RedisPool;

		public static int TotalPlayers { get; private set; }

		public static string CurrentHostAddress { get; set; }

		static ExternalGameHandler()
		{
			RedisPool = ConnectionMultiplexer.Connect("localhost");

			RedisPool.PreserveAsyncOrder = false; //Allow Concurrency
		}

		public static void Init(MiNetServer server)
		{
			server.MotdProvider = new SkyMotdProvider();
			new Thread(() =>
			{
				while (!SkyCoreAPI.IsDisabled)
				{
					Thread.Sleep(1000); // Update every 1 second

					int totalPlayers = 0;
					long expiryTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() - 15000; //15 Seconds
					foreach (GamePool gamePool in GameRegistrations.Values)
					{
						List<InstanceInfo> toRemoveInstances = new List<InstanceInfo>();

						foreach (InstanceInfo instanceInfo in gamePool.GetAllInstances())
						{
							if (!instanceInfo.HostAddress.Equals("local") && instanceInfo.LastUpdate < expiryTime)
							{
								toRemoveInstances.Add(instanceInfo);
							}
						}

						foreach (InstanceInfo instanceInfo in toRemoveInstances)
						{
							gamePool.RemoveInstance(instanceInfo);
							SkyUtil.log($"Removing {instanceInfo.HostAddress + ":" + instanceInfo.HostPort} from {gamePool.GameName}'s pool due to expiry");
						}

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
				if ((ipAddress + ":" + connectingPort).Equals(SkyCoreAPI.Instance.CurrentIp))
				{
					return;
				}

				RegisterExternalGame(messageSplit[2], connectingPort, messageSplit[0], messageSplit[1]);
			});

			//Always register the intent of a hub to exist
			RegisterGameIntent("hub");
		}

		//

		public static void RegisterGameIntent(string gameName, bool spawnNPC = false)
		{
			bool npcOnly = false; //Some games may not be completed. Use an NPC as a placeholder
			if (spawnNPC)
			{
				string neatName = gameName;
				PlayerLocation npcLocation = new PlayerLocation(0.5D, 30D, 16.5D, 180F, 180F, 0F);

				switch (gameName)
				{
					case "murder":
					{
						neatName = "§c§lMurder Mystery";
						npcLocation = new PlayerLocation(260.5, 77, 271.5, 180F, 180F, 0F);
						break;
					}
					case "build-battle":
					{
						neatName = "§e§lBuild Battle";
						npcLocation = new PlayerLocation(252.5, 77, 271.5, 180F, 180F, 0F);
						break;
					}
					case "block-hunt":
					{
						neatName = PlayerNPC.ComingSoonName;
						npcLocation = new PlayerLocation(263.5, 77, 269.5, 180F, 180F, 0F);
						npcOnly = true;
						break;
					}
					case "bed-wars":
					{
						neatName = PlayerNPC.ComingSoonName;
						npcLocation = new PlayerLocation(249.5, 77, 269.5, 180F, 180F, 0F);
						npcOnly = true;
						break;
					}
				}

				if (!gameName.Equals("hub"))
				{
					try
					{
						PlayerNPC.SpawnHubNPC(null, neatName, npcLocation, $"GID:{gameName}");
					}
					catch (Exception e)
					{
						Console.WriteLine(e);
					}
				}
			}

			{
				if (!GameRegistrations.ContainsKey(gameName))
				{
					//Initialize GamePool
					GamePool gamePool = GetGamePool(gameName);

					if (npcOnly)
					{
						gamePool.Active = false;
						return;
					}
				}
				else
				{
					return; //Already listening, Don't start again.
				}
			}

			ISubscriber subscriber = RedisPool.GetSubscriber();

			/*
			 * Channel <game>_info
			 * Format:
			 * {current-players}:{available-servers} //TODO: Update format?
			 */
			subscriber.SubscribeAsync($"{gameName}_info", (channel, message) =>
			{
				try
				{
					string[] messageSplit = ((string)message).Split(':');

					string hostAddress = messageSplit[0];
					if (!ushort.TryParse(messageSplit[1], out ushort hostPort))
					{
						SkyUtil.log($"Invalid format of port in message {message}");
						return;
					}

					InstanceInfo instanceInfo;

					if (!GameRegistrations.TryGetValue(gameName, out var gamePool))
					{
						instanceInfo = new InstanceInfo { HostAddress = hostAddress, HostPort = hostPort };

						SkyUtil.log($"Game {gameName} missing from GameRegistrations! Re-Registering...");
						RegisterGame(gameName, instanceInfo);
					}
					else
					{
						instanceInfo = gamePool.GetInstance(hostAddress + ":" + hostPort);
					}

					int.TryParse(messageSplit[2], out var instancePlayers);

					instanceInfo.CurrentPlayers = instancePlayers;

					List<GameInfo> availableGames = new List<GameInfo>();
					int i = 3;
					while (i < messageSplit.Length)
					{
						string messageSplitContent = messageSplit[i];
						if (messageSplitContent.Length == 0)
						{
							break; //Empty content - End of message
						}

						string[] gameInfoSplit = messageSplitContent.Split(',');

						int.TryParse(gameInfoSplit[1], out var currentPlayers);
						int.TryParse(gameInfoSplit[2], out var maxPlayers);

						availableGames.Add(new GameInfo(gameInfoSplit[0], currentPlayers, maxPlayers));

						i++;
					}

					instanceInfo.AvailableGames = availableGames;
					instanceInfo.Update();
					//SkyUtil.log($"Updated {availableGames.Count} available games on {gameName} ({hostAddress + ":" + hostPort})");
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
			});
		}

		public static void RegisterExternalGame(string connectingAddress, ushort connectingPort, string targetServer, string gameName)
		{
			InstanceInfo instanceInfo = new InstanceInfo
			{
				CurrentPlayers = 0,
				HostAddress = connectingAddress,
				HostPort = connectingPort
			};

			RegisterGame(gameName, instanceInfo);
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
							$"{gameName}:{gameName}:{CurrentHostAddress}");
					}

					Thread.Sleep(1000); // Update every 1 second

					InstanceInfo instanceInfo = GameRegistrations[gameName].GetLocalInstance();
					if (instanceInfo == null)
					{
						SkyUtil.log($"Game not registered under 'local'. {GameRegistrations[gameName].GetAllInstances()}");
						continue; //Game not registered?
					}

					string availableGameConcat = "";
					foreach (GameInfo gameInfo in instanceInfo.AvailableGames)
					{
						availableGameConcat += gameInfo.GameId + "," + gameInfo.CurrentPlayers + "," + gameInfo.MaxPlayers + ":";
					}

					string messageContents =
						SkyCoreAPI.Instance.CurrentIp + ":" + instanceInfo.CurrentPlayers + ":" + availableGameConcat;

					//SkyUtil.log($"Sending update on {gameName}_info as {messageContents}");
					RedisPool.GetSubscriber().PublishAsync($"{gameName}_info", messageContents);
				}

			}).Start();

			RegisterGame(gameName, new InstanceInfo{HostAddress = "local"});

			RedisPool.GetSubscriber().SubscribeAsync($"{gameName}_join", (channel, message) =>
			{
				string[] messageSplit = ((string)message).Split(':');

				if(GameRegistrations.TryGetValue(messageSplit[1], out var gamePool))
				{
					foreach (GameInfo gameInfo in gamePool.GetLocalInstance().AvailableGames)
					{
						if (gameInfo.GameId.Equals(messageSplit[2]))
						{
							if (IncomingPlayers.ContainsKey(messageSplit[0]))
							{
								IncomingPlayers[messageSplit[0]] = gameInfo;
							}
							else
							{
								if (!IncomingPlayers.TryAdd(messageSplit[0], gameInfo))
								{
									return; //Cannot process?
								}
							}

							foreach (GameLevel gameLevel in SkyCoreAPI.Instance.GameModes[messageSplit[1]].GameLevels.Values)
							{
								if (gameLevel.GameId.Equals(messageSplit[2]))
								{ 
									gameLevel.AddIncomingPlayer(messageSplit[0]);
									break;
								}
							}
							
							return;
						}
					}
				}
			});
		}
		
		private static readonly ConcurrentDictionary<string, GameInfo> IncomingPlayers = new ConcurrentDictionary<string, GameInfo>();

		public static GameInfo GetGameForIncomingPlayer(string username)
		{
			if (IncomingPlayers.TryGetValue(username, out var gameInfo))
			{
				return gameInfo;
			}

			return null;
		}

		public static GamePool GetGamePool(string gameName)
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

			return gamePool;
		}

		public static void RegisterGame(string gameName, InstanceInfo instanceInfo)
		{
			GetGamePool(gameName).AddInstance(instanceInfo);
		}

		public static void AddPlayer(SkyPlayer player, string gameName)
		{
			if (!GameRegistrations.ContainsKey(gameName))
			{
				player.SendMessage($"{ChatColors.Red}No game existed for the name '{gameName}'");
				return;
			}

			RequeuePlayer(player, gameName);
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
		
		public bool Active { get; set; }
		
		public string GameName { get; }

		public PlayerLocation NpcLocation { get; set; }

		//

		private readonly ConcurrentDictionary<string, InstanceInfo> _gameInstances = new ConcurrentDictionary<string, InstanceInfo>();

		public GamePool(string gameName)
		{
			GameName = gameName;
			Active = true;
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
			_gameInstances.TryGetValue("local", out var localInstance);
			return localInstance;
		}

		public void AddPlayer(SkyPlayer player)
		{
			InstanceInfo bestGameInstance = null;
			GameInfo bestAvailableGame = null;
			
			SkyUtil.log($"Checking available games from all instances for {GameName} ({_gameInstances.Count} total instances to search)");

			//Check current instance first
			if (_gameInstances.ContainsKey("local"))
			{
				InstanceInfo instanceInfo = _gameInstances["local"];
				foreach (GameInfo gameInfo in instanceInfo.AvailableGames)
				{
					SkyUtil.log($"Checking {gameInfo.GameId} on {instanceInfo.HostAddress}");
					if (bestAvailableGame == null || gameInfo.CurrentPlayers > bestAvailableGame.CurrentPlayers)
					{
						SkyUtil.log("Found best game!");
						bestGameInstance = instanceInfo;
						bestAvailableGame = gameInfo;
					}
				}
			}

			if (bestAvailableGame == null)
			{
				foreach (InstanceInfo instanceInfo in _gameInstances.Values)
				{
					SkyUtil.log($"Checking instance {instanceInfo.HostAddress} Available Servers: {instanceInfo.AvailableGames.Count}");
					foreach (GameInfo gameInfo in instanceInfo.AvailableGames)
					{
						SkyUtil.log($"Checking {gameInfo.GameId} on {instanceInfo.HostAddress}");
						if (bestAvailableGame == null || gameInfo.CurrentPlayers > bestAvailableGame.CurrentPlayers)
						{
							SkyUtil.log("Found best game!");
							bestGameInstance = instanceInfo;
							bestAvailableGame = gameInfo;
						}
					}
				}
			}

			if (bestAvailableGame == null)
			{
				//player.SendMessage($"§cWe were unable to move you to another game of {GameName}.");
				if (GetCurrentPlayers() > 0) //
				{
					TitleUtil.SendCenteredSubtitle(player, "  §c§lGAME FULL§r" + "\n" + "§7Try joining again soon!", false);
				}
				else //No players found, and all available servers unjoinable
				{
					TitleUtil.SendCenteredSubtitle(player, " §c§lGAME UNAVAILABLE§r" + "\n" + "§7Try joining again soon!", false);
				}
				
				player.Freeze(false); //Unfreeze
			}
			else
			{
				if (player.Level is GameLevel level)
				{
					level.RemovePlayer(player);
				}

				if (bestGameInstance.HostAddress.Equals("local"))
				{
					SkyCoreAPI.Instance.GameModes[GameName].InstantQueuePlayer(player, bestAvailableGame);
				}
				else
				{
					ExternalGameHandler.RedisPool.GetSubscriber().PublishAsync($"{GameName}_join", $"{player.Username}:{GameName}:{bestAvailableGame.GameId}");

					McpeTransfer transferPacket = new McpeTransfer
					{
						serverAddress = bestGameInstance.HostAddress,
						port = bestGameInstance.HostPort
					};

					player.SendPackage(transferPacket);
				}
			}
		}

		public List<InstanceInfo> GetAllInstances()
		{
			List<InstanceInfo> games = new List<InstanceInfo>();
			games.AddRange(_gameInstances.Values);
			return games;
		}

		public InstanceInfo GetInstance(string instanceKey)
		{
			if (!_gameInstances.TryGetValue(instanceKey, out var instanceInfo))
			{
				string[] messageSplit = instanceKey.Split(':');

				string hostAddress = messageSplit[0];
				if (!ushort.TryParse(messageSplit[1], out ushort hostPort))
				{
					SkyUtil.log($"Invalid format of port in instancekey {instanceKey}");
					return new InstanceInfo(); //Return empty instance info to keep them happy
				}

				instanceInfo = new InstanceInfo
				{
					HostAddress = hostAddress,
					HostPort = hostPort
				};

				_gameInstances.TryAdd(instanceKey, instanceInfo);
			}

			return instanceInfo;
		}

		public void AddInstance(InstanceInfo instanceInfo)
		{
			string instanceKey;
			if (instanceInfo.HostAddress.Equals("local"))
			{
				instanceKey = "local";
			}
			else
			{
				instanceKey = instanceInfo.HostAddress + ":" + instanceInfo.HostPort;
			}

			if (!_gameInstances.ContainsKey(instanceKey))
			{
				_gameInstances.TryAdd(instanceKey, instanceInfo);
				//SkyUtil.log($"Added {instanceKey} to {GameName}'s Instance Pool ({instanceInfo.AvailableGames.Count} Games)");
			}
			else
			{
				//_gameInstances[instanceKey] = instanceInfo; //Update
				_gameInstances[instanceKey].Update();
				//SkyUtil.log($"Updated {instanceKey} in {GameName}'s Instance Pool ({instanceInfo.AvailableGames.Count} Games)");
			}
		}

		public void RemoveInstance(InstanceInfo instanceInfo)
		{
			_gameInstances.TryRemove(instanceInfo.HostAddress + ":" + instanceInfo.HostPort, out _);
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

		public InstanceInfo()
		{
			Update();
		}

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
