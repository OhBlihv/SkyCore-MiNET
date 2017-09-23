using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using SkyCore.Game;
using SkyCore.Game.State;
using SkyCore.Game.State.Impl;
using SkyCore.Games.Murder;
using SkyCore.Games.Murder.State;
using SkyCore.Player;

namespace SkyCore.Games.BuildBattle.State
{

	class BuildBattleEndState : EndGameState
	{

		public BuildBattleEndState(Dictionary<SkyPlayer, int> voteDictionary)
		{
			
		}
		
		public override void EnterState(GameLevel gameLevel)
		{
			base.EnterState(gameLevel);

			gameLevel.DoForAllPlayers(player =>
			{
				player.Inventory.Clear();
			});

			MurderVictoryType victoryType;

			//Innocents Win (Murderer Dead)
			if (gameLevel.GetPlayersInTeam(MurderTeam.Murderer).Count == 0)
			{
				victoryType = MurderVictoryType.MURDERER_DEAD;

				MiNET.Player murderer = ((MurderLevel)gameLevel).Murderer;

				((SkyPlayer)murderer).BarHandler.AddMajorLine("§7The §aINNOCENTS §7have killed the §cMURDERER!§r", 2, 4);
				murderer.SendTitle("§e§lYOU LOSE§r");

				gameLevel.DoForPlayersIn(player =>
				{
					if (player == murderer)
					{
						return;
					}

					((SkyPlayer)murderer).BarHandler.AddMajorLine("§7The §aINNOCENTS §7have killed the §cMURDERER!§r", 2, 4);
					player.SendTitle("§a§lYOU WIN§r");
				}, MurderTeam.Spectator, MurderTeam.Innocent, MurderTeam.Detective);
			}
			//Murderer Wins (Innocents + Detective Dead)
			else if (gameLevel.GetPlayersInTeam(MurderTeam.Innocent, MurderTeam.Detective).Count == 0)
			{
				victoryType = MurderVictoryType.CONQUEST;

				gameLevel.DoForPlayersIn(player =>
				{
					player.BarHandler.AddMajorLine("§7The §cMURDERER §7have killed all §aINNOCENTS!§r", 2);
					player.SendTitle("§a§lYOU WIN§r");
				}, MurderTeam.Murderer);

				//TODO: DO properly
				gameLevel.DoForPlayersIn(player =>
				{
					player.BarHandler.AddMajorLine("§7The §cMURDERER §7have killed all §aINNOCENTS!§r", 2);
					player.SendTitle("§c§lYOU LOSE");
				}, MurderTeam.Spectator, MurderTeam.Innocent, MurderTeam.Detective);
			}
			//Innocents Win (Timeout)
			else
			{
				victoryType = MurderVictoryType.TIMEOUT;

				gameLevel.DoForPlayersIn(player =>
				{
					player.BarHandler.AddMajorLine("§7The §cMURDERER §7has run out of time!§r", 2);
					player.SendTitle("§a§lYOU WIN");
				}, MurderTeam.Spectator, MurderTeam.Innocent, MurderTeam.Detective);

				gameLevel.DoForPlayersIn(player =>
				{
					player.BarHandler.AddMajorLine("§7The §cMURDERER §7has run out of time!§r", 2);
					player.SendTitle("§c§lYOU LOSE");
				}, MurderTeam.Murderer);
			}
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
