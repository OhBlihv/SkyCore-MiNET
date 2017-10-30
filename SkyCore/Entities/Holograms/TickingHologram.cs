using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiNET.Entities;
using MiNET.Utils;
using MiNET.Worlds;

namespace SkyCore.Entities.Holograms
{
    public abstract class TickingHologram : Hologram
    {

        public TickingHologram(string name, Level level, PlayerLocation playerLocation) : base(name, level, playerLocation)
        {
            
        }

        public int Tick = 0;

	    public abstract override void OnTick(Entity[] entities);

	    /*public override void OnTick()
        {
            if (Tick++ == 20)
            {
                Tick = 0;
                if (Lobby && StaticName != null)
                {
                    NameTag = string.Format(StaticName, server.ServerInfo.NumberOfPlayers);
                    BroadcastSetEntityData();
                }
            }
        }*/


    }
}
