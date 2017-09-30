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
            Console.WriteLine("Creating new SkyPlayer");
            //pd.Username = pd.Username.Replace(" ", "_"); //Replace spaces with underscores
            var player = new SkyPlayer(server, endPoint, SkyCoreApi);
			//var player = new MiNET.Player(server, endPoint);
            player.HealthManager = new SkyHealthManager(player);
            player.HungerManager = new SkyFoodManager(player);
            player.MaxViewDistance = 7;
            //player.UseCreativeInventory = false;
            player.UseCreativeInventory = true;
			OnPlayerCreated(new PlayerEventArgs(player));
            Console.WriteLine("Returning new SkyPlayer");
            return player;
        }

    }
}
