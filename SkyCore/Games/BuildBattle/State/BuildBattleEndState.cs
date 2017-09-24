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
		}

		public override bool DoInteract(GameLevel gameLevel, int interactId, SkyPlayer player, SkyPlayer target)
		{
			//No Handling
			return true;
		}
	}

}
