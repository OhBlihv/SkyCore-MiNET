using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using MiNET.Blocks;
using MiNET.Entities.World;
using MiNET.Items;
using MiNET.Net;
using MiNET.Utils;
using MiNET.Worlds;
using SkyCore.Games.Murder.State;
using SkyCore.Player;

namespace SkyCore.Games.Murder.Entities
{
    class MurderGunPartEntity : ItemEntity
    {

        public MurderRunningState GameState { get; }

        //Hold a second location to ensure we have a valid key
        public PlayerLocation SpawnLocation { get; }

        public MurderGunPartEntity(MurderRunningState gameState, MurderLevel level, PlayerLocation spawnLocation) : base(level, new ItemGoldenApple())
        {
            GameState = gameState;
            KnownPosition = spawnLocation;
            SpawnLocation = spawnLocation;
        }

        public override void OnTick()
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
            foreach (var player in players)
            {
                if (player.GameMode != GameMode.Spectator && KnownPosition.DistanceTo(player.KnownPosition) <= 2)
                {
                    {
                        //Others

                        /*var takeItemEntity = McpeTakeItemEntity.CreateObject();
                        takeItemEntity.runtimeEntityId = EntityId;
                        takeItemEntity.target = player.EntityId;
                        Level.RelayBroadcast(player, takeItemEntity);*/
                    }
                    {
                        //Self
                        /*int amount = */GameState.AddPlayerGunParts((MurderLevel) Level, (SkyPlayer) player);
                        /*if (amount >= 0)
                        {
                            player.SendMessage($"Picked up gun part {amount}/{5}!");
                        }*/

                        GameState.GunParts.Remove(SpawnLocation);

                        /*var takeItemEntity = McpeTakeItemEntity.CreateObject();
                        takeItemEntity.runtimeEntityId = EntityId;
                        takeItemEntity.target = EntityManager.EntityIdSelf;
                        player.SendPackage(takeItemEntity);*/
                    }

                    DespawnEntity();
                    break;
                }
            }
        }

    }
}
