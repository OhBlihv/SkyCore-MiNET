using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MiNET;
using SkyCore.Game;

namespace SkyCore.Player
{

    public class SkyPlayerFactory : PlayerFactory
    {

        public SkyCoreAPI SkyCoreApi { get; set; }

        public override MiNET.Player CreatePlayer(MiNetServer server, IPEndPoint endPoint, PlayerInfo pd)
        {
            var player = new SkyPlayer(server, endPoint, SkyCoreApi);
            player.HealthManager = new SkyHealthManager(player);
            player.HungerManager = new SkyFoodManager(player);
            player.MaxViewDistance = 7;
            player.UseCreativeInventory = true;
			OnPlayerCreated(new PlayerEventArgs(player));
            return player;
        }

    }
}
