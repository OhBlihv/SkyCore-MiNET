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

		public static readonly ConcurrentDictionary<string, GameInfo> GameRegistrations = new ConcurrentDictionary<string, GameInfo>();

		private static readonly ConnectionMultiplexer RedisPool;

		public static int TotalPlayers { get; private set; }

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
					foreach (GameInfo gameInfo in GameRegistrations.Values)
					{
						totalPlayers += gameInfo.CurrentPlayers;
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

				ushort connectingPort;
				if (!ushort.TryParse(messageSplit[3], out connectingPort))
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

				SkyUtil.log($"Registering {messageSplit[0]} from {ipAddress}:{connectingPort}");

				RegisterExternalGame(messageSplit[2], connectingPort, messageSplit[0], messageSplit[1]);
			});
		}

		//

		public static void RegisterExternalGame(string connectingAddress, ushort connectingPort, string targetServer, string gameName)
		{
			ExternalGameInfo gameInfo = new ExternalGameInfo(connectingAddress, connectingPort, targetServer, gameName);
			GameRegistrations.TryAdd(gameName, gameInfo);

			SkyCoreAPI.Instance.AddPendingTask(() =>
			{
				Level level = SkyCoreAPI.Instance.Context.LevelManager.Levels.FirstOrDefault(l => l.LevelId.Equals("Overworld", StringComparison.InvariantCultureIgnoreCase));

				if (level == null)
				{
					Console.WriteLine($"§c§l(!) §r§cUnable to find level Overworld/world");

					string worldNames = "";
					foreach (Level levelLoop in SkyCoreAPI.Instance.Context.LevelManager.Levels)
					{
						worldNames += levelLoop.LevelName + "(" + levelLoop.LevelId + "), ";
					}

					Console.WriteLine($"§7§l* §r§7Valid Names: {worldNames}");
				}
				else
				{
					PlayerNPC.SpawnNPC(level, $"§e§l{gameName}", new PlayerLocation(0.5D, 30D, 16.5D, 180F, 180F), $"GID:{gameName}");
				}
			});

			ISubscriber subscriber = RedisPool.GetSubscriber();

			/*
			 * Channel <game>_info
			 * Format:
			 * {current-players}:{available-servers}
			 */
			subscriber.SubscribeAsync($"{gameName}_info", (channel, message) =>
			{
				SkyUtil.log($"Received update for {channel} > {message}");
				string[] messageSplit = ((string)message).Split(':');

				int currentPlayers, availableGames;

				int.TryParse(messageSplit[0], out currentPlayers);
				int.TryParse(messageSplit[1], out availableGames);

				gameInfo.update(currentPlayers, availableGames);
			});
		}

		public static void RegisterInternalGame(string gameName)
		{
			new Thread(delegate()
			{

				while (!SkyCoreAPI.IsDisabled)
				{
					Thread.Sleep(1000); // Update every 1 second

					GameInfo gameInfo = GameRegistrations[gameName];
					SkyUtil.log($"Sending update on {gameName}_info as " + gameInfo.CurrentPlayers + ":" + gameInfo.AvailableGames);
					RedisPool.GetSubscriber().PublishAsync($"{gameName}_info", gameInfo.CurrentPlayers + ":" + gameInfo.AvailableGames);
				}

			}).Start();

			ISubscriber subscriber = RedisPool.GetSubscriber();

			//Temp - Sending server is gameName
			subscriber.PublishAsync("game_register",
				$"{gameName}:{gameName}:{Config.GetProperty("ip", SkyCoreAPI.Instance.CurrentIp)}:{Config.GetProperty("port", "19132")}");

			GameRegistrations.TryAdd(gameName, new GameInfo("local", gameName));

			/*subscriber.SubscribeAsync($"{gameName}_join", (channel, message) =>
			{

			});*/
		}

		public static void AddPlayer(SkyPlayer player, string gameName)
		{
			if (!GameRegistrations.ContainsKey(gameName))
			{
				player.SendMessage($"{ChatColors.Red}No game existed for the name '{gameName}'");
				return;
			}

			player.SendMessage($"§e§l(!) §r§eJoining {gameName}...");

			GameInfo gameInfo = GameRegistrations[gameName];
			if (gameInfo is ExternalGameInfo)
			{
				McpeTransfer transferPacket = new McpeTransfer
				{
					serverAddress = ((ExternalGameInfo) gameInfo).ConnectingAddress,
					port = ((ExternalGameInfo) gameInfo).ConnectingPort
				};

				player.SendPackage(transferPacket);
			}
			else
			{
				SkyCoreAPI.Instance.GameModes[gameName].QueuePlayer(player);
			}
		}

		public static void RequeuePlayer(SkyPlayer player, string gameName)
		{
			if (!GameRegistrations.ContainsKey(gameName))
			{
				player.SendMessage($"{ChatColors.Red}No game existed for the name '{gameName}'");
				return;
			}

			GameInfo gameInfo = GameRegistrations[gameName];
			if (gameInfo is ExternalGameInfo)
			{
				McpeTransfer transferPacket = new McpeTransfer
				{
					serverAddress = ((ExternalGameInfo)gameInfo).ConnectingAddress,
					port = ((ExternalGameInfo)gameInfo).ConnectingPort
				};

				player.SendPackage(transferPacket);
			}
			else
			{
				SkyCoreAPI.Instance.GameModes[gameName].QueuePlayer(player);
			}
		}

	}

	public class GameInfo
	{

		public string HostingServer { get; }

		public string GameName { get; }

		public int CurrentPlayers { get; set; }

		public int AvailableGames { get; set; }

		public long LastUpdate { get; private set; }

		public PlayerLocation NpcLocation { get; set; }

		public GameInfo(string hostingServer, string gameName)
		{
			HostingServer = hostingServer;
			GameName = gameName;
		}

		public void update(int currentPlayers, int availableGames)
		{
			AvailableGames = availableGames;
			CurrentPlayers = currentPlayers;

			LastUpdate = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		}

	}

	public class ExternalGameInfo : GameInfo
	{

		public string ConnectingAddress { get; }

		public ushort ConnectingPort { get; }

		public ExternalGameInfo(string connectingAddress, ushort connectingPort, 
								string hostingServer, string gameName) : base(hostingServer, gameName)
		{
			ConnectingAddress = connectingAddress;
			ConnectingPort = connectingPort;
		}

	}

}
