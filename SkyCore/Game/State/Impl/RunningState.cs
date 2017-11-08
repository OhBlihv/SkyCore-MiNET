using System;
using SkyCore.Game.Level;
using SkyCore.Player;

namespace SkyCore.Game.State.Impl
{
    public abstract class RunningState : GameState
    {

	    public readonly Random Random = new Random();

	    public int MaxGameTime { get; set; } = 120;
		public int EndTick { get; set; } = -1; //Default value

		public int CurrentTick { get; protected set; }

		public override void EnterState(GameLevel gameLevel)
	    {
		    //
	    }

		public override void LeaveState(GameLevel gameLevel)
	    {
		    //
	    }

		public override bool CanAddPlayer(GameLevel gameLevel)
        {
            return false;
        }

        public override void HandleLeave(GameLevel gameLevel, SkyPlayer player)
        {
	        //Simulate removal by setting teams
	        gameLevel.SetPlayerTeam(player, null);
		}

        public override void OnTick(GameLevel gameLevel, int currentTick, out int outTick)
        {
            outTick = currentTick;

	        CurrentTick = currentTick;
        }

        public override StateType GetEnumState(GameLevel gameLevel)
        {
            return StateType.Running;
        }

	    public override bool DoInteract(GameLevel gameLevel, int interactId, SkyPlayer player, SkyPlayer target)
	    {
		    return true;
	    }

	    public int GetSecondsLeft()
	    {
		    return (EndTick - CurrentTick) / 2;
	    }

	    public string GetNeatTimeRemaining(int secondsLeft)
	    {
			string neatRemaining;
		    {
			    int minutes = 0;
			    while (secondsLeft >= 60)
			    {
				    secondsLeft -= 60;
				    minutes++;
			    }

			    neatRemaining = minutes + ":";

			    if (secondsLeft < 10)
			    {
				    neatRemaining += "0" + secondsLeft;
			    }
			    else
			    {
				    neatRemaining += secondsLeft;
			    }
		    }

		    return neatRemaining;
	    }

	}
}
