using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiNET.Utils;
using MiNET.Worlds;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkyCore.Entities;
using SkyCore.Game.Level;
using SkyCore.Game.State;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Game
{
    public abstract class CoreGameController : IDisposable
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

        public int NextGameId;

	    protected CoreGameController(SkyCoreAPI plugin, string gameName, string neatName, List<string> levelNames)
        {
            Plugin = plugin;
            
            GameName = neatName;
            RawName = gameName;

			ExternalGameHandler.RegisterInternalGame(RawName);
            
            foreach(var levelName in levelNames)
            {
                string fullLevelPath = "C:\\Users\\Administrator\\Desktop\\worlds\\" + gameName + "\\" + levelName;

                LevelNames.Add(fullLevelPath);

                SkyUtil.log($"Added world at {fullLevelPath}");
            }

			if (LevelNames.Count == 0)
			{
				SkyUtil.log($"No Levels configured for {gameName}");
			}

            GameTickThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                GameTick = new HighPrecisionTimer(50, _CoreGameTick, true);
            });
            GameTickThread.Start();
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
                Console.WriteLine(e);
            }
        }

		protected int Tick = 1;

        protected virtual void CoreGameTick()
        {
			if (++Tick % 10 == 0)
			{
				CheckCapacity();
			}

            //SkyUtil.log("Ticking Core");
            if (QueuedPlayers.IsEmpty && Tick % 20 != 0)
            {
                return;
            }
	        
            //SkyUtil.log($"Trying to add {QueuedPlayers.Count} players to {GameLevels.Count} games");
			lock (GameLevels)
			{
				InstanceInfo instanceInfo = ExternalGameHandler.GameRegistrations[RawName].GetLocalInstance();
				instanceInfo.CurrentPlayers = 0;

				//string message = "";

				//Show higher player count games first
				List<GameInfo> availableGames = new List<GameInfo>();
				foreach (GameLevel gameLevel in GetMostViableGames())
				{
					/*if (gameLevel.PlayerTeamDict.Count > 0 || gameLevel.GetAllPlayers().Count > 0)
					{
						List<SkyPlayer> players = gameLevel.GetAllPlayers();
						if (players.Count > 0)
						{
							message += gameLevel.GameId + ": " + gameLevel.GetPlayerCount() + " " + gameLevel.GetAllPlayers()[0].Username + " " + gameLevel.PlayerTeamDict.ContainsKey(gameLevel.GetAllPlayers()[0].Username) + " ";
						}
						else
						{
							message += gameLevel.GameId + ": " + gameLevel.GetPlayerCount() + " " + gameLevel.PlayerTeamDict.ContainsKey("OhBlihv") + " ";
						}
					}*/

					//Update player counts
					instanceInfo.CurrentPlayers += gameLevel.GetPlayerCount();
					//SkyUtil.log($"{gameLevel.GameId} Is Available: {gameLevel.CurrentState.CanAddPlayer(gameLevel)}");
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

				/*if (RawName.Equals("hub"))
				{
					if (message.Length == 0)
					{
						SkyUtil.log("No players on Hub");
					}
					else
					{
						SkyUtil.log(message);
					}
				}*/
				
				instanceInfo.AvailableGames = availableGames;
				instanceInfo.Update(); //Set last update time

				//SkyUtil.log($"InstanceInfo at {instanceInfo.CurrentPlayers} with {instanceInfo.AvailableGames}");
			}

            //If we're running out of free slots, create a new game lobby
            if (!QueuedPlayers.IsEmpty)
            {
                SkyUtil.log("Attempting to create a game to satisfy demand");
                //Register a fresh controller
                GetGameController();
            }
        }

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

	    public virtual void InstantQueuePlayer(SkyPlayer player)
	    {
			SkyUtil.log($"Trying to add {player.Username} player to {GameLevels.Count} games");
		    lock (GameLevels)
		    {
			    foreach (GameLevel gameLevel in GetMostViableGames())
			    {
				    if (!gameLevel.CurrentState.CanAddPlayer(gameLevel))
				    {
						QueuePlayer(player);
					    break; //Cannot add any more players
				    }

				    SkyUtil.log($"Adding {player.Username} to game {gameLevel.GameId}-({gameLevel.LevelId}-{gameLevel.LevelName})");
				    gameLevel.AddPlayer(player);
				    break;
			    }
		    }
		}

		public virtual bool InstantQueuePlayer(SkyPlayer player, GameInfo gameInfo)
		{
			SkyUtil.log($"Trying to add {QueuedPlayers.Count} players to {GameLevels.Count} games");
			lock (GameLevels)
			{
				GameLevel targetedGame = GameLevels[gameInfo.GameId];
				if (targetedGame == null || !targetedGame.CurrentState.CanAddPlayer(targetedGame))
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

			while (availableGames.Count < 2)
			{
				GameLevel gameLevel = GetGameController();
				if (gameLevel == null)
				{
					break;
				}
				
				availableGames.Add(gameLevel); //Create a new game for the pool
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

        public GameLevel GetGameController()
        {
	        lock (GameLevels)
	        {
		        if (GameLevels.Count >= MaxGames)
		        {
			        return null; //Cannot create any more games.
		        }

		        GameLevel gameLevel = _getGameController();

		        if (gameLevel != null)
		        {
			        GameLevels.TryAdd(gameLevel.GameId, gameLevel);
		        }

		        return gameLevel;
	        }
        }

        protected abstract GameLevel _getGameController();

        public string GetRandomLevelName()
        {
            return LevelNames[Random.Next(LevelNames.Count)];
        }

        public string GetNextGameId()
        {
            return $"{RawName}{++NextGameId}";
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

    }
}
