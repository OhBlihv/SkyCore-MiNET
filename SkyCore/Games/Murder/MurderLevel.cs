using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MiNET;
using MiNET.Effects;
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
using SkyCore.Util.File;

namespace SkyCore.Games.Murder
{
    class MurderLevel : GameLevel
    {

	    public string LevelName { get; }

        public List<PlayerLocation> PlayerSpawnLocations = new List<PlayerLocation>();
        public List<PlayerLocation> GunPartLocations = new List<PlayerLocation>();

        public SkyPlayer Murderer { get; set; }
        public SkyPlayer Detective { get; set; }

        public MurderLevel(SkyCoreAPI plugin, string gameId, string levelPath) : base(plugin, "murder", gameId, levelPath, new PlayerLocation(266, 11, 256))
        {
            string levelName;
            {
                string[] split = levelPath.Split('\\');
                levelName = split[split.Length - 1];
            }

	        LevelName = levelName;

			//TODO: Move loading this to the CoreGameController of this games type?
	        FlatFile flatFile = FlatFile.ForFile(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\config\\murder.yml");
            
            //Hardcoded spawn for initial map
            SkyUtil.log($"Initializing level '{levelName}'");
	        if (!flatFile.Contains($"level-names.{levelName}.name"))
	        {
		        throw new ArgumentException($"Level {levelName} not found in {GameType}.yml!");
	        }

	        SpawnPoint = flatFile.GetLocation($"level-names.{levelName}.hub-location", new PlayerLocation(0, 100D, 0));

	        PlayerSpawnLocations.AddRange(flatFile.GetLocationList($"level-names.{levelName}.spawn-locations", new List<PlayerLocation>()));
	        GunPartLocations.AddRange(flatFile.GetLocationList($"level-names.{levelName}.gun-part-locations", new List<PlayerLocation>()));

			SkyUtil.log($"Initialized Player Spawns with {PlayerSpawnLocations.Count} unique locations");
			SkyUtil.log($"Initialized Gun Part Locations with {GunPartLocations.Count} unique locations");

	        /*switch (levelName)
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
		        case "murder-library-alternative":
		        {
			        SpawnPoint = new PlayerLocation(255D, 54D, 255D);

					PlayerSpawnLocations.Add(new PlayerLocation(255D, 54D, 280D));
			        PlayerSpawnLocations.Add(new PlayerLocation(265D, 54D, 275D));
			        GunPartLocations.Add(new PlayerLocation(255D, 54D, 280D));
			        GunPartLocations.Add(new PlayerLocation(265D, 54D, 275D));
			        break;
		        }
			}*/

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

    }
}
