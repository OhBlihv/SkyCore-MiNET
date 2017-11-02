using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using MiNET.Effects;
using MiNET.Net;
using MiNET.Particles;
using MiNET.Utils;
using SkyCore.Entities;
using SkyCore.Game;
using SkyCore.Game.Level;
using SkyCore.Game.State;
using SkyCore.Games.Hub.Items;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Games.Hub.State
{
	public class HubState : GameState
	{
		public override void EnterState(GameLevel gameLevel)
		{
			gameLevel.AddPendingTask(() =>
			{
				SkyUtil.log("Spawning all NPCs for " + gameLevel.GameId);
				PlayerNPC.SpawnAllHubNPCs(gameLevel as HubLevel);
			});
		}

		public override void LeaveState(GameLevel gameLevel)
		{
			//
		}

		public override bool CanAddPlayer(GameLevel gameLevel)
		{
			return gameLevel.GetPlayerCount() < 100; //TODO: Add Constant
		}

		public override void InitializePlayer(GameLevel gameLevel, SkyPlayer player)
		{
			if (!player.Effects.ContainsKey(EffectType.NightVision))
			{
				NightVision nightVision = new NightVision
				{
					Duration = int.MaxValue,
					Level = 0,
					Particles = false
				};
				player.SetEffect(nightVision);
			}

			if (!(player.Inventory.Slots[4] is ItemNavigationCompass))
			{
				player.Inventory.SetInventorySlot(4, new ItemNavigationCompass());

				player.Inventory.SetHeldItemSlot(4);

				RunnableTask.RunTaskLater(() =>
				{
					player.Inventory.SetHeldItemSlot(4);
				}, 1000);
			}
		}

		private const int ParticleEventCount = 10;

		private static readonly PlayerLocation HubCentreLocation = new PlayerLocation(256.5, 80, 264);

		private static readonly Random Random = new Random();

		public override void OnTick(GameLevel gameLevel, int currentTick, out int outTick)
		{
			outTick = currentTick;

			//Update BarHandlers for all online players every 500 milliseconds (1 tick)
			gameLevel.DoForAllPlayers(player => player.BarHandler?.DoTick());

			if (currentTick % 2 == 0)
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

			if (currentTick % 5 == 0)
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
					else if (IsInInvisRegion(player.KnownPosition))
					{
						if (player.IsGameSpectator)
						{
							continue;
						}
						
						SkyUtil.log($"Isnt Game Spectator in team {player.GameTeam.DisplayName}. Setting to Spectator");
						gameLevel.SetPlayerTeam(player, HubTeam.Spectator);
					}
					else if (player.IsGameSpectator)
					{
						SkyUtil.log($"Is Game Spectator in team {player.GameTeam.DisplayName}. Setting to Player");
						gameLevel.SetPlayerTeam(player, HubTeam.Player);
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

		private bool IsInInvisRegion(PlayerLocation playerLocation)
		{
			return
				playerLocation.X >= 252 && playerLocation.X <= 261 &&
				playerLocation.Y >= 77 && playerLocation.Y <= 83 &&
				playerLocation.Z >= 252 && playerLocation.Z <= 258;
		}
	
		public override GameState GetNextGameState(GameLevel gameLevel)
		{
			return this; //No state changing
		}

		public override StateType GetEnumState(GameLevel gameLevel)
		{
			return StateType.PreGame; //Allow all joins
		}

		public override bool DoInteract(GameLevel gameLevel, int interactId, SkyPlayer player, SkyPlayer target)
		{
			SkyUtil.log($"Handling Hub Interacting from {player.Username} ID:{interactId}");
			if (player.Inventory.GetItemInHand() is ItemNavigationCompass)
			{
				GameUtil.ShowGameList(player);
				RunnableTask.RunTaskLater(() => player.Inventory.SetInventorySlot(player.Inventory.InHandSlot, new ItemNavigationCompass()), 250);
			}

			return false;
		}
	}
}
