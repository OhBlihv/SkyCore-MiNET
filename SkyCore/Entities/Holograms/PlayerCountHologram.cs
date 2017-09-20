using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiNET.Utils;
using MiNET.Worlds;
using SkyCore.Game;

namespace SkyCore.Entities.Holograms
{
	public class PlayerCountHologram : TickingHologram
	{

		private readonly string _gameName;

		public PlayerCountHologram(string name, Level level, PlayerLocation playerLocation, string gameName) : base(name, level, playerLocation)
		{
			_gameName = gameName;

			KnownPosition = (PlayerLocation) KnownPosition.Clone();
			KnownPosition.Y += 2.5f;
		}

		public override void OnTick()
		{
			int playerCount = -1;
			try
			{
				playerCount = ExternalGameHandler.GameRegistrations[_gameName].CurrentPlayers;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}

			if (playerCount >= 0)
			{
				SetNameTag($"§e§lCurrent Players:§r {playerCount}");
			}
			else
			{
				SetNameTag("§c§lOFFLINE");
			}
		}

	}
}
