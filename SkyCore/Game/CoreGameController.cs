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

				List<GameInfo> availableGames = new List<GameInfo>();
				foreach (GameLevel gameLevel in GameLevels.Values)
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

	    public virtual void InstantQueuePlayer(SkyPlayer player)
	    {
			SkyUtil.log($"Trying to add {QueuedPlayers.Count} players to {GameLevels.Count} games");
		    lock (GameLevels)
		    {
			    foreach (GameLevel gameLevel in GameLevels.Values)
			    {
				    if (!gameLevel.CurrentState.CanAddPlayer(gameLevel))
				    {
						QueuePlayer(player);
					    break; //Cannot add any more players
				    }

				    SkyUtil.log($"Adding {player.Username} to game {gameLevel.GameId}-({gameLevel.LevelId}-{gameLevel.LevelName})");
				    gameLevel.AddPlayer(player);
				}
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
			
			//SkyUtil.log("Current Games " + availableGames.Count);

			if (availableGames.Count == 0)
			{
				GetGameController(); //Create a new game for the pool
			}
			//Clean up unnecessary games
			else if (availableGames.Count >= 5)
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

        public void GetGameController()
        {
	        lock (GameLevels)
	        {
		        if (GameLevels.Count >= MaxGames)
		        {
			        return; //Cannot create any more games.
		        }

		        GameLevel gameLevel = _getGameController();

		        if (gameLevel != null)
		        {
			        GameLevels.TryAdd(gameLevel.GameId, gameLevel);
		        }
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
