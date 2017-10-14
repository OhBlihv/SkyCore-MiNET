using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fNbt;
using MiNET.Items;
using MiNET.Utils;

namespace SkyCore.Games.Murder.Items
{
    class ItemMurderKnife : ItemIronSword
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
                        new NbtString("Name", "§r§c§lMurder Knife§r"),
                        new NbtList("Lore")
                        {
                            new NbtString("§r§7Stick the pointy end toward innocents.")
                        }
                    }
                };
            }

            return _extraData;
        }


    }
}
