using SkyCore.Game.Level;
using SkyCore.Game.State.Impl;
using SkyCore.Games.Murder.Level;
using SkyCore.Player;
using SkyCore.Util;

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

			SkyPlayer murderer = ((MurderLevel)gameLevel).Murderer;

			//Innocents Win (Murderer Dead)
			if (gameLevel.GetPlayersInTeam(MurderTeam.Murderer).Count == 0)
            {
                //victoryType = MurderVictoryType.MURDERER_DEAD;
				
				gameLevel.DoForAllPlayers(player =>
                {
					TitleUtil.SendCenteredSubtitle(player, $"§a§lInnocents §r§7§lWin§r\n§7{murderer?.Username ?? "An Unknown Player"} §fwas the Murderer!");
                });
            }
            //Murderer Wins (Innocents + Detective Dead)
            else if(gameLevel.GetPlayersInTeam(MurderTeam.Innocent, MurderTeam.Detective).Count == 0)
            {
				//victoryType = MurderVictoryType.CONQUEST;

				gameLevel.DoForAllPlayers(player =>
				{
					TitleUtil.SendCenteredSubtitle(player, $"§c§lMurderer §r§7§lWins§r\n§7{murderer?.Username ?? "An Unknown Player"} §fwas the Murderer!");
				});
			}
            //Innocents Win (Timeout)
            else
            {
				//victoryType = MurderVictoryType.TIMEOUT;

				gameLevel.DoForAllPlayers(player =>
				{
					TitleUtil.SendCenteredSubtitle(player, $"§a§lInnocents §r§7Win\n§7{murderer?.Username ?? "An Unknown Player"} §fwas the Murderer!");
				});
			}
        }

    }
}
