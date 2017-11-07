using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading;
using Bugsnag;
using MiNET.Net;
using MiNET.Particles;
using MiNET.Plugins.Attributes;
using MiNET.Utils;
using Newtonsoft.Json;
using SkyCore.BugSnag;
using SkyCore.Game.Level;
using SkyCore.Game.State;
using SkyCore.Game.State.Impl;
using SkyCore.Games.Murder.Level;
using SkyCore.Games.Murder.State;
using SkyCore.Player;
using SkyCore.Util;
using StackExchange.Redis;

namespace SkyCore.Game
{
    public abstract class GameController : IDisposable, IBugSnagMetadatable
    {

	    public const int MaxGames = 30;

        protected static readonly Random Random = new Random();
        
        public SkyCoreAPI Plugin { get; }

        public readonly ConcurrentDictionary<string, GameLevel> GameLevels = new ConcurrentDictionary<string, GameLevel>();

        public readonly ConcurrentQueue<SkyPlayer> QueuedPlayers = new ConcurrentQueue<SkyPlayer>();

        protected readonly Thread GameTickThread;
        protected HighPrecisionTimer GameTick;
        
        public readonly List<string> LevelNames = new List<string>();
        
        public string GameName { get; }

        public string RawName { get; }

	    private string RedisGameIdKey { get; }

	    protected GameController(SkyCoreAPI plugin, string gameName, string neatName, List<string> levelNames)
        {
            Plugin = plugin;
            
            GameName = neatName;
            RawName = gameName;
            
            foreach(var levelName in levelNames)
            {
                string fullLevelPath = "C:\\Users\\Administrator\\Desktop\\worlds\\" + gameName + "\\" + levelName;
	            if (File.Exists(fullLevelPath))
	            {
					SkyUtil.log($"Unable to find world at ({fullLevelPath})");
				}
	            else
	            {
					LevelNames.Add(fullLevelPath);

		            SkyUtil.log($"Added world at ({fullLevelPath})");

		            //Pre-load GameLevelInfo
		            LoadGameLevelInfo(levelName);
	            }
            }

			if (LevelNames.Count == 0)
			{
				SkyUtil.log($"No Levels configured for {gameName}");
				return;
			}

			RedisGameIdKey = $"next_game_id_{GameName}";

	        ExternalGameHandler.RegisterInternalGame(RawName);

			GameTickThread = new Thread(() =>
	        {
		        Thread.CurrentThread.IsBackground = true;

		        GameTick = new HighPrecisionTimer(50, _CoreGameTick, true);
	        });
	        GameTickThread.Start();
		}

	    public virtual void PostLaunchTask()
	    {
		    
		}

	    public void Dispose()
	    {
		    Close();
	    }

        public void Close()
        {
            GameTick.Dispose();
            GameTickThread.Abort();
        }

        private void _CoreGameTick(object sender)
        {
            try
            {
	            if (SkyCoreAPI.IsRebootQueued)
	            {
		            //Disable game-creation ticks
		            Close();
		            return;
	            }
	            
                CoreGameTick();
            }
            catch (Exception e)
            {
	            BugSnagUtil.ReportBug(this, e);
			}
        }

		protected int Tick = 1;

        protected virtual void CoreGameTick()
        {
			if (++Tick % 10 == 0)
			{
				CheckCapacity();
			}

            if (QueuedPlayers.IsEmpty && Tick % 20 != 0)
            {
                return;
            }
	        
			lock (GameLevels)
			{
				InstanceInfo instanceInfo = ExternalGameHandler.GameRegistrations[RawName].GetLocalInstance();
				instanceInfo.CurrentPlayers = 0;

				//Show higher player count games first
				List<GameInfo> availableGames = new List<GameInfo>();
				foreach (GameLevel gameLevel in GetMostViableGames())
				{
					//Update player counts
					instanceInfo.CurrentPlayers += gameLevel.GetPlayerCount();
					if (gameLevel.CurrentState.CanAddPlayer(gameLevel))
					{
						availableGames.Add(new GameInfo(gameLevel.GameId, gameLevel.GetPlayerCount(), gameLevel.GetMaxPlayers()));
					}

					while (!QueuedPlayers.IsEmpty)
					{
						if (!gameLevel.CurrentState.CanAddPlayer(gameLevel) || !QueuedPlayers.TryDequeue(out var nextPlayer))
						{
							break; //Cannot add any more players
						}

						//Need to find something that indicates this player is loaded
						if (nextPlayer.Level != null)
						{
							SkyUtil.log($"Adding {nextPlayer.Username} to game {gameLevel.GameId}-({gameLevel.LevelId}-{gameLevel.LevelName})");
							gameLevel.AddPlayer(nextPlayer);
						}
						else
						{
							//Re-queue this player :(
							QueuedPlayers.Enqueue(nextPlayer);
						}
					}
				}
				
				instanceInfo.AvailableGames = availableGames;
				instanceInfo.Update(); //Set last update time
			}

            //If we're running out of free slots, create a new game lobby
            if (!QueuedPlayers.IsEmpty)
            {
                SkyUtil.log("Attempting to create a game to satisfy demand");
                //Register a fresh controller
                InitializeNewGame();
            }
        }

