using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using MiNET;
using MiNET.Blocks;
using MiNET.Items;
using MiNET.Net;
using MiNET.Plugins;
using MiNET.Plugins.Attributes;
using MiNET.Utils;
using MiNET.Worlds;
using SkyCore.Blocks;
using SkyCore.Commands;
using SkyCore.Entities;
using SkyCore.Game;
using SkyCore.Game.Level;
using SkyCore.Games.BuildBattle;
using SkyCore.Games.Hub;
using SkyCore.Games.Murder;
using SkyCore.Permissions;
using SkyCore.Player;
using SkyCore.Punishments;
using SkyCore.Restart;
using SkyCore.Statistics;
using SkyCore.Util;

namespace SkyCore
{
    public class SkyCoreAPI : IPlugin, IStartup
    {
        
        public static SkyCoreAPI Instance { get; set; }

	    public static bool IsRebootQueued { get; set; }
	    
		public static bool IsDisabled { get; private set; }
        
        public static string ServerPath { get; private set; }

        public MiNetServer Server;

		public string CurrentIp { get; private set; }

        public SkyPermissions Permissions { get; set; }

		public string GameType { get; private set; }

        public readonly ConcurrentDictionary<string, GameController> GameModes = new ConcurrentDictionary<string, GameController>();

		private readonly List<PendingTask> _pendingTasks = new List<PendingTask>();
		public delegate void PendingTask();
		private bool _shouldSchedule = true;
		public void AddPendingTask(PendingTask pendingTask)
		{
			if (pendingTask == null)
			{
				throw new NullReferenceException("Null Task Provided");
			}
			
			if (!_shouldSchedule)
			{
				pendingTask.Invoke();
			}
			else
			{
				_pendingTasks.Add(pendingTask);
			}
		}

        public void OnEnable(PluginContext context)
        {
			context.PluginManager.LoadCommands(new SkyCommands(this));  //Initialize Generic Commands
	        context.PluginManager.LoadCommands(Permissions);            //Initialize Permission Commands

	        //Register listeners
	        context.Server.PlayerFactory.PlayerCreated += (sender, args) =>
	        {
		        _shouldSchedule = false; //Avoid scheduling pending tasks once a player has joined

		        MiNET.Player player = args.Player;

		        player.PlayerJoin += OnPlayerJoin;
		        player.PlayerLeave += OnPlayerLeave;

		        if (_pendingTasks.Count > 0)
		        {
			        foreach (PendingTask pendingTask in _pendingTasks)
			        {
				        RunnableTask.RunTaskLater(() =>
				        {
					        try
					        {
						        pendingTask.Invoke();
					        }
					        catch (Exception e)
					        {
						        Console.WriteLine(e);
					        }

				        }, 5000);
			        }

			        _pendingTasks.Clear();
		        }
	        };

	        //Trigger any post-launch tasks that cannot be run during startup
	        foreach (GameController coreGameController in GameModes.Values)
	        {
		        coreGameController.PostLaunchTask();
	        }

	        //Start RestartHandler for Automatic Reboots
	        RestartHandler.Start();

	        SkyUtil.log("Initialized!");
		}

        private void _initializeCustomGame(GameController coreGameController)
        {
            GameModes.TryAdd(coreGameController.RawName, coreGameController);
            SkyUtil.log($"Initialized {coreGameController.GameName} Controller.");
        }

        public void Configure(MiNetServer server)
        {
			Server = server;

			SkyUtil.log("Hooking into MiNET.");

			Instance = this;
			SkyUtil.log("SkyCore Initializing...");

			ServerPath = Environment.CurrentDirectory;
			SkyUtil.log($"Registered Server Path at '{ServerPath}'");

			CurrentIp = new WebClient().DownloadString("http://icanhazip.com").Replace("\n", "") + ":" + Config.GetProperty("port", "19132");
			SkyUtil.log($"Registered current IP as {CurrentIp}");

			ExternalGameHandler.CurrentHostAddress = CurrentIp;

			BlockFactory.CustomBlockFactory = new SkyBlockFactory();
			server.LevelManager = new SkyLevelManager(this);

			//Create Games once the LevelManager has been initialized to avoid launching without any levels

			ExternalGameHandler.Init(server); //Start listening for game servers

			string serverGame = Config.GetProperty("game-type", "none");
			SkyUtil.log($"Setting up Custom Game {serverGame}...");
			GameType = serverGame;
			try
			{
				Type gameControllerType = null;
				string gameName = null;

				switch (serverGame)
				{
					case "murder":
					{
						gameName = "Murder Mystery";
						gameControllerType = typeof(MurderGameController);
						break;
					}
					case "build-battle":
					{
						gameName = "Build Battle";
						gameControllerType = typeof(BuildBattleGameController);
						break;
					}
					case "none":
					{
						gameName = "Pure Hub";
						
						//none -> hub
						GameType = "hub";

						gameControllerType = typeof(HubController);

						break;
					}
				}

				if (gameControllerType == null)
				{
					SkyUtil.log("No Game Loaded.");
					return;
				}

				SkyUtil.log($"Initializing Game {gameName}...");
				_initializeCustomGame(Activator.CreateInstance(gameControllerType, this) as GameController);
				Thread.Sleep(1000); //Pause the main thread for a second to ensure the levels are setup and avoid any CME
				SkyUtil.log($"Finished Initializing {gameName}");

				//Register all remaining games
				bool spawnNPC = gameName.Equals("Pure Hub");

				ExternalGameHandler.RegisterGameIntent("murder", spawnNPC);
				ExternalGameHandler.RegisterGameIntent("build-battle", spawnNPC);
				ExternalGameHandler.RegisterGameIntent("block-hunt", spawnNPC);
				ExternalGameHandler.RegisterGameIntent("bed-wars", spawnNPC);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				//TODO: Prevent players joining
			}

			//

			Permissions = new SkyPermissions(this);
			server.PlayerFactory = new SkyPlayerFactory { SkyCoreApi = this };

			SkyUtil.log("Finished Hooks.");
		}

