using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using MiNET.Effects;
using MiNET.Items;
using MiNET.Net;
using MiNET.Particles;
using MiNET.Utils;
using MiNET.Worlds;
using SkyCore.Game;
using SkyCore.Game.Level;
using SkyCore.Games.Hub.Items;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Games.Hub
{
	public class HubCoreController : CoreGameController
	{
		
		private static readonly PlayerLocation HubCentreLocation = new PlayerLocation(256.5, 80, 264);
		
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
			++Tick;
			
			int playerCount = 0;
			if (_hubLevel == null)
			{
				_hubLevel = SkyCoreAPI.Instance.GetHubLevel();
			}
			else
			{
				playerCount = _hubLevel.PlayerCount;

				foreach (SkyPlayer player in _hubLevel.Players.Values)
				{
					//Update BarHandlers for all online players every 500 milliseconds (10 ticks)
					if (Tick % 10 == 0)
					{
						player.BarHandler?.DoTick();
					}
					if (Tick % 5 == 0)
					{
						if (IsInPortal(player.KnownPosition))
						{
							PlayerLocation teleportLocation = player.KnownPosition;
							teleportLocation.Z -= 2;

							player.Teleport(teleportLocation);

							GameUtil.ShowGameList(player);
						}
					}
				}

				//Do Hub Particles
				for (int i = 0; i < 10; i++)
				{
					Vector3 particleLocation = HubCentreLocation.ToVector3();

					particleLocation.X += (Random.Next(2) == 0 ? -1 : 1) * (float) (Random.NextDouble() * 25);
					particleLocation.Y += (Random.Next(2) == 0 ? -1 : 1) * (float) (Random.NextDouble() * 15);
					particleLocation.Z += (Random.Next(2) == 0 ? -1 : 1) * (float) (Random.NextDouble() * 25);


					McpeLevelEvent particleEvent = McpeLevelEvent.CreateObject();
					particleEvent.eventId = 0x4000 | (int) ParticleType.WitchSpell;
					particleEvent.position = particleLocation;
					particleEvent.data = 13369599;
					_hubLevel.RelayBroadcast(particleEvent);
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

		private bool IsInPortal(PlayerLocation playerLocation)
		{
			return
				playerLocation.X >= 253 && playerLocation.X <= 259 &&
				playerLocation.Y >= 77 && playerLocation.Y <= 83 &&
				playerLocation.Z >= 276 && playerLocation.Z <= 279;
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

			player.SpawnLevel(_hubLevel, _hubLevel.SpawnPoint, true);

			NightVision nightVision = new NightVision
			{
				Duration = int.MaxValue,
				Level = 0,
				Particles = false
			};
			player.SetEffect(nightVision);
			
			player.Inventory.SetInventorySlot(4, new ItemNavigationCompass());

			player.Inventory.SetHeldItemSlot(4);

			RunnableTask.RunTaskLater(() =>
			{
				player.Inventory.SetHeldItemSlot(4);
			}, 2000);
		}

		public override void CheckCapacity()
		{

		}

		public override Type GetGameLevelInfoType()
		{
			return typeof(GameLevelInfo); //Nothing Custom
		}

		public void DoInteract(int interactId, SkyPlayer player, SkyPlayer target)
		{
			SkyUtil.log($"Handling Hub Interacting from {player.Username} ID:{interactId}");
			if (player.Inventory.GetItemInHand() is ItemNavigationCompass)
			{
				GameUtil.ShowGameList(player);
			}
		}

	}
}
