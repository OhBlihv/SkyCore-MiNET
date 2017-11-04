using MiNET.Worlds;
using SkyCore.Commands;
using SkyCore.Game;
using SkyCore.Game.Level;
using SkyCore.Game.State;
using SkyCore.Game.State.Impl;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Games.Murder.State
{
    class MurderLobbyState : LobbyState
    {
        public override void EnterState(GameLevel gameController)
        {
            base.EnterState(gameController);

            foreach (SkyPlayer existingPlayer in gameController.GetPlayers())
            {
                existingPlayer.SetGameMode(GameMode.Adventure);
            }
        }

	    public override void InitializePlayer(GameLevel gameLevel, SkyPlayer player)
	    {
		    base.InitializePlayer(gameLevel, player);

			RunnableTask.RunTaskLater(() =>
		    {
				SkyCommands.Instance.Video2X(player);
		    }, 2000);
	    }

	    public override GameState GetNextGameState(GameLevel gameController)
        {
            return new MurderRunningState();
        }

        public override bool DoInteract(GameLevel gameLevel, int interactId, SkyPlayer player, SkyPlayer target)
        {
            //No Handling
            return false;
        }
    }
}
