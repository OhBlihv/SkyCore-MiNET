using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using MiNET.Effects;
using MiNET.Items;
using MiNET.Worlds;
using SkyCore.Game;
using SkyCore.Game.Level;
using SkyCore.Player;

namespace SkyCore.Games.Hub
{
	public class HubCoreController : CoreGameController
	{

		private Level _hubLevel;

		public HubCoreController(SkyCoreAPI plugin) : base(plugin, "hub", "Hub", new List<string>())
		{
			Tick = 1;
			
			ExternalGameHandler.RegisterGameIntent("murder");
			ExternalGameHandler.RegisterGameIntent("build-battle");
			ExternalGameHandler.RegisterGameIntent("block-hunt");
			ExternalGameHandler.RegisterGameIntent("bed-wars");
		}

		protected override GameLevel _getGameController()
		{
			throw new NotImplementedException();
		}

		protected override void CoreGameTick()
		{
			int playerCount = 0;
			if (_hubLevel == null)
			{
				_hubLevel = SkyCoreAPI.Instance.GetHubLevel();
			}
			else
			{
				playerCount = _hubLevel.PlayerCount;

				//Update BarHandlers for all online players every 500 milliseconds (10 ticks)
				if (++Tick % 10 == 0 && _hubLevel != null)
				{
					foreach (SkyPlayer player in _hubLevel.Players.Values)
					{
						player.BarHandler?.DoTick();
					}
				}
			}

			if (Tick % 20 != 0)
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

			instanceInfo.CurrentPlayers = playerCount;
			instanceInfo.AvailableGames = new List<GameInfo> {new GameInfo("0", playerCount, 100)};
			instanceInfo.Update();
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

			NightVision nightVision = new NightVision
			{
				Duration = int.MaxValue,
				Level = 0,
				Particles = false
			};
			player.SetEffect(nightVision);
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
