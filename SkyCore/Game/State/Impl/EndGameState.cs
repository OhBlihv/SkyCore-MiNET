﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiNET;
using MiNET.Entities;
using MiNET.Items;
using SkyCore.Player;

namespace SkyCore.Game.State.Impl
{
    public abstract class EndGameState : GameState
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
            
        }

        public override void HandleDamage(GameLevel gameLevel, Entity source, Entity target, Item item, int damage, DamageCause damageCause)
        {
            //No damage during End Game Phase
        }

        public override void OnTick(GameLevel gameLevel, int currentTick, out int outTick)
        {
            outTick = currentTick;
        }

        public override GameState GetNextGameState(GameLevel gameLevel)
        {
            return null; //End
        }

        public override StateType GetEnumState(GameLevel gameLevel)
        {
            return StateType.EndGame;
        }
    }
}
