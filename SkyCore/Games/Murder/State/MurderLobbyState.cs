using MiNET.Worlds;
using SkyCore.Game;
using SkyCore.Game.State;
using SkyCore.Game.State.Impl;
using SkyCore.Player;

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

        public override GameState GetNextGameState(GameLevel gameController)
        {
            return new MurderRunningState();
        }

        public override bool DoInteract(GameLevel gameLevel, int interactId, SkyPlayer player, SkyPlayer target)
        {
            //No Handling
            return true;
        }
    }
}
