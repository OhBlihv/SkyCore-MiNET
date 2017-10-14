using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MiNET;
using MiNET.Items;
using MiNET.Net;
using MiNET.Plugins;
using MiNET.Plugins.Attributes;
using MiNET.Sounds;
using MiNET.Utils;
using MiNET.Worlds;
using SkyCore.Commands;
using SkyCore.Entities;
using SkyCore.Game;
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

        public PluginContext Context;

		public string CurrentIp { get; private set; }

        public SkyPermissions Permissions { get; set; }

		public string GameType { get; private set; }

        public readonly ConcurrentDictionary<string, CoreGameController> GameModes = new ConcurrentDictionary<string, CoreGameController>();

		private readonly List<PendingTask> _pendingTasks = new List<PendingTask>();
		public delegate void PendingTask();
		private bool _shouldSchedule = true;
		public void AddPendingTask(PendingTask pendingTask)
		{
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
            Instance = this;
            SkyUtil.log("SkyCore Initializing...");

            ServerPath = Environment.CurrentDirectory;
            SkyUtil.log($"Registered Server Path at '{ServerPath}'");

			CurrentIp = new WebClient().DownloadString("http://icanhazip.com").Replace("\n", "") + ":" + Config.GetProperty("port", "19132");
			SkyUtil.log($"Registered current IP as {CurrentIp}");

	        ExternalGameHandler.CurrentHostAddress = CurrentIp;

			Context = context;

            //

            context.PluginManager.LoadCommands(new SkyCommands(this));  //Initialize Generic Commands
            context.PluginManager.LoadCommands(Permissions);            //Initialize Permission Commands
	        
            context.LevelManager.LevelCreated += (sender, args) =>
            {
                Level level = args.Level;
                
                //Override Spawn Point for testing
                if (level.LevelId.Equals("Overworld"))
                {
					//level.SpawnPoint = new PlayerLocation(0D, 36D, 10D, 0f, 0f, 90f);
					level.SpawnPoint = new PlayerLocation(256.5, 78, 255.5);

                    level.BlockBreak += LevelOnBlockBreak;
                    level.BlockPlace += LevelOnBlockPlace;

	                level.CurrentWorldTime = 22000; //Sunrise?
	                SkyUtil.log($"Set world time to {level.CurrentWorldTime}");
	                
	                AddPendingTask(() =>
	                {
						{
							PlayerLocation portalInfoLocation = new PlayerLocation(256.5, 79.5, 276.5);

							string hologramContent =
								"  §d§lSkytonia§r §f§lNetwork§r" + "\n" + 
								" §7Enter the portal and§r" + "\n" +
								"§7enjoy your adventure!§r" + "\n" +
								"     §ewww.skytonia.com§r";

							Hologram portalInfoHologram = new Hologram(hologramContent, level, portalInfoLocation);

							portalInfoHologram.SpawnEntity();
						}
					});
                }
            };

            //Register listeners
            context.Server.PlayerFactory.PlayerCreated += (sender, args) =>
			{
				_shouldSchedule = false; //Avoid scheduling pending tasks once a player has joined

                //SkyPlayer player = (SkyPlayer) args.Player;
                MiNET.Player player = args.Player;

				//Disable inventory editing
				//player.Inventory.InventoryChange

				//Only add this join listener for hubs
				if (GameModes.Count == 0)
				{
					player.PlayerJoin += OnPlayerJoin;
				}
				player.PlayerLeave += OnPlayerLeave;

				if (_pendingTasks.Count > 0)
				{
					foreach (PendingTask pendingTask in _pendingTasks)
					{
						RunnableTask.RunTaskLater(() =>
						{
							pendingTask.DynamicInvoke();
						}, 5000);
					}

					_pendingTasks.Clear();
				}

				/*ThreadPool.QueueUserWorkItem(state =>
				{
					Thread.Sleep(1000);

					try
					{
						//Add this player to any games if available and if this is the only game available
						if (GameModes.Count == 1)
						{
							//Foreach, but only one value.
							foreach (CoreGameController coreGameController in GameModes.Values)
							{
								coreGameController.QueuePlayer(player as SkyPlayer);
								break;
							}
						}
					}
					catch (Exception e)
					{
						Console.WriteLine(e);
					}
				});*/
			};

			//Start RestartHandler
	        RestartHandler.Start();

			SkyUtil.log("Initialized!");

            ThreadPool.QueueUserWorkItem(state =>
            {
                Thread.Sleep(1000);

				ExternalGameHandler.Init(); //Start listening for game servers

				string serverGame = Config.GetProperty("game-type", "none");
				SkyUtil.log($"Setting up Custom Game {serverGame}...");
	            GameType = serverGame;
				try
				{
					switch (serverGame)
					{
						case "murder":
						{
							SkyUtil.log("Initializing Murder Mystery...");
							_initializeCustomGame(new MurderCoreGameController(this));
							break;
						}
						case "build-battle":
						{
							SkyUtil.log("Initializing Build Battle...");
							_initializeCustomGame(new BuildBattleCoreGameController(this));
							break;
						}
						case "none":
						{
							SkyUtil.log("Initializing Pure Hub Handling...");
							_initializeCustomGame(new HubCoreController(this));
							break;
						}
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e.StackTrace);
				}
			});
        }

        private void _initializeCustomGame(CoreGameController coreGameController)
        {
            GameModes.TryAdd(coreGameController.RawName, coreGameController);
            SkyUtil.log($"Initialized {coreGameController.GameName} Controller.");
        }

        public void Configure(MiNetServer server)
        {
            SkyUtil.log("Startup begun.");

            Permissions = new SkyPermissions(this);
            server.PlayerFactory = new SkyPlayerFactory { SkyCoreApi = this };

            SkyUtil.log("Startup complete.");
        }

        public void OnDisable()
        {
	        IsDisabled = true;

            foreach (Level level in Context.LevelManager.Levels)
            {
                foreach (MiNET.Player player in level.Players.Values)
                {
                    //TODO: Improve?
                    player.Disconnect("§eThis instance is currently rebooting. Rejoin to continue playing!");
                }
            }

			PunishCore.Close();
			StatisticsCore.Close();
        }

        private void LevelOnBlockBreak(object sender, BlockBreakEventArgs e)
        {
            if (!((SkyPlayer) e.Player).PlayerGroup.isAtLeast(PlayerGroup.Admin))
            {
                e.Cancel = true;
            }
        }

        private void LevelOnBlockPlace(object sender, BlockPlaceEventArgs e)
        {
            if (!((SkyPlayer)e.Player).PlayerGroup.isAtLeast(PlayerGroup.Admin))
            {
                e.Cancel = true;
            }
        }

        private void OnPlayerJoin(object o, PlayerEventArgs eventArgs)
        {
            Console.Write("Processing join");
            Level level = eventArgs.Level;
            if (level == null) throw new ArgumentNullException(nameof(eventArgs.Level));

            SkyPlayer player = (SkyPlayer) eventArgs.Player;
            if (player == null) throw new ArgumentNullException(nameof(eventArgs.Player));
            Console.Write(" for " + player.Username + "\n");

            player.Inventory.Slots[4] = new ItemCompass() {Count = 1};

            player.SendPlayerInventory();

	        int i = 0;

            ThreadPool.QueueUserWorkItem(state =>
            {
	            player.SendTitle("§f", TitleType.Clear);
	            player.SendTitle("§f", TitleType.AnimationTimes, 6, 6, 20 * 10);
	            player.SendTitle("§f", TitleType.ActionBar, 6, 6, 20 * 10);
	            player.SendTitle("§f", TitleType.Title, 6, 6, 20 * 10);
	            player.SendTitle("§f", TitleType.SubTitle, 6, 6, 20 * 10);

				Thread.Sleep(2000);

                //Group isn't initialized yet - wait.
                SkyUtil.log($"{++i} Group for {player.Username} == {player.PlayerGroup} vs {PlayerGroup.Youtuber} == {player.PlayerGroup.CompareTo(PlayerGroup.Youtuber)}");

                //player.SendTitle($"{ChatColors.Gold}Welcome {player.Username}\n{ChatColors.LightPurple}~ To the Skytonia Network ~", TitleType.ActionBar);
                Console.WriteLine(" (Joined!)");
            });
        
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
                rank = Permissions.getPlayerGroup(player.Username).Prefix;
            }

            if (rank.Length > 2)
            {
                rank += " ";
            }

            return $"{rank}{username}";
        }

	    public Level GetHubLevel()
	    {
		    Level level = Context.LevelManager.Levels.FirstOrDefault(l => l.LevelId.Equals("Overworld", StringComparison.InvariantCultureIgnoreCase));

		    if (level == null)
		    {
			    if (Context.LevelManager.Levels.Count > 0)
			    {
					Console.WriteLine("§c§l(!) §r§cUnable to find level Overworld/world. Returning 0th level.");
				    return Context.LevelManager.Levels[0];
				}

			    return null;
		    }

		    return level;
	    }

	    public SkyPlayer GetPlayer(string username)
        {
            username = username.ToLower();
            
            MiNET.Player foundPlayer = null;
            foreach (Level level in Context.LevelManager.Levels)
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
			foreach (Level level in Context.LevelManager.Levels)
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
