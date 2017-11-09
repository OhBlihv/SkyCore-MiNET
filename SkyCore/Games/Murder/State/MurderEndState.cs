using SkyCore.Game;
using SkyCore.Game.Level;
using SkyCore.Game.State.Impl;
using SkyCore.Games.Murder.Level;
using SkyCore.Player;

namespace SkyCore.Games.Murder.State
{
    class MurderEndState : EndGameState
    {
        public override void EnterState(GameLevel gameLevel)
        {
            base.EnterState(gameLevel);

			gameLevel.DoForAllPlayers(player =>
			{
				player.Inventory.Clear();
				player.AddExperience(-1000, true); //Reset gun cooldowns
			});

            //MurderVictoryType victoryType;

            //Innocents Win (Murderer Dead)
            if (gameLevel.GetPlayersInTeam(MurderTeam.Murderer).Count == 0)
            {
                //victoryType = MurderVictoryType.MURDERER_DEAD;

				MiNET.Player murderer = ((MurderLevel)gameLevel).Murderer;

	            ((SkyPlayer) murderer).BarHandler.AddMajorLine("§7The §aINNOCENTS §7have killed the §cMURDERER!§r", 20, 8);
	            murderer.SendTitle("§e§lYOU LOSE§r");

				gameLevel.DoForPlayersIn(player =>
                {
	                if (player == murderer)
	                {
		                return;
	                }

	                ((SkyPlayer)murderer).BarHandler.AddMajorLine("§7The §aINNOCENTS §7have killed the §cMURDERER!§r", 20, 8);
                    player.SendTitle("§a§lYOU WIN§r");
                }, MurderTeam.Spectator, MurderTeam.Innocent, MurderTeam.Detective);
            }
            //Murderer Wins (Innocents + Detective Dead)
            else if(gameLevel.GetPlayersInTeam(MurderTeam.Innocent, MurderTeam.Detective).Count == 0)
            {
                //victoryType = MurderVictoryType.CONQUEST;

                gameLevel.DoForPlayersIn(player =>
                {
	                player.BarHandler.AddMajorLine("§7The §cMURDERER §7have killed all §aINNOCENTS!§r", 20, 8);
					player.SendTitle("§a§lYOU WIN§r");
                }, MurderTeam.Murderer);

                //TODO: DO properly
                gameLevel.DoForPlayersIn(player =>
                {
	                player.BarHandler.AddMajorLine("§7The §cMURDERER §7have killed all §aINNOCENTS!§r", 20, 8);
                    player.SendTitle("§c§lYOU LOSE");
                }, MurderTeam.Spectator, MurderTeam.Innocent, MurderTeam.Detective);
            }
            //Innocents Win (Timeout)
            else
            {
                //victoryType = MurderVictoryType.TIMEOUT;

                gameLevel.DoForPlayersIn(player =>
                {
	                player.BarHandler.AddMajorLine("§7The §cMURDERER §7has run out of time!§r", 20, 8);
					player.SendTitle("§a§lYOU WIN");
                }, MurderTeam.Spectator, MurderTeam.Innocent, MurderTeam.Detective);

                gameLevel.DoForPlayersIn(player =>
                {
	                player.BarHandler.AddMajorLine("§7The §cMURDERER §7has run out of time!§r", 20, 8);
                    player.SendTitle("§c§lYOU LOSE");
                }, MurderTeam.Murderer);
            }
        }

        public override bool DoInteractAtEntity(GameLevel gameLevel, int interactId, SkyPlayer player, SkyPlayer target)
        {
            //No Handling
            return true;
        }
    }
}
