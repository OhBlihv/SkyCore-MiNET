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
using SkyCore.Game.Level;

namespace SkyCore.Player
{
    class SkyHealthManager : HealthManager
    {

        public SkyHealthManager(Entity entity) : base(entity)
        {

        }

        public override void TakeHit(Entity source, Item tool, int damage = 1, DamageCause cause = DamageCause.Unknown)
        {
	        if (Entity.Level is GameLevel level)
	        {
		        level.HandleDamage(source, Entity, tool, damage, cause);
	        }

			//Void damage should be handled this way regardless of location
			if (cause == DamageCause.Void)
			{
				PlayerLocation spawnLocation = (PlayerLocation)Entity.Level.SpawnPoint.Clone();
				spawnLocation.Y += 1; //Spawn slightly above the spawn block

				((SkyPlayer)Entity).Teleport(spawnLocation);
			}
		}

        private readonly object _killSync = new object();

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
