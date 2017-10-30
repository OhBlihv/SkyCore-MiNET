using MiNET;
using MiNET.Effects;
using MiNET.Entities;
using MiNET.Items;
using MiNET.UI;
using MiNET.Utils;
using MiNET.Worlds;
using Newtonsoft.Json;
using SkyCore.Game.State;
using SkyCore.Player;
using SkyCore.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading;
using MiNET.Net;
using SkyCore.Entities;

namespace SkyCore.Game.Level
{
	public abstract class GameLevel : MiNET.Worlds.Level, IDisposable, IComparable<GameLevel>
    {

	    public new string LevelName { get; }

		public delegate void PlayerAction(SkyPlayer player);

	    private readonly IDictionary<string, long> _incomingPlayers = new Dictionary<string, long>();

	    public void AddIncomingPlayer(string username)
	    {
			_incomingPlayers.Add(username, DateTimeOffset.Now.ToUnixTimeSeconds() + 15);
	    }

	    //Player -> Team
        public readonly Dictionary<string, GameTeam> PlayerTeamDict = new Dictionary<string, GameTeam>();

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

	    protected GameLevel(SkyCoreAPI plugin, string gameType, string gameId, String levelPath, GameLevelInfo gameLevelInfo, bool modifiable = false)
                : base(plugin.Context.LevelManager, gameId, 
					AnvilProviderFactory.GetLevelProvider(plugin.Context.LevelManager, levelPath, modifiable, true, !modifiable),
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
	        GameLevelInfo = gameLevelInfo;

	        EnableBlockTicking = false;
            EnableChunkTicking = false;

            SkyUtil.log($"Initializing world {gameId}");
            Initialize();

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
	        
	        //Spawn Lobby NPC
	        PlayerNPC.SpawnLobbyNPC(this, "§eChange Game", new PlayerLocation(260.5, 10, 252.5));
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
            return PlayerTeamDict.Count + _incomingPlayers.Count;
        }

	    public int GetGamePlayerCount()
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

	            /*
				 * Clean up any 'incoming players' who never showed up
				 * '15 seconds' to appear before they are cleared
				 */
	            if (_incomingPlayers.Count > 0)
	            {
		            long currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();
					foreach (KeyValuePair<string, long> entry in _incomingPlayers)
		            {
			            //entry.Value starts off as 15 seconds ahead of UNIX time
			            if (entry.Value - currentTime < 0)
			            {
				            _incomingPlayers.Remove(entry.Key);
			            }
		            }
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
	        if (player.Level != this && player.Level is GameLevel)
	        {
		        player.Level.RemovePlayer(player); //Clear from old world
	        }
	        
	        if (_incomingPlayers.ContainsKey(player.Username))
	        {
		        _incomingPlayers.Remove(player.Username);
	        }
	        
            GameTeam defaultTeam = GetDefaultTeam();

			SetPlayerTeam(player, defaultTeam);
            SkyUtil.log($"Added {player.Username} to team {defaultTeam.DisplayName} in game {GameId}");

            player.SpawnLevel(this, GameLevelInfo.LobbyLocation, false);

			CurrentState.InitializePlayer(this, player);

			McpeGameRulesChanged gameRulesChanged = McpeGameRulesChanged.CreateObject();
	        gameRulesChanged.rules = GetGameRules();
	        
	        player.SendPackage(gameRulesChanged);
		}
        
