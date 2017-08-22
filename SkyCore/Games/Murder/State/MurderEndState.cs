using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiNET;
using SkyCore.Game;
using SkyCore.Game.State;
using SkyCore.Game.State.Impl;
using SkyCore.Player;

namespace SkyCore.Games.Murder.State
{
    class MurderEndState : EndGameState
    {
        public override void EnterState(GameLevel gameLevel)
        {
            base.EnterState(gameLevel);

            MurderVictoryType victoryType;

            //Innocents Win (Murderer Dead)
            if (gameLevel.GetPlayersInTeam(MurderTeam.Murderer).Count == 0)
            {
                victoryType = MurderVictoryType.MURDERER_DEAD;

                gameLevel.DoForPlayersIn(player =>
                {
                    player.SendTitle($"§c§lYOU LOSE§r\n" +
                                     $"" +
                                     $"§7The §aINNOCENTS §7have killed the §cMURDERER!§r", TitleType.ActionBar);
                    player.SendTitle("§e§lYOU LOSE§r");
                }, MurderTeam.Murderer);

                gameLevel.DoForPlayersIn(player =>
                {
                    player.SendTitle($"§a§lYOU WIN§r\n" +
                                     $"" +
                                     $"§7The §aINNOCENTS §7have killed the §cMURDERER!§r", TitleType.ActionBar);
                    player.SendTitle("§a§lYOU WIN§r");
                }, MurderTeam.Spectator, MurderTeam.Innocent, MurderTeam.Detective);
            }
            //Murderer Wins (Innocents + Detective Dead)
            else if(gameLevel.GetPlayersInTeam(MurderTeam.Innocent, MurderTeam.Detective).Count == 0)
            {
                victoryType = MurderVictoryType.CONQUEST;

                gameLevel.DoForPlayersIn(player =>
                {
                    player.SendTitle($"§a§lYOU WIN§r\n" +
                                     $"" +
                                     $"§7The §cMURDERER §7have killed all §aINNOCENTS!§r", TitleType.ActionBar);
                    player.SendTitle("§a§lYOU WIN§r");
                }, MurderTeam.Murderer);

                //TODO: DO properly
                gameLevel.DoForPlayersIn(player =>
                {
                    player.SendTitle($"§c§lYOU LOSE§r\n" +
                                     $"" +
                                     $"§7The §cMURDERER §7have killed all §aINNOCENTS!§r", TitleType.ActionBar);
                    player.SendTitle("§c§lYOU LOSE");
                }, MurderTeam.Spectator, MurderTeam.Innocent, MurderTeam.Detective);
            }
            //Innocents Win (Timeout)
            else
            {
                victoryType = MurderVictoryType.TIMEOUT;

                gameLevel.DoForPlayersIn(player =>
                {
                    player.SendTitle($"§a§lYOU WIN§r\n" +
                                     $"\n" +
                                     $"§7The §cMURDERER §7has run out of time!§r", TitleType.ActionBar);

                    player.SendTitle("§a§lYOU WIN");
                }, MurderTeam.Spectator, MurderTeam.Innocent, MurderTeam.Detective);

                gameLevel.DoForPlayersIn(player =>
                {
                    player.SendTitle($"§c§lYOU LOSE§r\n" +
                                     $"\n" +
                                     $"§7The §cMURDERER §7has run out of time!§r", TitleType.ActionBar);
                    player.SendTitle("§c§lYOU LOSE");
                }, MurderTeam.Murderer);
            }

            ThreadPool.QueueUserWorkItem(state =>
            {
				try
				{
					Thread.Sleep(5000);
					gameLevel.DoForPlayersIn(player =>
					{
						player.SendTitle("§c§lGAME RESTARTING");
						player.SendTitle("§7in 5 seconds...", TitleType.SubTitle);
					}, MurderTeam.Innocent, MurderTeam.Detective, MurderTeam.Murderer, MurderTeam.Spectator);
					Thread.Sleep(5000);

					gameLevel.UpdateGameState(new MurderLobbyState());
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
            });
        }

        public override GameState GetNextGameState(GameLevel gameLevel)
        {
            //TODO: Have a timeout on end game state which moves all players away
            return new MurderLobbyState();
        }

        public override bool DoInteract(GameLevel gameLevel, int interactId, SkyPlayer player, SkyPlayer target)
        {
            //No Handling
            return true;
        }
    }
}
