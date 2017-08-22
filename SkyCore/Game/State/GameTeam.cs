using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkyCore.Util;

namespace SkyCore.Game.State
{
    public class GameTeam : Enumeration
    {

        public bool IsSpectator { get; }

        public GameTeam(int value, string name, bool isSpectator = false) : base(value, name)
        {
            IsSpectator = isSpectator;
        }

    }
}
