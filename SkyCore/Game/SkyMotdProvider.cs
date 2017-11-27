using System.Net;
using MiNET;
using MiNET.Utils;

namespace SkyCore.Game
{
	public class SkyMotdProvider : MotdProvider
	{

		public SkyMotdProvider()
		{
			Motd = Config.GetProperty("motd", "§d§lSkytonia Network");
			SecondLine = Config.GetProperty("motd-2nd", " ");
		}

		public override string GetMotd(ServerInfo serverInfo, IPEndPoint caller, bool eduMotd = false)
		{
			NumberOfPlayers = serverInfo.NumberOfPlayers;
			MaxNumberOfPlayers = serverInfo.MaxNumberOfPlayers;

			var protocolVersion = "140";
			var clientVersion = "1.2.5";
			var edition = "MCPE";

			if (eduMotd)
			{
				protocolVersion = "111";
				clientVersion = "1.0.17";
				edition = "MCEE";
			}

#pragma warning disable CS0618 // Type or member is obsolete
			return string.Format($"{edition};{Motd};{protocolVersion};{clientVersion};{ExternalGameHandler.TotalPlayers};{MaxNumberOfPlayers};{Motd.GetHashCode() + caller.Address.Address + caller.Port};{SecondLine};Survival;");
#pragma warning restore CS0618 // Type or member is obsolete
		}
	}
}
