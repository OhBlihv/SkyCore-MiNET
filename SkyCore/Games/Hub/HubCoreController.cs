using System;
using System.Collections.Generic;
using System.Linq;
using MiNET.Worlds;
using SkyCore.Game;
using SkyCore.Game.Level;
using SkyCore.Player;

namespace SkyCore.Games.Hub
{
	public class HubCoreController : CoreGameController
	{

		private Level _hubLevel = null;

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
			if(_hubLevel == null)
			{
				return;
			}

			//Update BarHandlers for all online players every 500 milliseconds (10 ticks)
			if (++_tick % 10 == 0)
			{
				foreach (SkyPlayer player in _hubLevel.Players.Values)
				{
					player.BarHandler?.DoTick();
				}
			}

			if (_tick % 100 != 0)
			{
				return; //Only update player counts every 100 ticks (5 seconds)
			}

			InstanceInfo instanceInfo;
			try
			{
				if (!ExternalGameHandler.GameRegistrations.ContainsKey(RawName))
				{
					SkyUtil.log($"Game Not Registered! '{RawName}'. Contains: {ExternalGameHandler.GameRegistrations.Keys}");
					return;
				}
				instanceInfo = ExternalGameHandler.GameRegistrations[RawName].GetLocalInstance();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return;
			}

			instanceInfo.CurrentPlayers = _hubLevel.PlayerCount;
			//TODO: Improve?
			instanceInfo.AvailableGames = new List<GameInfo> {new GameInfo("0", SkyCoreAPI.Instance.GetHubLevel().PlayerCount, 100)};
		}

		public override void QueuePlayer(SkyPlayer player)
		{
			InstantQueuePlayer(player);
		}

		public override void InstantQueuePlayer(SkyPlayer player)
		{
			if (_hubLevel == null)
			{
				_hubLevel = SkyCoreAPI.Instance.GetHubLevel();
			}

			player.SpawnLevel(_hubLevel, _hubLevel.SpawnPoint);
		}

		public override void CheckCapacity()
		{

		}

		public override Type GetGameLevelInfoType()
		{
			return typeof(GameLevelInfo); //Nothing Custom
		}

	}
}
