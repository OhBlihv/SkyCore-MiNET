using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
using SkyCore.Games.Murder;
using SkyCore.Permissions;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore
{
    public class SkyCoreAPI : IPlugin, IStartup
    {
        
        public static SkyCoreAPI Instance { get; set; }
        
        public static string ServerPath { get; private set; }

        public PluginContext Context;

        public SkyPermissions Permissions { get; set; }

        public readonly ConcurrentDictionary<string, CoreGameController> GameModes = new ConcurrentDictionary<string, CoreGameController>();

		public List<PendingTask> PendingTasks = new List<PendingTask>();
		public delegate void PendingTask();

        public void OnEnable(PluginContext context)
        {
            Instance = this;
            SkyUtil.log("SkyCore Initializing...");

            ServerPath = Environment.CurrentDirectory;
            SkyUtil.log($"Registered Server Path at '{ServerPath}'");
            
            this.Context = context;

            //

            context.PluginManager.LoadCommands(new SkyCommands(this));  //Initialize Generic Commands
            context.PluginManager.LoadCommands(Permissions);            //Initialize Permission Commands

            context.LevelManager.LevelCreated += (sender, args) =>
            {
                Level level = args.Level;
                
                //Override Spawn Point for testing
                if (level.LevelId.Equals("Overworld"))
                {
                    level.SpawnPoint = new PlayerLocation(0D, 36D, 10D, 0f, 0f, 90f);

                    /*for (int x = -5; x < 5; x++)
                    {
                        for (int z = -5; z < 5; z++)
                        {
                            args.Level.SetBlock(x, 99, z, 1); //Set block below spawn point to STONE
                        }
                    }*/

                    level.BlockBreak += LevelOnBlockBreak;
                    level.BlockPlace += LevelOnBlockPlace;
                }
            };

            //Register listeners
            context.Server.PlayerFactory.PlayerCreated += (sender, args) =>
            {
                //SkyPlayer player = (SkyPlayer) args.Player;
                MiNET.Player player = args.Player;

                player.PlayerJoin += OnPlayerJoin;
                player.PlayerLeave += OnPlayerLeave;

				if (PendingTasks.Count > 0)
				{
					foreach (Delegate pendingTask in PendingTasks)
					{
						pendingTask.DynamicInvoke();
					}

					PendingTasks.Clear();
				}

                if (PlayerNPC.PendingNpcs.Count > 0)
                {
                    List<PlayerNPC> pendingNpcClone = new List<PlayerNPC>(PlayerNPC.PendingNpcs);
                    PlayerNPC.PendingNpcs.Clear();

                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        Thread.Sleep(1000);

                        try
                        {
                            foreach (PlayerNPC pendingNpc in pendingNpcClone)
                            {
                                pendingNpc.SpawnEntity();
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    });
                }
            };

            SkyUtil.log("Initialized!");

            ThreadPool.QueueUserWorkItem(state =>
            {
                Thread.Sleep(1000);

                SkyUtil.log("Setting up Custom Games...");

                try
                {
                    _initializeCustomGame(new MurderCoreGameController(this));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
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
            foreach (Level level in Context.LevelManager.Levels)
            {
                foreach (MiNET.Player player in level.Players.Values)
                {
                    //TODO: Improve?
                    player.Disconnect("Server Shutting Down");
                }
            }
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

            player.Inventory.Slots[3] = new ItemMap() {Count = 1};
            player.Inventory.Slots[4] = new ItemItemFrame() {Count = 1};
            player.Inventory.Slots[5] = new ItemEmptyMap() {Count = 1};

            player.SendPlayerInventory();

            ThreadPool.QueueUserWorkItem(state =>
            {
                Thread.Sleep(2000);

                //Group isn't initialized yet - wait.
                SkyUtil.log($"Group for {player.Username} == {player.PlayerGroup} vs {PlayerGroup.Youtuber} == {player.PlayerGroup.CompareTo(PlayerGroup.Youtuber)}");

                GameMode targetGameMode;
                if (player.PlayerGroup.CompareTo(PlayerGroup.Youtuber) >= 0)
                {
                    targetGameMode = GameMode.Creative;
                }
                else
                {
                    targetGameMode = GameMode.Adventure;
                }
                player.SetGameMode(targetGameMode);

                player.SendTitle(null, TitleType.Clear);
                player.SendTitle(null, TitleType.AnimationTimes, 6, 6, 20 * 10);

                player.SendTitle($"{ChatColors.Gold}Welcome {player.Username}\n{ChatColors.LightPurple}~ To the Skytonia Network ~", TitleType.ActionBar);
                Console.WriteLine(" (Joined!)");
                
                /*try
                {
                    SkyUtil.log($"Coming from {player.Level.LevelId}");
                    /*Task.Delay(5000).ContinueWith(t =>
                    {
                        Level gameLevel = new MurderCoreGameController(this).GetGameController().Level;
                        SkyUtil.log($"Travelling to {gameLevel.LevelName} ({gameLevel.LevelId})");
                        
                        player.SpawnLevel(gameLevel, new PlayerLocation(0D, 100D, 0D));
                    });#1#
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }*/
            });
        
        }

        private void OnPlayerLeave(object o, PlayerEventArgs eventArgs)
        {
            if (eventArgs.Level is GameLevel)
            {
                ((GameLevel) eventArgs.Level).RemovePlayer((SkyPlayer) eventArgs.Player);
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
            
            text = TextUtils.RemoveFormatting(text);

            string formattedText = $"{GetNameTag(player)}:{ChatColors.White} {text}";
            SkyUtil.log($"Broadcasting to {player.Level.LevelId}: {formattedText}");
            player.Level.BroadcastMessage(formattedText, MessageType.Raw);

            return null;
        }

        private string GetNameTag(MiNET.Player player)
        {
            string username = player.Username;

            string rank;
            //if (username.StartsWith("gurun") || username.StartsWith("Oliver"))
            //{
            //	rank = $"{ChatColors.Red}[ADMIN]";
            //}
            //else 
            /*if (player.CertificateData.ExtraData.Xuid != null)
            {
                rank = $"{ChatColors.Green}[XBOX]";
            }
            else
            {
                rank = $"{ChatColors.White}";
            }*/

            if (player.GetType() == typeof(SkyPlayer))
            {
                rank = ((SkyPlayer) player).PlayerGroup.Prefix;
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

    }
}