		// JSON
	    
	    private readonly IDictionary<string, GameLevelInfo> _cachedGameLevelInfos = new ConcurrentDictionary<string, GameLevelInfo>();

		public virtual GameLevelInfo GetGameLevelInfo(string levelName)
	    {
		    if (_cachedGameLevelInfos.TryGetValue(levelName, out var gameLevelInfo))
		    {
			    return gameLevelInfo;
		    }
		    
			gameLevelInfo = LoadGameLevelInfo(levelName) ?? new GameLevelInfo(RawName, levelName, 6000, new PlayerLocation(255.5, 11, 268.5, 180, 180));

		    _cachedGameLevelInfos.Add(levelName, gameLevelInfo);

		    return gameLevelInfo;
	    }

	    public static string GetGameLevelInfoLocation(string rawGameName, string levelName)
	    {
			return $@"C:\Users\Administrator\Desktop\worlds\{rawGameName}\{rawGameName}-{levelName}.json";
		}

	    public GameLevelInfo LoadGameLevelInfo(string levelName)
	    {
		    try
		    {
			    string shortLevelName;
			    {
				    string[] levelNameSplit = levelName.Split('\\');

				    shortLevelName = levelNameSplit[levelNameSplit.Length - 1];
			    }

			    string levelInfoFilename = GetGameLevelInfoLocation(RawName, shortLevelName);

				if (File.Exists(levelInfoFilename))
			    {
				    SkyUtil.log($"Found '{levelInfoFilename}' for level. Loading...");

				    GameLevelInfo gameLevelInfo = (GameLevelInfo) JsonConvert.DeserializeObject(File.ReadAllText(levelInfoFilename),
					    GetGameLevelInfoType(), new GameLevelInfoJsonConverter());

					//Forcefully update/recalculate BlockCoordinates in-case the configuration was changed without modifying the Distance Property
					gameLevelInfo.LobbyMapLocation = new BlockCoordinates(gameLevelInfo.LobbyMapLocation.X, gameLevelInfo.LobbyMapLocation.Y, gameLevelInfo.LobbyMapLocation.Z);

					return gameLevelInfo;
			    }

				SkyUtil.log($"Could not find '{levelInfoFilename} for level. Not loading.");

				return null;
		    }
		    catch (Exception e)
		    {
			    Console.WriteLine(e);
			    return null;
		    }
	    }
	    
	    // Games

		public virtual SortedSet<GameLevel> GetMostViableGames()
	    {
			SortedSet<GameLevel> mostViableGames = new SortedSet<GameLevel>();

			foreach (GameLevel gameLevel in GameLevels.Values)
			{
				if (gameLevel.CurrentState.GetEnumState(gameLevel).IsJoinable())
				{
					mostViableGames.Add(gameLevel);
				}
			}

		    return mostViableGames;
	    }

		private bool _isFirstLevelRetrieve = true;

	    public virtual GameLevel InstantQueuePlayer(SkyPlayer player, bool join = true)
	    {
		    if (player == null)
		    {
			    if (_isFirstLevelRetrieve)
			    {
				    _isFirstLevelRetrieve = false;

				    //Get Next. Should be used for join.
					return GameLevels.Values.GetEnumerator().Current; 
				}
			    
			    SkyUtil.log("Attempted to pass null SkyPlayer to InstantQueuePlayer. Bad Join?");
			    return null;
		    }
		    
			SkyUtil.log($"Trying to add {player.Username} player to {GameLevels.Count} games");
		    lock (GameLevels)
		    {
			    foreach (GameLevel gameLevel in GetMostViableGames())
			    {
				    if (!gameLevel.CurrentState.CanAddPlayer(gameLevel))
				    {
					    //Player shouldn't be here if no games are accessible
						ExternalGameHandler.AddPlayer(player, "hub");
					    return null;
				    }

				    if (join)
				    {
						SkyUtil.log($"Adding {player.Username} to game {gameLevel.GameId}-({gameLevel.LevelId}-{gameLevel.LevelName})");
						gameLevel.AddPlayer(player);
					}

				    return gameLevel;
			    }
		    }

		    return null;
	    }

		public virtual bool InstantQueuePlayer(SkyPlayer player, GameInfo gameInfo)
		{
			SkyUtil.log($"Trying to add {QueuedPlayers.Count} players to {GameLevels.Count} games");
			lock (GameLevels)
			{
				if (!GameLevels.TryGetValue(gameInfo.GameId, out var targetedGame) || !targetedGame.CurrentState.CanAddPlayer(targetedGame))
				{
					return false;
				}

				SkyUtil.log($"(TARGETED) Adding {player.Username} to game {targetedGame.GameId}-({targetedGame.LevelId}-{targetedGame.LevelName})");
				targetedGame.AddPlayer(player);

				return true;
			}
		}

