﻿using System;
using System.Linq;
using MiNET.Entities;
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
			KnownPosition.Y += 2.8f;
		}

		public override void OnTick(Entity[] entities)
		{
			int playerCount = -1;
			GamePool gamePool = null;
			try
			{
				gamePool = ExternalGameHandler.GameRegistrations[_gameName];
				playerCount = gamePool.GetCurrentPlayers();
			}
			catch (Exception e)
			{
				SkyUtil.log($"Looking for '{_gameName}'");
				SkyUtil.log(ExternalGameHandler.GameRegistrations.Keys.ToArray().ToString());
				Console.WriteLine(e);
			}

			if (gamePool != null && gamePool.Active && playerCount >= 0)
			{
				if (gamePool.GetAllInstances().Count > 0)
				{
					SetNameTag($"§fPlayers Online:§r §e{playerCount}");
				}
				else
				{
					SetNameTag("§cUnavailable");
				}
			}
			else
			{
				SetNameTag("§a(Coming Soon)");
			}
		}

	}
}
