﻿using System;
using MiNET.Blocks;
using MiNET.Effects;
using MiNET.Utils;
using SkyCore.BugSnag;
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

		public const int MaxPlayers = 100;

		public override void EnterState(GameLevel gameLevel)
		{
			
		}

		public override void LeaveState(GameLevel gameLevel)
		{
			//
		}

		public override bool CanAddPlayer(GameLevel gameLevel)
		{
			return gameLevel.GetPlayerCount() < MaxPlayers;
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

				//Wait until the compass has appeared to change held slots
				RunnableTask.RunTaskLater(() =>
				{
					player.Inventory.SetHeldItemSlot(4);
				}, 500);
			}

			/*try
			{
				ISet<long> mapIds = MapUtil.GetLevelMapIds(gameLevel);
				if (mapIds == null)
				{
					SkyUtil.log(
						$"Attempted to respawn missing maps for {player.Username}, but no maps were registered for {gameLevel.GameId}");
				}
				else
				{
					RunnableTask.RunTaskLater(() =>
					{
						//Murder one as example
						Block block = gameLevel.GetBlock(new BlockCoordinates(260, 77, 270));

						var message = McpeUpdateBlock.CreateObject();
						message.blockId = block.Id;
						message.coordinates = block.Coordinates;
						message.blockMetaAndPriority = (byte)(0xb << 4 | (block.Metadata & 0xf));
						player.SendPackage(message);

						/*MapUtil.SpawnMapImage(@"C:\Users\Administrator\Desktop\dl\map-images\comingsoonmapimage.png", 1, 1, this,
							new BlockCoordinates(249, 77, 268), MapUtil.MapDirection.West);
						MapUtil.SpawnMapImage(@"C:\Users\Administrator\Desktop\dl\map-images\buildbattlemapimage.png", 1, 1, this,
							new BlockCoordinates(252, 77, 270), MapUtil.MapDirection.West);
						MapUtil.SpawnMapImage(@"C:\Users\Administrator\Desktop\dl\map-images\murdermapimage.png", 1, 1, this,
							new BlockCoordinates(260, 77, 270), MapUtil.MapDirection.West);
						MapUtil.SpawnMapImage(@"C:\Users\Administrator\Desktop\dl\map-images\comingsoonmapimage.png", 1, 1, this,
							new BlockCoordinates(263, 77, 268), MapUtil.MapDirection.West);#1#

						/*foreach (long mapEntityId in mapIds)
						{
							if (gameLevel.GetEntity(mapEntityId) is MapEntity mapEntity)
							{
								player.SendPackage(((MapImageProvider)mapEntity.ImageProvider).Batch);
							}
						}#1#
					}, 1000);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}*/
		}

		private const int ParticleEventCount = 10;

		private static readonly PlayerLocation HubCentreLocation = new PlayerLocation(256.5, 80, 264);

		private static readonly Random Random = new Random();

		public override void OnTick(GameLevel gameLevel, int currentTick, out int outTick)
		{
			outTick = currentTick;

			//Update BarHandlers for all online players every 500 milliseconds (1 tick)
			gameLevel.DoForAllPlayers(player => player.BarHandler?.DoTick());

			//DISABLE PARTICLES UNTIL THE 0,0 BUG IS FIXED
			/*if (currentTick % 2 == 0)
			{
				//Do Hub Particles
				for (int i = 0; i < ParticleEventCount; i++)
				{
					Vector3 particleLocation = HubCentreLocation.ToVector3();

					particleLocation.X += (Random.Next(2) == 0 ? -1 : 1) * (float)(Random.NextDouble() * 25);
					particleLocation.Y += (Random.Next(2) == 0 ? -1 : 1) * (float)(Random.NextDouble() * 15);
					particleLocation.Z += (Random.Next(2) == 0 ? -1 : 1) * (float)(Random.NextDouble() * 25);

					McpeLevelEvent particleEvent = McpeLevelEvent.CreateObject();
					particleEvent.eventId = 0x4000 | (int) ParticleType.WitchSpell;
					particleEvent.position = particleLocation;
					particleEvent.data = 13369599;
					gameLevel.RelayBroadcast(particleEvent);
				}
			}*/

			if (currentTick % 2 == 0)
			{
				foreach (SkyPlayer player in gameLevel.Players.Values)
				{
					//Player is not initialized yet.
					if (player == null || !player.IsConnected || player.KnownPosition == null)
					{
						continue;
					}

					if (IsInPortal(player.KnownPosition))
					{
						PlayerLocation teleportLocation = player.KnownPosition;
						teleportLocation.Z -= 2;

						player.Teleport(teleportLocation);

						try
						{
							GameUtil.ShowGameList(player);
						}
						catch (Exception e)
						{
							BugSnagUtil.ReportBug(e, this, player);
						}
					}
					else if (IsInInvisRegion(player.KnownPosition))
					{
						if (player.IsGameSpectator)
						{
							continue;
						}
						
						//SkyUtil.log($"Isnt Game Spectator in team {player.GameTeam.DisplayName}. Setting to Spectator");
						gameLevel.SetPlayerTeam(player, HubTeam.Spectator);
					}
					else if (player.IsGameSpectator)
					{
						//SkyUtil.log($"Is Game Spectator in team {player.GameTeam.DisplayName}. Setting to Player");
						gameLevel.SetPlayerTeam(player, HubTeam.Player);
					}
				}
			}
		}

		private static bool IsInPortal(PlayerLocation playerLocation)
		{
			return
				playerLocation.X >= 253 && playerLocation.X <= 259 &&
				playerLocation.Y >= 77 && playerLocation.Y <= 83 &&
				playerLocation.Z >= 276 && playerLocation.Z <= 279;
		}

		private static bool IsInInvisRegion(PlayerLocation playerLocation)
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

		public override bool DoInteractAtEntity(GameLevel gameLevel, int interactId, SkyPlayer player, SkyPlayer target)
		{
			return DoInteract(gameLevel, player, interactId);
		}

		public override bool DoInteractAtBlock(GameLevel gameLevel, int interactId, SkyPlayer player, Block block)
		{
			return DoInteract(gameLevel, player, interactId);
		}

		private bool DoInteract(GameLevel gameLevel, SkyPlayer player, int interactId)
		{
			//SkyUtil.log($"Handling Hub Interacting from {player.Username} ID:{interactId}");
			if (player.Inventory.GetItemInHand() is ItemNavigationCompass)
			{
				GameUtil.ShowGameList(player);
				//RunnableTask.RunTaskLater(() => player.Inventory.SetInventorySlot(player.Inventory.InHandSlot, new ItemNavigationCompass()), 250);
			}

			return true;
		}

		/*public override bool HandleBlockBreak(GameLevel gameLevel, SkyPlayer player, Block block, List<Item> drops)
		{
			return !player.PlayerGroup.IsAtLeast(PlayerGroup.Admin);
		}

		public override bool HandleBlockPlace(GameLevel gameLevel, SkyPlayer player, Block existingBlock, Block targetBlock)
		{
			return !player.PlayerGroup.IsAtLeast(PlayerGroup.Admin);
		}*/

	}
}
