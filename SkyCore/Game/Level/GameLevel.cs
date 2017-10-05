using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Threading;
using MiNET;
using MiNET.Effects;
using MiNET.Entities;
using MiNET.Items;
using MiNET.Net;
using MiNET.UI;
using MiNET.Utils;
using MiNET.Worlds;
using Newtonsoft.Json;
using SkyCore.Game.Level;
using SkyCore.Game.State;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Game
{
    public abstract class GameLevel : MiNET.Worlds.Level, IDisposable
    {

	    public new string LevelName { get; }

		public delegate void PlayerAction(SkyPlayer player);

        //Player -> Team
        protected readonly Dictionary<string, GameTeam> PlayerTeamDict = new Dictionary<string, GameTeam>();

        //Team -> Player(s) //TODO: Possibly remove due to complexity?
        protected readonly Dictionary<GameTeam, List<SkyPlayer>> TeamPlayerDict = new Dictionary<GameTeam, List<SkyPlayer>>();

	    public GameLevelInfo GameLevelInfo { get; protected set; }

        //

        public SkyCoreAPI Plugin { get; }

		public string GameType { get; }

        public string GameId { get; }

        public GameState CurrentState { get; private set; }

        protected readonly Thread GameLevelTickThread;
        protected HighPrecisionTimer GameLevelTick;

        public int Tick { get; private set; }

        //

	    protected GameLevel(SkyCoreAPI plugin, string gameType, string gameId, String levelPath, bool modifiable = false)
                //: base(plugin.Context.LevelManager, gameId, AnvilProviderFactory.GetLevelProvider(plugin.Context.LevelManager, levelPath),
                : base(plugin.Context.LevelManager, gameId, 
					AnvilProviderFactory.GetLevelProvider(plugin.Context.LevelManager, levelPath, modifiable),
					//new AnvilWorldProvider(levelPath), 
                      plugin.Context.LevelManager.EntityManager, GameMode.Creative)
        {
	        string levelName;
	        {
		        string[] split = levelPath.Split('\\');
		        levelName = split[split.Length - 1];
	        }

	        LevelName = levelName;

			Plugin = plugin;
            GameId = gameId;
	        GameType = gameType;

	        GameLevelInfo = new GameLevelInfo(gameType, LevelName, new PlayerLocation(266, 11, 256));
	        {
		        GameLevelInfo gameLevelInfo = LoadThisLevelInfo();
		        if (gameLevelInfo != null)
		        {
			        GameLevelInfo = gameLevelInfo;
		        }
	        }

	        EnableBlockTicking = false;
            EnableChunkTicking = false;

            SkyUtil.log($"Initializing world {gameId}");
            Initialize();

			//TODO: Aren't required?
            //((AnvilWorldProvider)WorldProvider).PruneAir();
            //((AnvilWorldProvider)WorldProvider).MakeAirChunksAroundWorldToCompensateForBadRendering();

            if (!plugin.Context.LevelManager.Levels.Contains(this))
            {
                SkyUtil.log($"Adding {gameId} to Main LevelManager");
                plugin.Context.LevelManager.Levels.Add(this);
            }

            InitializeTeamMap();

            CurrentState = GetInitialState();
            CurrentState.EnterState(this);

            GameLevelTickThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                GameLevelTick = new HighPrecisionTimer(500, PreGameTick, true);
            });
            GameLevelTickThread.Start();

			BlockBreak += HandleBlockBreak;
			BlockPlace += HandleBlockPlace;
        }

		protected virtual void HandleBlockPlace(object sender, BlockPlaceEventArgs e)
		{
			e.Cancel = CurrentState.HandleBlockPlace(this, e.Player as SkyPlayer, e.ExistingBlock, e.TargetBlock);
		}

	    protected virtual void HandleBlockBreak(object sender, BlockBreakEventArgs e)
		{
			e.Cancel = CurrentState.HandleBlockBreak(this, e.Player as SkyPlayer, e.Block, e.Drops);
		}

		protected abstract void InitializeTeamMap();

	    public void Dispose()
	    {
		    Close();
		}

		public override void Close()
        {
            GameLevelTick.Dispose();
            GameLevelTickThread.Abort();

			DoForAllPlayers(player =>
			{
				RemovePlayer(player);
			});

			base.Close();

	        Plugin.Context.LevelManager.Levels.Remove(this);
        }

        public int GetPlayerCount()
        {
            return PlayerTeamDict.Count;
        }

	    public new List<SkyPlayer> GetAllPlayers()
	    {
		    return GetPlayers();
	    }

        public List<SkyPlayer> GetPlayers()
        {
            List<SkyPlayer> allPlayers = new List<SkyPlayer>();
            foreach (List<SkyPlayer> teamPlayers in TeamPlayerDict.Values)
            {
                allPlayers.AddRange(teamPlayers);
            }

            return allPlayers;
        }

        public List<SkyPlayer> GetPlayersInTeam(params GameTeam[] teams)
        {
            List<SkyPlayer> playerList = null;
            if (teams.Length > 1)
            {
                playerList = new List<SkyPlayer>();
            }

            foreach (GameTeam team in teams)
            {
	            if (!TeamPlayerDict.ContainsKey(team))
	            {
		            continue; //Ignore if invalid
	            }

                if (playerList == null)
                {
                    playerList = TeamPlayerDict[team];
                }
                else
                {
                    playerList.AddRange(TeamPlayerDict[team]);
                }
            }

            return playerList;
        }

        public void DoForPlayersIn(PlayerAction action, params GameTeam[] teams)
        {
            foreach (SkyPlayer player in GetPlayersInTeam(teams))
            {
                action(player);
            }
        }

        public void DoForAllPlayers(PlayerAction action)
        {
            foreach (SkyPlayer player in GetPlayers())
            {
                action(player);
            }
        }

        protected void PreGameTick(object sender)
        {
            try
            {
                int tick = Tick;

                GameTick(++tick);

                CurrentState.OnTick(this, tick, out tick);

				//Process player action/popup bars
	            foreach (SkyPlayer player in GetAllPlayers())
	            {
		            player.BarHandler.DoTick();
	            }

                Tick = tick; //Workaround?
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

		public void AddPlayer(SkyPlayer player)
        {
            GameTeam defaultTeam = GetDefaultTeam();

			SetPlayerTeam(player, defaultTeam);
            SkyUtil.log($"Added {player.Username} to team {defaultTeam.DisplayName}");

            //player.SpawnLevel(this, new PlayerLocation(7.5, 181, -20.5));
            //player.SpawnLevel(this, new PlayerLocation(255, 70, 255));
            player.SpawnLevel(this, GameLevelInfo.LobbyLocation);

			CurrentState.InitializePlayer(this, player);
        }
        
        public new void RemovePlayer(MiNET.Player player, bool removeFromWorld = false)
        {
	        if (((SkyPlayer) player).GameTeam == null)
	        {
				return; //Shouldn't be in the/any game.
	        }

	        CurrentState.HandleLeave(this, (SkyPlayer)player);

	        PlayerTeamDict.TryGetValue(player.Username, out var gameTeam);

			if (gameTeam != null)
	        {
		        PlayerTeamDict.Remove(player.Username);
		        TeamPlayerDict[gameTeam].Remove((SkyPlayer) player);
	        }

			//Enforce removing the attached team
	        ((SkyPlayer) player).GameTeam = null;

			player.RemoveAllEffects();

		
			base.RemovePlayer(player); //Remove player from the 'world'

	        if (removeFromWorld && player.Level == this)
	        {
		        MiNET.Worlds.Level level = SkyCoreAPI.Instance.GetHubLevel();
		        if (level == null)
		        {
			        player.Disconnect("Unable to send you back to the hub!");
			        return;
		        }

		        player.SpawnLevel(level, level.SpawnPoint);
			}
        }

        public GameTeam GetPlayerTeam(SkyPlayer player)
        {
            if (PlayerTeamDict.ContainsKey(player.Username))
            {
                return PlayerTeamDict[player.Username];
            }

            return GetDefaultTeam();
        }

        public void SetPlayerTeam(SkyPlayer player, GameTeam team)
        {
            if (player == null)
            {
                throw new ArgumentException();
            }

            GameTeam oldTeam = null;

            if (PlayerTeamDict.ContainsKey(player.Username))
            {
                oldTeam = PlayerTeamDict[player.Username];

                if (team != null)
                {
                    PlayerTeamDict[player.Username] = team;
                }
            }
            else if(team != null)
            {
                PlayerTeamDict.Add(player.Username, team);
            }

	        player.GameTeam = team; //Attach to the player

            SkyUtil.log($"Updating {player.Username}'s team from {(oldTeam == null ? "null" : oldTeam.DisplayName)} to {(team == null ? "null" : team.DisplayName)}");

            SetPlayerTeam(player, oldTeam, team);
        }

        public virtual void SetPlayerTeam(SkyPlayer player, GameTeam oldTeam, GameTeam team)
        {
            if (oldTeam != null)
            {
                TeamPlayerDict[oldTeam].Remove(player);
            }

            if (team != null)
            {
                TeamPlayerDict[team].Add(player);

                if (team.IsSpectator)
                {
                    AddSpectator(player);
                }
            }
        }

        public void UpdateGameState(GameState gameState)
        {
            CurrentState.LeaveState(this);

            CurrentState = gameState;

            CurrentState.EnterState(this);
        }

        //Returns true if default behaviour should not occur (effectively cancelled)
        public bool DoInteract(int interactId, SkyPlayer player, SkyPlayer target)
        {
            return CurrentState.DoInteract(this, interactId, player, target);
        }

        //

        public abstract GameState GetInitialState();

        public abstract GameTeam GetDefaultTeam();

        public abstract int GetMaxPlayers();

        public abstract void GameTick(int tick);

        public void HandleDamage(Entity source, Entity target, Item item, int damage, DamageCause damageCause)
        {
            if (!(target is SkyPlayer))
            {
                return;
            }

            CurrentState.HandleDamage(this, source, target, item, damage, damageCause);
        }

        public void AddSpectator(SkyPlayer player)
        {
            player.SetEffect(new Invisibility
            {
                Duration = int.MaxValue,
                Particles = false
            });

			player.SetEffect(new Blindness
			{
				Duration = 100,
				Particles = false
			});

			player.SetAllowFly(true);
            player.IsFlying = true;

            //Bump the player up into the air to signify death
            player.Knockback(new Vector3(0f, 0.5f, 0f));
        }

	    // JSON

	    public abstract Type GetGameLevelInfoType();

		public GameLevelInfo LoadThisLevelInfo()
	    {
		    try
		    {
			    string levelInfoFilename =
				    $"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\\config\\{GameType}-{LevelName}.json";

			    if (File.Exists(levelInfoFilename))
			    {
				    SkyUtil.log($"Found '{levelInfoFilename}' for level. Loading...");

				    //return JsonConvert.DeserializeObject(File.ReadAllText(levelInfoFilename), new GameLevelInfoConverter());
				    GameLevelInfo gameLevelInfo = (GameLevelInfo)JsonConvert.DeserializeObject(File.ReadAllText(levelInfoFilename), GetGameLevelInfoType());

				    SkyUtil.log("Returning of type " + gameLevelInfo.GetType());

				    return gameLevelInfo;
			    }
			    else
			    {
				    SkyUtil.log($"Could not find '{levelInfoFilename} for level. Not loading.");
			    }

			    return null;
			}
		    catch (Exception e)
		    {
			    Console.WriteLine(e);
			    return null;
		    }
	    }

		// Forms

	    public void ShowEndGameMenu(SkyPlayer player)
	    {
			var simpleForm = new SimpleForm
		    {
			    Title = "Game Finished",
			    Content = "Play Again?",
			    Buttons = new List<Button>
			    {
				    new Button
				    {
					    Text = "Play Again",
					    Image = new Image
					    {
						    Type = "url",
						    //Url = "https://cdn.discordapp.com/attachments/192533470608621570/363942495329189889/1031826.png"
						    Url = "https://cdn.discordapp.com/attachments/192533470608621570/363945144992399362/TestMiNetIcon.png"
						},
					    ExecuteAction = delegate { ExternalGameHandler.AddPlayer(player, GameType); }
				    }
			    }
		    };

		    StateType currentStateType = CurrentState.GetEnumState(this);
			if (currentStateType != StateType.EndGame && 
				currentStateType != StateType.Closing)
		    {
				simpleForm.Buttons.Add(
					new Button
					{
						Text = "Spectate",
						//ExecuteAction = delegate { ExternalGameHandler.AddPlayer((SkyPlayer) player, "build-battle"); }
					}
				);
			}

		    simpleForm.Buttons.Add(
			    new Button
			    {
				    Text = "Change Game",
				    ExecuteAction = delegate { ShowGameList(player); }
			    }
			);

		    simpleForm.Buttons.Add(
			    new Button
			    {
				    Text = "Return to Hub",
				    ExecuteAction = delegate { ExternalGameHandler.AddPlayer(player, "hub"); }
			    }
			);

			player.SendForm(simpleForm);
		}

	    public void ShowGameList(SkyPlayer player)
	    {
			var simpleForm = new SimpleForm
		    {
			    Title = "Game list",
				Content = "",
			    Buttons = new List<Button>
			    {
				    new Button
				    {
					    Text = "Murder",
					    Image = new Image
					    {
						    Type = "url",
						    Url = "https://cdn.discordapp.com/attachments/192533470608621570/363945144992399362/TestMiNetIcon.png"
					    },
					    ExecuteAction = delegate { ExternalGameHandler.AddPlayer(player, "murder"); }
				    },
				    new Button
				    {
					    Text = "Build Battle",
					    Image = new Image
					    {
						    Type = "url",
						    Url = "https://cdn.discordapp.com/attachments/192533470608621570/363945144992399362/TestMiNetIcon.png"
					    },
					    ExecuteAction = delegate { ExternalGameHandler.AddPlayer(player, "build-battle"); }
				    },
				    new Button
				    {
					    Text = "Return to Hub",
					    Image = new Image
					    {
						    Type = "url",
						    Url = "https://cdn.discordapp.com/attachments/192533470608621570/363945144992399362/TestMiNetIcon.png"
					    },
					    ExecuteAction = delegate { ExternalGameHandler.AddPlayer(player, "hub"); }
				    }
				}
		    };

		    player.SendForm(simpleForm);
		}

    }
    
}
