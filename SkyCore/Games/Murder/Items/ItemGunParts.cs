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
    class ItemGunParts : ItemGoldenApple
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
                        //new NbtString("Name", "§r§d§lGun Parts§r"),
                        new NbtString("Name", "§r"),
                        new NbtList("Lore")
                        {
                            new NbtString("§r§7Collect 5 Gun Parts to craft a Gun."),
                            //new NbtString(),
                            //new NbtString(),
                        }
                    }
                };
            }
            
            return _extraData;
        }


    }
}
