using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiNET.Worlds;
using SkyCore.Game;
using SkyCore.Game.Level;
using SkyCore.Game.State;
using SkyCore.Game.State.Impl;
using SkyCore.Games.Murder.State;
using SkyCore.Player;

namespace SkyCore.Games.BuildBattle.State
{
	class BuildBattleLobbyState : LobbyState
	{
		public override void EnterState(GameLevel gameController)
		{
			base.EnterState(gameController);

			foreach (SkyPlayer existingPlayer in gameController.GetPlayers())
			{
				existingPlayer.UpdateGameMode(GameMode.Creative, true);
			}
		}

		public override GameState GetNextGameState(GameLevel gameController)
		{
			return new BuildBattleBuildState();
		}

		public override bool DoInteract(GameLevel gameLevel, int interactId, SkyPlayer player, SkyPlayer target)
		{
			//No Handling
			return true;
		}
	}
}
