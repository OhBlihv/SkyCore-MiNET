using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiNET.Utils;
using MiNET.Worlds;
using SkyCore.Entities;
using SkyCore.Game.State;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Game
{
    public abstract class CoreGameController
    {

        protected readonly Random Random = new Random();
        
        public SkyCoreAPI Plugin { get; }

        public readonly ConcurrentDictionary<string, GameLevel> GameLevels = new ConcurrentDictionary<string, GameLevel>();

        public readonly ConcurrentQueue<SkyPlayer> QueuedPlayers = new ConcurrentQueue<SkyPlayer>();

        protected readonly Thread GameTickThread;
        protected HighPrecisionTimer GameTick;
        
        public readonly List<string> LevelNames = new List<string>();
        
        public string GameName { get; }

        public string RawName { get; }

        public int NextGameId;
        
        public CoreGameController(SkyCoreAPI plugin, string gameName, string neatName, List<string> levelNames)
        {
            Plugin = plugin;
            
            GameName = neatName;
            RawName = gameName;
            
            foreach(var levelName in levelNames)
            {
                //string fullLevelPath = SkyCoreAPI.ServerPath + "\\..\\worlds\\" + gameName + "\\" + levelName;
                string fullLevelPath = "C:\\Users\\Administrator\\Desktop\\worlds\\" + gameName + "\\" + levelName;
                //string fullLevelPath = gameName + "\\" + levelName;

                LevelNames.Add(fullLevelPath);

                SkyUtil.log($"Added world at {fullLevelPath}");
            }

            GameTickThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                GameTick = new HighPrecisionTimer(50, _CoreGameTick, true);
            });
            GameTickThread.Start();
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
                CoreGameTick();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        protected void CoreGameTick()
        {
            //SkyUtil.log("Ticking Core");
            if (QueuedPlayers.IsEmpty)
            {
                return;
            }

            SkyUtil.log($"Trying to add {QueuedPlayers.Count} players to {GameLevels.Count} games");
            foreach (GameLevel gameLevel in GameLevels.Values)
            {
                SkyPlayer nextPlayer;
                while (!QueuedPlayers.IsEmpty)
                {
                    if (gameLevel.CurrentState.CanAddPlayer(gameLevel) && QueuedPlayers.TryDequeue(out nextPlayer))
                    {
                        SkyUtil.log($"Adding {nextPlayer.Username} to game {gameLevel.GameId}-({gameLevel.LevelId}-{gameLevel.LevelName})");
                        gameLevel.AddPlayer(nextPlayer);
                    }
                }
            }

            //If we're running out of free slots, create a new game lobby
            if (!QueuedPlayers.IsEmpty)
            {
                SkyUtil.log("Attempting to create a game to satisfy demand");
                //Register a fresh controller
                GetGameController();

                //Try again
                //CoreGameTick(); //Avoid an infinite loop for now. Wait the extra 499 milliseconds
            }
        }

        public GameLevel GetGameController()
        {
            GameLevel gameLevel = _getGameController();

            if (gameLevel != null)
            {
                GameLevels.TryAdd(gameLevel.GameId, gameLevel);
            }

            return gameLevel;
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

        public void QueuePlayer(SkyPlayer player)
        {
            if (!QueuedPlayers.Contains(player))
            {
                QueuedPlayers.Enqueue(player);
            }
        }

    }
}
