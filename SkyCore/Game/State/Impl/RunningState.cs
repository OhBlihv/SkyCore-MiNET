using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiNET;
using MiNET.Entities;
using MiNET.Items;
using MiNET.Utils;
using SkyCore.Player;

namespace SkyCore.Game.State.Impl
{
    public abstract class RunningState : GameState
    {
        public override void EnterState(GameLevel gameLevel)
        {
            
        }

        public override void LeaveState(GameLevel gameLevel)
        {
            
        }

        public override bool CanAddPlayer(GameLevel gameLevel)
        {
            return false;
        }

        public override void InitializePlayer(GameLevel gameLevel, SkyPlayer player)
        {
           
        }

        public override void HandleLeave(GameLevel gameLevel, SkyPlayer player)
        {
	        //Simulate removal by setting teams
	        gameLevel.SetPlayerTeam(player, null);
		}

        public override void OnTick(GameLevel gameLevel, int currentTick, out int outTick)
        {
            outTick = currentTick;
        }

        public override StateType GetEnumState(GameLevel gameLevel)
        {
            return StateType.Running;
        }

	    public override bool DoInteract(GameLevel gameLevel, int interactId, SkyPlayer player, SkyPlayer target)
	    {
		    return true;
	    }

	    public override void HandleDamage(GameLevel gameLevel, Entity source, Entity target, Item item, int damage, DamageCause damageCause)
	    {
		    
	    }

	}
}
