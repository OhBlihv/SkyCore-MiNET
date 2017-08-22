﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiNET;
using MiNET.Effects;
using MiNET.Entities;
using MiNET.Items;
using MiNET.Utils;
using MiNET.Worlds;
using SkyCore.Game.State;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Game
{
    public abstract class GameLevel : Level
    {

        public delegate void PlayerAction(SkyPlayer player);

        //Player -> Team
        protected readonly Dictionary<string, GameTeam> PlayerTeamDict = new Dictionary<string, GameTeam>();

        //Team -> Player(s) //TODO: Possibly remove due to complexity?
        protected readonly Dictionary<GameTeam, List<SkyPlayer>> TeamPlayerDict = new Dictionary<GameTeam, List<SkyPlayer>>();

        //

        public SkyCoreAPI Plugin { get; }

        public string GameId { get; }

        public GameState CurrentState { get; private set; }

        protected readonly Thread _gameTickThread;
        protected HighPrecisionTimer _gameTick;

        public int Tick { get; private set; }

        //

        public GameLevel(SkyCoreAPI plugin, string gameId, String levelPath)
                //: base(plugin.Context.LevelManager, gameId, AnvilProviderFactory.GetLevelProvider(plugin.Context.LevelManager, levelPath),
                : base(plugin.Context.LevelManager, gameId, new AnvilWorldProvider(levelPath), 
                      plugin.Context.LevelManager.EntityManager, GameMode.Creative)
        {
            Plugin = plugin;
            GameId = gameId;

            AllowBreak = false;
            AllowBuild = false;

            EnableBlockTicking = false;
            EnableChunkTicking = false;

            SkyUtil.log($"Initializing world {gameId}");
            Initialize();

            ((AnvilWorldProvider)WorldProvider).PruneAir();
            ((AnvilWorldProvider)WorldProvider).MakeAirChunksAroundWorldToCompensateForBadRendering();

            if (!plugin.Context.LevelManager.Levels.Contains(this))
            {
                SkyUtil.log($"Adding {gameId} to Main LevelManager");
                plugin.Context.LevelManager.Levels.Add(this);
            }

            InitializeTeamMap();

            CurrentState = GetInitialState();
            CurrentState.EnterState(this);

            _gameTickThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                _gameTick = new HighPrecisionTimer(500, PreGameTick, true);
            });
            _gameTickThread.Start();
        }

        protected abstract void InitializeTeamMap();

        public void Close()
        {
            _gameTick.Dispose();
            _gameTickThread.Abort();
        }

        public int GetPlayerCount()
        {
            return PlayerTeamDict.Count;
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

            PlayerTeamDict.Add(player.Username, defaultTeam);
            SkyUtil.log($"Added {player.Username} to team {defaultTeam.DisplayName}");

            //player.SpawnLevel(this, new PlayerLocation(7.5, 181, -20.5));
            player.SpawnLevel(this, new PlayerLocation(255, 70, 255));
        }
        
        public void RemovePlayer(SkyPlayer player)
        {
            PlayerTeamDict.Remove(player.Username);

            Level level = LevelManager.Levels.FirstOrDefault(l => l.LevelId.Equals("Overworld", StringComparison.InvariantCultureIgnoreCase));
            if (level == null)
            {
                player.Disconnect("Unable to send you back to the hub!");
                return;
            }

            player.SpawnLevel(level, level.SpawnPoint);
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

            player.SetAllowFly(true);
            player.IsFlying = true;

            //Bump the player up into the air to signify death
            player.Knockback(new Vector3(0f, 0.5f, 0f));
        }

    }
    
}
