using MiNET;
using MiNET.Entities;
using MiNET.Utils;
using MiNET.Worlds;

namespace SkyCore.Entities
{
	public class Hologram : PlayerMob
    {
        public string StaticName { get; set; } = null;

        public MiNetServer Server { get; set; } = null;

        public Hologram(string name, Level level, PlayerLocation playerLocation) : base(name, level)
        {
            NameTag = name;
	        Scale = 0f;

            KnownPosition = playerLocation;
        }

        public virtual void SetNameTag(string nameTag)
        {
            NameTag = nameTag;

            BroadcastSetEntityData();
        }

    }
}
