using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiNET;
using MiNET.Blocks;
using MiNET.Entities;
using MiNET.Items;
using MiNET.Utils;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Game.State
{
    public abstract class GameState
    {

        public abstract void EnterState(GameLevel gameLevel);

        public abstract void LeaveState(GameLevel gameLevel);

        public abstract bool CanAddPlayer(GameLevel gameLevel);

	    public virtual void InitializePlayer(GameLevel gameLevel, SkyPlayer player)
	    {
		    //
	    }

	    public virtual void HandleLeave(GameLevel gameLevel, SkyPlayer player)
	    {
		    
	    }

        public abstract void OnTick(GameLevel gameLevel, int currentTick, out int outTick);
        
        public abstract GameState GetNextGameState(GameLevel gameLevel);

        public abstract StateType GetEnumState(GameLevel gameLevel);

	    public virtual bool DoInteract(GameLevel gameLevel, int interactId, SkyPlayer player, SkyPlayer target)
	    {
		    return false;
	    }

	    public virtual void HandleDamage(GameLevel gameLevel, Entity source, Entity target, Item item, int damage, DamageCause damageCause)
	    {
		    
	    }

	    public virtual bool HandleBlockPlace(GameLevel gameLevel, SkyPlayer player, Block existingBlock, Block targetBlock)
	    {
		    return true;
	    }

	    public virtual bool HandleBlockBreak(GameLevel gameLevel, SkyPlayer player, Block block, List<Item> drops)
	    {
		    return true;
	    }

		public bool IsActiveState(GameLevel gameController)
        {
            return gameController.CurrentState == this;
        }

    }
}
