using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using MiNET.Blocks;
using MiNET.Entities;
using MiNET.Entities.World;
using MiNET.Items;
using MiNET.Net;
using MiNET.Utils;
using MiNET.Worlds;
using SkyCore.Games.Murder.Level;
using SkyCore.Games.Murder.State;
using SkyCore.Player;

namespace SkyCore.Games.Murder.Entities
{
    class MurderGunPartEntity : ItemEntity
    {

        public MurderRunningState GameState { get; }

        //Hold a second location to ensure we have a valid key
        public PlayerLocation SpawnLocation { get; }

        public MurderGunPartEntity(MurderRunningState gameState, MurderLevel level, PlayerLocation spawnLocation) : base(level, new ItemGoldIngot())
        {
            GameState = gameState;
            KnownPosition = spawnLocation;
            SpawnLocation = spawnLocation;
        }

        public override void OnTick(Entity[] entities)
		{
            TimeToLive--;
            PickupDelay--;

            if (TimeToLive <= 0)
            {
                DespawnEntity();
                return;
            }

            if (PickupDelay > 0) return;

            var players = Level.GetSpawnedPlayers();
            foreach (SkyPlayer player in players)
            {
                if (!player.IsGameSpectator && KnownPosition.DistanceTo(player.KnownPosition) <= 2)
                {
					GameState.AddPlayerGunParts((MurderLevel) Level, player);

	                GameState.GunParts.Remove(SpawnLocation);

					DespawnEntity();
                    break;
                }
            }
        }

    }
}
