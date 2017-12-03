using MiNET.Worlds;
using SkyCore.Game.Level;
using SkyCore.Game.State;
using SkyCore.Game.State.Impl;
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
				existingPlayer.UpdateGameMode(GameMode.Adventure, true);
			}
		}

		public override GameState GetNextGameState(GameLevel gameController)
		{
			return new BuildBattleBuildState();
		}

	}
}
