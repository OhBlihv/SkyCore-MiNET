using SkyCore.Game.Level;
using SkyCore.Game.State;
using SkyCore.Game.State.Impl;

namespace SkyCore.Games.Murder.State
{
    class MurderLobbyState : LobbyState
    {

	    public override GameState GetNextGameState(GameLevel gameController)
        {
            return new MurderRunningState();
        }

    }
}