		///<summary>Ensures at least 1 game is available to join. If not, creates one to fill capacity</summary>
		public virtual void CheckCapacity()
		{
			List<GameLevel> availableGames = new List<GameLevel>();
			List<GameLevel> closingGames   = new List<GameLevel>();
			lock (GameLevels)
			{
				foreach (GameLevel gameLevel in GameLevels.Values)
				{
					if (gameLevel.CurrentState.CanAddPlayer(gameLevel))
					{
						availableGames.Add(gameLevel);
					}
					else if (gameLevel.CurrentState.GetEnumState(gameLevel) == StateType.Closing)
					{
						closingGames.Add(gameLevel);
					}
				}
			}

			lock (GameLevels)
			{
				foreach (GameLevel gameLevel in closingGames)
				{
					SkyUtil.log($"Closing game {gameLevel.GameId}...");
					gameLevel.Close();

					GameLevels.TryRemove(gameLevel.GameId, out _);
				}
			}

			int j = 0;
			while (j++ + availableGames.Count < 2)
			{
				InitializeNewGame(); //Cannot add the new games to the available games list this tick.
			}
			
			//Clean up unnecessary games
			if (availableGames.Count >= 5)
			{
				lock (GameLevels)
				{
					//Start at the most recently created game
					for (int i = availableGames.Count - 1; i >= 0; i--)
					{
						GameLevel gameLevel = availableGames[i];

						gameLevel.Close();

						if (!GameLevels.TryRemove(gameLevel.GameId, out _))
						{
							SkyUtil.log($"Failed to remove game id:{gameLevel.GameId} from GameLevels");
						}
					}
				}
			}
		}

        public void InitializeNewGame()
        {
			RunnableTask.RunTask(() =>
			{
				lock (GameLevels)
				{
					if (GameLevels.Count >= MaxGames)
					{
						return; //Cannot create any more games.
					}

					GameLevel gameLevel = _initializeNewGame();

					if (gameLevel != null)
					{
						GameLevels.TryAdd(gameLevel.GameId, gameLevel);
					}
				}
			});
		}

	    public GameLevel InitializeNewGame(string levelName)
	    {
		    if (!LevelNames.Contains(levelName))
		    {
			    return null;
		    }

			GameLevel gameLevel = _initializeNewGame(levelName);

		    if (gameLevel != null)
		    {
			    GameLevels.TryAdd(gameLevel.GameId, gameLevel);
		    }

		    return gameLevel;
	    }

	    protected abstract GameLevel _initializeNewGame();

	    protected abstract GameLevel _initializeNewGame(string levelName);

        public virtual string GetRandomLevelName() //TODO: Override and select games fairly, to not use the same small pool of maps
        {
            return LevelNames[Random.Next(LevelNames.Count)];
        }

	    private readonly TimeSpan _defaultRedisTimestamp = TimeSpan.FromHours(96); //4 Days. In-case a game goes idle, it won't reset.
	    private const int MaxGameIdVal = 99999; //Stop at 5 characters, reset to 1

		public virtual string GetNextGameId()
        {
	        IDatabase redis = ExternalGameHandler.RedisPool.GetDatabase();

	        int nextGameId = 1;
			RedisValue nextGameIdVal = redis.StringGet(RedisGameIdKey);
	        if (nextGameIdVal.HasValue && int.TryParse(nextGameIdVal, out var nextGameIdResult))
	        {
		        nextGameId = nextGameIdResult;
				SkyUtil.log($"Loaded Redis Cached GameId As {nextGameId}");
			}
	        else
	        {
				SkyUtil.log($"No Redis Cached Value Found. Using {nextGameId}");
			}

			if (nextGameId + 1 > MaxGameIdVal)
			{
				nextGameId = 0;
			}

			redis.StringSet(RedisGameIdKey, nextGameId + 1, _defaultRedisTimestamp);

			return $"{RawName}{nextGameId}";
        }

        public virtual void QueuePlayer(SkyPlayer player)
        {
			if (player == null)
			{
				return;
			}

            if (!QueuedPlayers.Contains(player))
            {
                QueuedPlayers.Enqueue(player);
            }
        }

	    public abstract Type GetGameLevelInfoType();

		/*
		 * Commands
		 */

	    public abstract bool HandleGameEditCommand(SkyPlayer player, GameLevel gameLevel, GameLevelInfo gameLevelInfo, params string[] args);

	    public abstract string GetGameEditCommandHelp(SkyPlayer player);

	    public void PopulateMetadata(Metadata metadata)
	    {
		    metadata.AddToTab("GameController", "GameName", RawName);
		    metadata.AddToTab("GameController", "Registered Levels", LevelNames);
			metadata.AddToTab("GameController", "Active Games", GameLevels);
			metadata.AddToTab("GameController", "Queued Players", QueuedPlayers);
			metadata.AddToTab("GameController", "Next Redis Game Id", RedisGameIdKey);
	    }
    }
}