        public new void RemovePlayer(MiNET.Player player, bool removeFromWorld = false)
        {
	        SkyUtil.log($"Attempting to remove {player.Username} from {GameId}");
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

		        //player.SpawnLevel(level, level.SpawnPoint, true);
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
                else
                {
					//Respawn if this player is missing
	                if (player.IsGameSpectator)
	                {
		                List<MiNET.Player> gamePlayers = new List<MiNET.Player>();
		                DoForAllPlayers(gamePlayer =>
		                {
			                if (!gamePlayer.IsGameSpectator)
			                {
				                gamePlayers.Add(gamePlayer);
			                }
		                });

		                SkyUtil.log(
			                $"Spawning {player.Username} to {string.Join(",", gamePlayers.Select(x => x.ToString()).ToArray())}");
		                player.SpawnToPlayers(gamePlayers.ToArray());
	                }

	                player.IsGameSpectator = false;
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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="player"></param>
		/// <param name="item"></param>
		/// <returns>Whether to cancel the drop. true = cancelled</returns>
		public virtual bool DropItem(SkyPlayer player, Item item)
	    {
		    return true;
	    }

	    /// <summary>
		/// 
		/// </summary>
		/// <param name="player"></param>
		/// <param name="item"></param>
		/// <returns>Whether to cancel the pickup. true = cancelled</returns>
	    public virtual bool PickupItem(SkyPlayer player, Item item)
	    {
		    return true;
	    }

	    //

        public abstract GameState GetInitialState();

        public abstract GameTeam GetDefaultTeam();
	    
        public abstract GameTeam GetSpectatorTeam(); 

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

        public virtual void AddSpectator(SkyPlayer player)
        {
			player.IsGameSpectator = true;

			List<MiNET.Player> gamePlayers = new List<MiNET.Player>();
			DoForAllPlayers(gamePlayer =>
			{
				if (!gamePlayer.IsGameSpectator)
				{
					gamePlayers.Add(gamePlayer);
				}
			});

			SkyUtil.log($"Despawning {player.Username} from {string.Join(",", gamePlayers.Select(x => x.ToString()).ToArray())}");
			player.DespawnFromPlayers(gamePlayers.ToArray());

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

		// Forms

	    public abstract string GetEndOfGameContent(SkyPlayer player);

	    public abstract string GetGameModalTitle();

	    public void ShowEndGameMenu(SkyPlayer player)
	    {
			var simpleForm = new SimpleForm
		    {
			    Title = GetGameModalTitle(),
			    Content = GetEndOfGameContent(player),
			    Buttons = new List<Button>
			    {
				    new Button
				    {
					    Text = "§2§lPlay Again\n" +
							   "§r§8(Jump into a new game)",
					    Image = new Image
					    {
						    Type = "url",
						    Url = "https://static.skytonia.com/dl/replayicon.png"
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
						Text = "§6§lSpectate Game\n" +
							   "§r§8(Continue watching this game)",
						Image = new Image
						{
							Type = "url",
							Url = "https://static.skytonia.com/dl/spectateicon.png"
						},
						ExecuteAction = delegate { SetPlayerTeam(player, GetSpectatorTeam()); }
					}
				);
			}

		    simpleForm.Buttons.Add(
			    new Button
			    {
					Text = "§c§lChange Game\n" +
						   "§r§8(Choose a different game)",
				    Image = new Image
				    {
					    Type = "url",
					    Url = "https://static.skytonia.com/dl/comingsoonicon.png"
					},
					ExecuteAction = delegate { GameUtil.ShowGameList(player); }
			    }
			);

			player.SendForm(simpleForm);
		}

	    public int CompareTo(GameLevel other)
	    {
			int result = 0 - Math.Sign(GetPlayerCount().CompareTo(other.GetPlayerCount()));
		    
		    if (result == 0)
		    {
			    return 1;
		    }
		    
		    return result;
	    }

		public override GameRules GetGameRules()
		{
			GameRules rules = new GameRules
			{
				new GameRule<bool>(GameRulesEnum.DrowningDamage, false),
				new GameRule<bool>(GameRulesEnum.CommandblockOutput, false),
				new GameRule<bool>(GameRulesEnum.DoTiledrops, false),
				new GameRule<bool>(GameRulesEnum.DoMobloot, false),
				new GameRule<bool>(GameRulesEnum.KeepInventory, false),
				new GameRule<bool>(GameRulesEnum.DoDaylightcycle, false),
				new GameRule<bool>(GameRulesEnum.DoMobspawning, false),
				new GameRule<bool>(GameRulesEnum.DoEntitydrops, false),
				new GameRule<bool>(GameRulesEnum.DoFiretick, false),
				new GameRule<bool>(GameRulesEnum.DoWeathercycle, false),
				new GameRule<bool>(GameRulesEnum.Pvp, false),
				new GameRule<bool>(GameRulesEnum.Falldamage, false),
				new GameRule<bool>(GameRulesEnum.Firedamage, false),
				new GameRule<bool>(GameRulesEnum.Mobgriefing, false),
				new GameRule<bool>(GameRulesEnum.ShowCoordinates, false),
				new GameRule<bool>(GameRulesEnum.NaturalRegeneration, false),
				new GameRule<bool>(GameRulesEnum.TntExploads, false),
				new GameRule<bool>(GameRulesEnum.SendCommandfeedback, false)
			};
			return rules;
		}
	}
	
    
}
