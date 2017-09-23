using System;
using System.Collections.Generic;
using System.Linq;
using MiNET.Worlds;
using SkyCore.Game;
using SkyCore.Player;

namespace SkyCore.Games.Hub
{
	public class HubCoreController : CoreGameController
	{
		public HubCoreController(SkyCoreAPI plugin) : base(plugin, "hub", "Hub", new List<string>())
		{
			_tick = 1;
		}

		protected override GameLevel _getGameController()
		{
			throw new NotImplementedException();
		}

		protected override void CoreGameTick()
		{
			//Update BarHandlers for all online players every 500 milliseconds (10 ticks)
			if (++_tick % 10 == 0)
			{
				foreach (SkyPlayer player in SkyCoreAPI.Instance.GetAllOnlinePlayers())
				{
					player.BarHandler?.DoTick();
				}
			}

			if (_tick % 100 != 0)
			{
				return; //Only update player counts every 100 ticks (5 seconds)
			}

			Level level = SkyCoreAPI.Instance.Context.LevelManager.Levels.FirstOrDefault(l =>
				l.LevelId.Equals("Overworld", StringComparison.InvariantCultureIgnoreCase));

			GameInfo gameInfo;
			try
			{
				if (!ExternalGameHandler.GameRegistrations.ContainsKey(RawName))
				{
					SkyUtil.log($"Game Not Registered! '{RawName}'. Contains: {ExternalGameHandler.GameRegistrations.Keys}");
					return;
				}
				gameInfo = ExternalGameHandler.GameRegistrations[RawName];
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return;
			}

			gameInfo.update(0, 0); //Reset

			if (level != null)
			{
				gameInfo.CurrentPlayers = level.PlayerCount;
				gameInfo.AvailableGames = 1;
			}
		}

		public override void InstantQueuePlayer(SkyPlayer player)
		{

		}

		public override void CheckCapacity()
		{

		}

	}
}
