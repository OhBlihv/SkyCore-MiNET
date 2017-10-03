using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MiNET.Effects;
using MiNET.Utils;
using Newtonsoft.Json;
using SkyCore.Game;
using SkyCore.Game.Level;
using SkyCore.Game.State;
using SkyCore.Games.Murder.Level;
using SkyCore.Games.Murder.State;
using SkyCore.Player;

namespace SkyCore.Games.Murder
{
    class MurderLevel : GameLevel
    {

        public SkyPlayer Murderer { get; set; }
        public SkyPlayer Detective { get; set; }

        public MurderLevel(SkyCoreAPI plugin, string gameId, string levelPath) : base(plugin, "murder", gameId, levelPath)
        {
	        GameLevelInfo = new MurderLevelInfo(LevelName, new PlayerLocation(266, 11, 256), new List<PlayerLocation>(),
		        new List<PlayerLocation>());
	        SpawnPoint = GameLevelInfo.LobbyLocation;

			//Hardcoded spawn for initial map
			SkyUtil.log($"Initializing level '{LevelName}'");
			/*if(!File.Exists($"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\\config\\murder-{LevelName}.json"))
	        {
		        throw new ArgumentException($"Level {LevelName} not found in config folder!");
	        }*/

	        foreach (PlayerLocation playerSpawnLocation in ((MurderLevelInfo) GameLevelInfo).PlayerSpawnLocations)
	        {
				playerSpawnLocation.Y += 0.2f; //Ensure this spawn is not inside the ground
			}
			
			SkyUtil.log($"Initialized Player Spawns with {((MurderLevelInfo) GameLevelInfo).PlayerSpawnLocations.Count} unique locations");
			SkyUtil.log($"Initialized Gun Part Locations with {((MurderLevelInfo)GameLevelInfo).GunPartLocations.Count} unique locations");
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
            else if ((team == null || team == MurderTeam.Spectator) && oldTeam != null)
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
                    else
                    {
						player.SetEffect(new Blindness { Duration = 60, Particles = false }); //Should be 3 seconds?
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

	    public override Type GetGameLevelInfoType()
	    {
		    return typeof(MurderLevelInfo);
	    }

	}
}
