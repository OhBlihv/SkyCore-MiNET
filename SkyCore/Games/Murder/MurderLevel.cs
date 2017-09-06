using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiNET;
using MiNET.Entities;
using MiNET.Entities.Projectiles;
using MiNET.Items;
using MiNET.Utils;
using SkyCore.Game;
using SkyCore.Game.State;
using SkyCore.Games.Murder.Items;
using SkyCore.Games.Murder.State;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Games.Murder
{
    class MurderLevel : GameLevel
    {

        public List<PlayerLocation> PlayerSpawnLocations = new List<PlayerLocation>();
        public List<PlayerLocation> GunPartLocations = new List<PlayerLocation>();

        public SkyPlayer Murderer { get; set; }
        public SkyPlayer Detective { get; set; }

        public MurderLevel(SkyCoreAPI plugin, string gameId, string levelPath) : base(plugin, "murder", gameId, levelPath)
        {
            string levelName;
            {
                string[] split = levelPath.Split('\\');
                levelName = split[split.Length - 1];
            }
            
            //Hardcoded spawn for initial map
            SkyUtil.log($"Initializing level '{levelName}'");
            switch (levelName)
            {
                case "murder-library":
                {
                    SpawnPoint = new PlayerLocation(255D, 54D, 255D);

                    PlayerSpawnLocations.Add(new PlayerLocation(255D, 54D, 280D));
                    PlayerSpawnLocations.Add(new PlayerLocation(265D, 54D, 275D));
                    GunPartLocations.Add(new PlayerLocation(255D, 54D, 280D));
                    GunPartLocations.Add(new PlayerLocation(265D, 54D, 275D));
                    break;
                }
            }
           
        }

        protected override void InitializeTeamMap()
        {
            TeamPlayerDict.Add(MurderTeam.Innocent, new List<SkyPlayer>());
            TeamPlayerDict.Add(MurderTeam.Murderer, new List<SkyPlayer>());
            TeamPlayerDict.Add(MurderTeam.Detective, new List<SkyPlayer>());
            TeamPlayerDict.Add(MurderTeam.Spectator, new List<SkyPlayer>());
        }

        public override void SetPlayerTeam(SkyPlayer player, GameTeam oldTeam, GameTeam team)
        {
            SkyUtil.log("Using MurderLevel SetPlayerTeam");
            base.SetPlayerTeam(player, oldTeam, team);

            if (oldTeam == team)
            {
                return;
            }

            if (team == MurderTeam.Murderer)
            {
                Murderer = player;
            }
            else if (team == MurderTeam.Detective)
            {
                Detective = player;
            }
            //Handle Death
            else if (team == MurderTeam.Spectator && oldTeam != null)
            {
                if (oldTeam == MurderTeam.Innocent || oldTeam == MurderTeam.Detective)
                {
                    //Check remaining players to see if the game should 'end'
                    if (GetPlayersInTeam(MurderTeam.Innocent, MurderTeam.Detective).Count == 0)
                    {
                        //TODO: End Game
                        UpdateGameState(new MurderEndState());
                        return;
                    }
                }
                else if (oldTeam == MurderTeam.Murderer)
                {
                    //TODO: Remove this second check, since if the murderer is changing teams the game must be won?
                    //Check remaining players to see if the game should 'end'
                    if (GetPlayersInTeam(MurderTeam.Murderer).Count == 0)
                    {
                        //TODO: End Game
                        UpdateGameState(new MurderEndState());
                        return;
                    }
                }
            }
        }

        public override GameState GetInitialState()
        {
            return new MurderLobbyState();
        }

        public override GameTeam GetDefaultTeam()
        {
            return MurderTeam.Innocent;
        }

        public override int GetMaxPlayers()
        {
            return 12;
        }

        public override void GameTick(int tick)
        {
            //Console.WriteLine("Tick: " + tick);
        }

    }
}
