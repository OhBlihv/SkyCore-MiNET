using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MiNET.Effects;
using MiNET.Items;
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
			SkyUtil.log($"Initializing level '{LevelName}'");

	        foreach (PlayerLocation playerSpawnLocation in ((MurderLevelInfo) GameLevelInfo).PlayerSpawnLocations)
	        {
				playerSpawnLocation.Y += 0.2f; //Ensure this spawn is not inside the ground

				//Round to the centre of the block.
		        playerSpawnLocation.X = (float) (Math.Floor(playerSpawnLocation.X) + 0.5f);
		        playerSpawnLocation.Z = (float) (Math.Floor(playerSpawnLocation.Z) + 0.5f);
	        }

	        if (((MurderLevelInfo) GameLevelInfo).PlayerSpawnLocations.Count == 0 ||
	            ((MurderLevelInfo) GameLevelInfo).GunPartLocations.Count == 0)
	        {
		        SkyUtil.log($"Player Spawns -> {((MurderLevelInfo)GameLevelInfo).PlayerSpawnLocations.Count}");
		        SkyUtil.log($"Gun Part Locations -> {((MurderLevelInfo)GameLevelInfo).GunPartLocations.Count}");

				throw new Exception("Defined spawns below range!");
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

	    public override string GetGameModalTitle()
	    {
		    return "§d§lMURDER MYSTERY";
	    }

		public override string GetEndOfGameContent(SkyPlayer player)
	    {
		    GameTeam winningTeam = null;

		    int innocentPlayers = GetPlayersInTeam(MurderTeam.Innocent, MurderTeam.Detective).Count;

		    if (innocentPlayers > 0)
		    {
			    if (GetPlayersInTeam(MurderTeam.Murderer).Count == 0)
			    {
					winningTeam = MurderTeam.Innocent;
				}
				//Else, null
		    }
		    else
		    {
				winningTeam = MurderTeam.Murderer;
			}

		    if (winningTeam == null)
		    {
			    return
				    "\n" +
				    TextUtils.Center("", 205) + "\n" +
				    TextUtils.Center("§cYou are dead!", 205) + "\n";
		    }

		    return
			    "\n" +
			    TextUtils.Center("§f§lWinner:§r " + ((MurderTeam) winningTeam).TeamPrefix + winningTeam.DisplayName.ToUpper(),
				    205) + "\n" +
			    " " + "\n" +
				"§8§o---------------------------------§r" + "\n" +
			    " " + "\n" +
			    TextUtils.Center("§9§lDetective:§r §f" + Detective.Username + (Detective.GameTeam == null || Detective.GameTeam.IsSpectator ? " §7[DEAD]" : ""),
				    205) +
			    "\n" +
			    TextUtils.Center("§c§lMurderer:§r §f" + Murderer.Username + (Murderer.GameTeam == null || Murderer.GameTeam.IsSpectator ? " §7[DEAD]" : ""), 205) +
			    "\n";
	    }

    }
}
