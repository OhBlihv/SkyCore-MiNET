using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiNET;
using MiNET.Entities;
using MiNET.Utils;
using MiNET.Worlds;

namespace SkyCore.Entities
{
    public class Hologram : MiNET.Entities.Hologram
    {
        public string StaticName { get; set; } = null;

        public MiNetServer server { get; set; } = null;

        public Hologram(string name, Level level, PlayerLocation playerLocation) : base(level)
        {
            NameTag = name;

            KnownPosition = playerLocation;
        }

        public override void SetNameTag(string nameTag)
        {
            NameTag = nameTag;

            BroadcastSetEntityData();
        }

    }
}
