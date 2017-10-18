using System;
using System.Collections.Generic;
using System.Numerics;
using MiNET.Net;
using MiNET.Particles;
using MiNET.Utils;
using MiNET.Worlds;
using SkyCore.Game;
using SkyCore.Game.Level;
using SkyCore.Games.Hub.Items;
using SkyCore.Games.Murder;
using SkyCore.Player;

namespace SkyCore.Games.Hub
{
	public class HubCoreController : CoreGameController
	{

		public const int MaxHubCount = 5;
		
		private static readonly PlayerLocation HubCentreLocation = new PlayerLocation(256.5, 80, 264);

		public HubCoreController(SkyCoreAPI plugin) : base(plugin, "hub", "Hub", new List<string>{"hub"})
		{
			Tick = 1;
			
			ExternalGameHandler.RegisterGameIntent("murder");
			ExternalGameHandler.RegisterGameIntent("build-battle");
			ExternalGameHandler.RegisterGameIntent("block-hunt");
			ExternalGameHandler.RegisterGameIntent("bed-wars");
			
			//Register all hubs
			for (int i = 0; i < MaxHubCount; i++)
			{
				GetGameController();
			}
		}

		protected override GameLevel _getGameController()
		{
			return new HubLevel(Plugin, GetNextGameId(), GetRandomLevelName());
		}

		private const int ParticleEventCount = 6;

		protected override void CoreGameTick()
		{
			base.CoreGameTick();
			
			//TODO: Move this to the hub state?
			foreach (GameLevel gameLevel in GameLevels.Values)
			{
				if (Tick % 2 == 0)
				{
					//Do Hub Particles
					for (int i = 0; i < ParticleEventCount; i++)
					{
						Vector3 particleLocation = HubCentreLocation.ToVector3();

						particleLocation.X += (Random.Next(2) == 0 ? -1 : 1) * (float)(Random.NextDouble() * 25);
						particleLocation.Y += (Random.Next(2) == 0 ? -1 : 1) * (float)(Random.NextDouble() * 15);
						particleLocation.Z += (Random.Next(2) == 0 ? -1 : 1) * (float)(Random.NextDouble() * 25);

						McpeLevelEvent particleEvent = McpeLevelEvent.CreateObject();
						particleEvent.eventId = 0x4000 | (int)ParticleType.WitchSpell;
						particleEvent.position = particleLocation;
						particleEvent.data = 13369599;
						gameLevel.RelayBroadcast(particleEvent);
					}
				}

				if (Tick % 5 == 0)
				{
					foreach (SkyPlayer player in gameLevel.Players.Values)
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
			}
		}

		private bool IsInPortal(PlayerLocation playerLocation)
		{
			return
				playerLocation.X >= 253 && playerLocation.X <= 259 &&
				playerLocation.Y >= 77 && playerLocation.Y <= 83 &&
				playerLocation.Z >= 276 && playerLocation.Z <= 279;
		}

		public override SortedSet<GameLevel> GetMostViableGames()
		{
			SortedSet<GameLevel> mostViableGames = new SortedSet<GameLevel>();

			foreach (GameLevel gameLevel in GameLevels.Values)
			{
				if (gameLevel.CurrentState.GetEnumState(gameLevel).IsJoinable())
				{
					mostViableGames.Add(gameLevel);
				}
			}

			return mostViableGames;
		}

		public override void CheckCapacity()
		{
			//Ignored. Static Hub Count
		}

		public override Type GetGameLevelInfoType()
		{
			return typeof(GameLevelInfo); //Nothing Custom
		}

	}
}
