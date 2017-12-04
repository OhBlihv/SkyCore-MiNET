using System;
using System.Collections.Generic;
using MiNET.Effects;
using MiNET.Utils;
using SkyCore.Game.Level;
using SkyCore.Game.State;
using SkyCore.Games.Murder.State;
using SkyCore.Player;

namespace SkyCore.Games.Murder.Level
{
    class MurderLevel : GameLevel
    {

        public SkyPlayer Murderer { get; set; }
        public SkyPlayer Detective { get; set; }

        public MurderLevel(SkyCoreAPI plugin, string gameId, string levelPath, GameLevelInfo gameLevelInfo) : base(plugin, "murder", gameId, levelPath, gameLevelInfo)
        {
	        if (!(gameLevelInfo is MurderLevelInfo))
	        {
		        throw new Exception($"Could not load MurderLevelInfo for level {LevelName}");
	        }

	        foreach (PlayerLocation playerSpawnLocation in ((MurderLevelInfo) GameLevelInfo).PlayerSpawnLocations)
	        {
				//TODO: Remove - Causes locations to rise above the roof
				//				 Clone if this is still required
				//playerSpawnLocation.Y += 0.1f; //Ensure this spawn is not inside the ground

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
			
			//SkyUtil.log($"Initialized Player Spawns with {((MurderLevelInfo) GameLevelInfo).PlayerSpawnLocations.Count} unique locations");
			//SkyUtil.log($"Initialized Gun Part Locations with {((MurderLevelInfo)GameLevelInfo).GunPartLocations.Count} unique locations");
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

	    public override GameTeam GetSpectatorTeam()
	    {
		    return MurderTeam.Spectator;
	    }

	    public override int GetMaxPlayers()
        {
            return 12;
        }

        public override void GameTick(int tick)
        {
            
        }

	    private MurderTeam GetWinningTeam()
	    {
		    MurderTeam winningTeam = null;

		    int innocentPlayers = GetPlayersInTeam(MurderTeam.Innocent, MurderTeam.Detective).Count;

		    if (innocentPlayers > 0)
		    {
			    if (CurrentState is MurderRunningState runningState && runningState.GetSecondsLeft() <= 0 ||
			        GetPlayersInTeam(MurderTeam.Murderer).Count == 0)
			    {
				    winningTeam = MurderTeam.Innocent;
			    }
			    //Else, null / Game is still running
		    }
		    else
		    {
			    winningTeam = MurderTeam.Murderer;
		    }

		    return winningTeam;
	    }

	    public override string GetGameModalTitle()
	    {
		    MurderTeam winningTeam = GetWinningTeam();

			//Game still running
		    if (winningTeam == null)
		    {
				return "§d§lMURDER MYSTERY";
			}

		    return "§lGame Finished";

	    }

		public override string GetEndOfGameContent(SkyPlayer player)
		{
			MurderTeam winningTeam = GetWinningTeam();

			//Game still running
		    if (winningTeam == null)
		    {
			    return "";
		    }

		    return
			    "\n" +
			    TextUtils.Center("§f§lWinner:§r " + ((MurderTeam) winningTeam).TeamPrefix + winningTeam.DisplayName.ToUpper(),
				    205) + "\n" +
			    TextUtils.Center("§8§m--------------------§r", 205) + "\n" +
			    TextUtils.Center("§9§lDetective:§r §f" + Detective?.Username, 205) +
			    "\n" +
			    TextUtils.Center("§c§lMurderer:§r §f" + Murderer?.Username, 205) +
			    "\n" +
			    "\n";
	    }

    }
}
