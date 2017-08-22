using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiNET;
using MiNET.Entities;
using MiNET.Items;
using MiNET.Utils;
using SkyCore.Game;

namespace SkyCore.Player
{
    class SkyHealthManager : HealthManager
    {

        public SkyHealthManager(Entity entity) : base(entity)
        {

        }

        public override void TakeHit(Entity source, Item tool, int damage = 1, DamageCause cause = DamageCause.Unknown)
        {
            //SkyUtil.log($"{Entity} Received damage from {source} with {tool}");
            if (Entity.Level is GameLevel)
            {
                //SkyUtil.log("Handling in GameLevel");
                ((GameLevel) Entity.Level).HandleDamage(source, Entity, tool, damage, cause);
            }
        }

        private object _killSync = new object();

        public override void Kill()
        {
            lock (_killSync)
            {
                if (IsDead) //Change to IsDeathHandled? We aren't killing players
                {
                    return;
                }

                IsDead = true;
            }

            DeathTeleport();
            ThreadPool.QueueUserWorkItem(state =>
            {
                Thread.Sleep(10);

                IsDead = false; //Allow the player to 'die' again once in safety
            });
        }

        /*
         *  Can be overridden to teleport the player to the lobby hub area, or to set spectator at some generic death location (or theirs!)
         */
        public void DeathTeleport()
        {
            ((MiNET.Player) Entity).Teleport(Entity.Level.SpawnPoint);
        }

    }
}