        public void OnDisable()
        {
	        IsDisabled = true;

	        if (!GameType.Equals("hub"))
	        {
				foreach (Level level in Server.LevelManager.Levels)
		        {
			        foreach (MiNET.Player player in level.Players.Values)
			        {
				        ExternalGameHandler.AddPlayer(player as SkyPlayer, "hub");
			        }
		        }

		        Thread.Sleep(1000);
			}

			foreach (Level level in Server.LevelManager.Levels)
			{
				foreach (MiNET.Player player in level.Players.Values)
				{
					player.Disconnect("                      §d§lSkytonia §f§lNetwork§r\n" +
					                  "§7Skytonia is currently rebooting, try joining again soon!");
				}
			}
	        
	        RunnableTask.CancelAllTasks();

			PunishCore.Close();
			StatisticsCore.Close();
        }

        private void OnPlayerJoin(object o, PlayerEventArgs eventArgs)
        {
            Console.Write("Processing join");
            Level level = eventArgs.Level;
            if (level == null) throw new ArgumentNullException(nameof(eventArgs.Level));

            SkyPlayer player = (SkyPlayer) eventArgs.Player;
            if (player == null) throw new ArgumentNullException(nameof(eventArgs.Player));
            Console.Write(" for " + player.Username + "\n");

	        RunnableTask.RunTaskLater(() =>
	        {
		        player.SendTitle("§f", TitleType.Clear);
		        player.SendTitle("§f", TitleType.AnimationTimes, 6, 6, 20 * 10);
		        player.SendTitle("§f", TitleType.ActionBar, 6, 6, 20 * 10);
		        player.SendTitle("§f", TitleType.Title, 6, 6, 20 * 10);
		        player.SendTitle("§f", TitleType.SubTitle, 6, 6, 20 * 10);
			}, 500);
		}

        private void OnPlayerLeave(object o, PlayerEventArgs eventArgs)
        {
	        if (o == null)
	        {
		        return;
	        }

            if (eventArgs.Level is GameLevel level)
            {
                level.RemovePlayer((SkyPlayer) eventArgs.Player);
            }
        }

        [PacketHandler, Receive]
        public Package MessageHandler(McpeText message, MiNET.Player player)
        {
            string text = message.message;
            if (text.StartsWith(".") || text.StartsWith("/"))
            {
                return message;
            }

	        if (PunishCore.GetPunishmentsFor(player.CertificateData.ExtraData.Xuid).HasActive(PunishmentType.Mute))
	        {
				player.SendMessage("§c§l(!)§r §cYou cannot chat while you are muted.");
		        return null;
	        }
            
            text = TextUtils.RemoveFormatting(text);

	        string chatColor = ChatColors.White;
	        if (((SkyPlayer) player).PlayerGroup == PlayerGroup.Player)
	        {
		        chatColor = ChatColors.Gray;
	        }

            string formattedText = $"{GetNameTag(player)}{ChatColors.Gray}: {chatColor}{text}";
            SkyUtil.log($"Broadcasting to {player.Level.LevelId}: {formattedText}");
            player.Level.BroadcastMessage(formattedText, MessageType.Raw);

            return null;
        }

        private string GetNameTag(MiNET.Player player)
        {
            string username = player.Username;

            string rank;
            if (player is SkyPlayer skyPlayer)
            {
                rank = skyPlayer.PlayerGroup.Prefix;
            }
            else
            {
                rank = Permissions.GetPlayerGroup(player.Username).Prefix;
            }

            if (rank.Length > 2)
            {
                rank += " ";
            }

            return $"{rank}{username}";
        }

	    public Level GetHubLevel()
	    {
		    Level level = Server.LevelManager.Levels.FirstOrDefault(l => l.LevelId.Equals("Overworld", StringComparison.InvariantCultureIgnoreCase));

		    if (level == null)
		    {
			    if (Server.LevelManager.Levels.Count > 0)
			    {
					Console.WriteLine("§c§l(!) §r§cUnable to find level Overworld/world. Returning 0th level.");
				    return Server.LevelManager.Levels[0];
				}

			    return null;
		    }

		    return level;
	    }

	    public SkyPlayer GetPlayer(string username)
        {
            username = username.ToLower();
            
            MiNET.Player foundPlayer = null;
            foreach (Level level in Server.LevelManager.Levels)
            {
                foreach (MiNET.Player player in level.Players.Values)
                {
                    if (player.Username.ToLower().Equals(username))
                    {
                        foundPlayer = player;
                    }
                }
            }

            return (SkyPlayer) foundPlayer;
        }

	    public ISet<SkyPlayer> GetAllOnlinePlayers()
	    {
			ISet<SkyPlayer> players = new HashSet<SkyPlayer>();
			foreach (Level level in Server.LevelManager.Levels)
			{
				foreach (MiNET.Player player in level.Players.Values)
				{
					players.Add((SkyPlayer) player);
				}
			}

		    return players;
	    }

    }
}
