using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiNET;
using MiNET.Entities;
using MiNET.Items;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Game.State
{
    public abstract class GameState
    {

        public abstract void EnterState(GameLevel gameLevel);

        public abstract void LeaveState(GameLevel gameLevel);

        public abstract bool CanAddPlayer(GameLevel gameLevel);

        public abstract void InitializePlayer(GameLevel gameLevel, SkyPlayer player);

        public abstract void HandleLeave(GameLevel gameLevel, SkyPlayer player);

        public abstract void OnTick(GameLevel gameLevel, int currentTick, out int outTick);
        
        public abstract GameState GetNextGameState(GameLevel gameLevel);

        public abstract StateType GetEnumState(GameLevel gameLevel);

        public abstract bool DoInteract(GameLevel gameLevel, int interactId, SkyPlayer player, SkyPlayer target);

        public abstract void HandleDamage(GameLevel gameLevel, Entity source, Entity target, Item item, int damage, DamageCause damageCause);

        public bool IsActiveState(GameLevel gameController)
        {
            return gameController.CurrentState == this;
        }

    }
}
