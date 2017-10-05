using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fNbt;
using MiNET.Entities.Projectiles;
using MiNET.Items;
using MiNET.Utils;
using MiNET.Worlds;
using SkyCore.Game;
using SkyCore.Games.Murder.Entities;
using SkyCore.Games.Murder.State;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Games.Murder.Items
{
    public class ItemInnocentGun : ItemBow
    {

        private NbtCompound _extraData;

        public override NbtCompound ExtraData
        {
            get => _extraData = UpdateExtraData();
            set => _extraData = value;
        }

        private NbtCompound UpdateExtraData()
        {
            if (_extraData == null)
            {
                _extraData = new NbtCompound
                {
                    new NbtCompound("display")
                    {
                        new NbtString("Name", "§r§d§lInnocent's Gun§r"),
                        new NbtList("Lore")
                        {
                            new NbtString("§r§7Contains a finite amount of bullets"),
                            new NbtString("§r§7Collect Gun Parts to refill"),
                            //new NbtString(),
                        }
                    }
                };
            }

            return _extraData;
        }

        public override void Release(MiNET.Worlds.Level world, MiNET.Player player, BlockCoordinates blockCoordinates, long timeUsed)
        {
	        ((MurderRunningState) ((MurderLevel) world).CurrentState).DoInteract((GameLevel) world, 1, (SkyPlayer) player, null);
	        /*if (player.Experience > 0.1f)
	        {
		        return;
	        }

            float force = 2.0f;

	        GunProjectile arrow = new GunProjectile(player, world, 2, force >= 1.0)
	        {
		        PowerLevel = 1,
		        KnownPosition = (PlayerLocation) player.KnownPosition.Clone()
	        };
	        arrow.KnownPosition.Y += 1.62f;
            arrow.Velocity = arrow.KnownPosition.GetHeadDirection() * (float)(force * 2.0 * 1.5);
            arrow.KnownPosition.Yaw = (float) arrow.Velocity.GetYaw();
            arrow.KnownPosition.Pitch = (float) arrow.Velocity.GetPitch();
            arrow.BroadcastMovement = true;
            arrow.DespawnOnImpact = true;
            arrow.SpawnEntity();

	        player.Inventory.Slots[0].Durability = 0;*/
        }
    }
}
