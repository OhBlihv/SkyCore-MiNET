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
using SkyCore.Game.Level;
using SkyCore.Games.Murder.Entities;
using SkyCore.Games.Murder.Level;
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
                        //new NbtString("Name", "§r§d§lInnocent's Gun§r"),
	                    new NbtString("Name", "§r"),
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
	        ((MurderRunningState) ((MurderLevel) world).CurrentState).DoInteractAtEntity((GameLevel) world, 1, (SkyPlayer) player, null);
        }
    }
}
