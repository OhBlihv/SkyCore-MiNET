using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MiNET;
using MiNET.Utils;

namespace SkyCore.Game
{
	public class SkyMotdProvider : MiNET.MotdProvider
	{

		public SkyMotdProvider()
		{
			Motd = Config.GetProperty("motd", "MiNET: MCPE Server");
			SecondLine = Config.GetProperty("motd-2nd", "MiNET");
		}

		public override string GetMotd(ServerInfo serverInfo, IPEndPoint caller, bool eduMotd = false)
		{
			NumberOfPlayers = serverInfo.NumberOfPlayers;
			MaxNumberOfPlayers = serverInfo.MaxNumberOfPlayers;

			var protocolVersion = "113";
			var clientVersion = "1.1.0";
			var edition = "MCPE";

			if (eduMotd)
			{
				protocolVersion = "111";
				clientVersion = "1.0.17";
				edition = "MCEE";
			}

			return string.Format($"{edition};{Motd};{protocolVersion};{clientVersion};{ExternalGameHandler.TotalPlayers};{MaxNumberOfPlayers};{Motd.GetHashCode() + caller.Address.Address + caller.Port};{SecondLine};Survival;");
		}
	}
}
