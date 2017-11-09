using System.Collections.Generic;
using MiNET;
using MiNET.Blocks;
using MiNET.Entities;
using MiNET.Items;
using MiNET.Utils;
using SkyCore.Game.Level;
using SkyCore.Player;

namespace SkyCore.Game.State
{

	public interface IMessageTickableState
	{

		void SendTickableMessage(GameLevel gameLevel, SkyPlayer player, ITickableInformation tickableInformation);

		ITickableInformation GetTickableInformation(SkyPlayer player);

	}

	public interface ITickableInformation
	{
		


	}

	//

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

	    public virtual bool DoInteractAtEntity(GameLevel gameLevel, int interactId, SkyPlayer player, SkyPlayer target)
	    {
		    return true;
	    }

	    public virtual bool DoInteractAtBlock(GameLevel gameLevel, int interactId, SkyPlayer player, Block block)
	    {
		    return true;
	    }

		public virtual bool HandleInventoryModification(SkyPlayer player, GameLevel gameLevel, TransactionRecord message)
	    {
		    return true;
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

	    public virtual void HandleHeldItemSlotChange(GameLevel gameLevel, SkyPlayer player, int newHeldItemSlot)
	    {
			if(this is IMessageTickableState tickableState)
		    {
				tickableState.SendTickableMessage(gameLevel, player, null);
		    }
	    }

		public bool IsActiveState(GameLevel gameController)
        {
            return gameController.CurrentState == this;
        }

    }
}
