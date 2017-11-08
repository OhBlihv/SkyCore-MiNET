﻿using System;
using System.Collections.Generic;
using System.Threading;
using MiNET;
using MiNET.Blocks;
using MiNET.Items;
using MiNET.Utils;
using MiNET.Worlds;
using SkyCore.Game.Level;
using SkyCore.Game.State;
using SkyCore.Game.State.Impl;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Games.BuildBattle.State
{
	class BuildBattleBuildState : RunningState
	{

		private const int PreStartTime = 10;

		public BuildBattleTheme SelectedCategory { get; private set; }

		public BuildBattleBuildState()
		{
			MaxGameTime = 60 * 2;
		}

		public override void EnterState(GameLevel gameLevel)
		{
			EndTick = gameLevel.Tick + MaxGameTime + PreStartTime;

			RunnableTask.RunTask(() =>
			{
				ICollection<MiNET.Player> players = new List<MiNET.Player>(gameLevel.Players.Values);

				foreach (BuildBattleTeam gameTeam in ((BuildBattleLevel) gameLevel).BuildTeams)
				{
					foreach (SkyPlayer player in gameLevel.GetPlayersInTeam(gameTeam))
					{
						player.IsWorldImmutable = true; //Prevent Breaking
						//player.IsWorldBuilder = false;
						player.Teleport(gameTeam.SpawnLocation);

						player.SetAllowFly(true);
						player.IsFlying = true;

						player.SendAdventureSettings();

						player.UseCreativeInventory = true;
						player.UpdateGameMode(GameMode.Creative, false);

						player.SetNameTagVisibility(false);
					}
				}

				List<BuildBattleTheme> categoryRotation = ((BuildBattleLevel) gameLevel).ThemeList;
				{
					int initialTheme = Random.Next(categoryRotation.Count);
					for (int i = initialTheme; i < (initialTheme + 12); i++)
					{
						BuildBattleTheme category = categoryRotation[i % categoryRotation.Count];
						foreach (MiNET.Player player in players)
						{
							TitleUtil.SendCenteredSubtitle(player, "§r\n" + category.ThemeName);
						}

						Thread.Sleep(250);
					}
				}

				SelectedCategory = categoryRotation[Random.Next(categoryRotation.Count)];
				gameLevel.DoForAllPlayers(player =>
				{
					player.IsWorldImmutable = true; //Allow breaking
					//player.IsWorldBuilder = false;
					player.SendAdventureSettings();
					
					player.UpdateGameMode(GameMode.Creative, true);

					string secondLine = "§fYou have §75 minutes§f, let's go!";

					TitleUtil.SendCenteredSubtitle(player, $"{SelectedCategory.ThemeName}\n{secondLine}");

					player.Inventory.Clear();
					for (int i = 0; i < SelectedCategory.TemplateItems.Count;i++)
					{
						player.Inventory.SetInventorySlot(i, SelectedCategory.TemplateItems[i].GetItem());
					}

					//Ensure this player is at the correct spawn location
					if (gameLevel.GetBlock(player.KnownPosition).Id != 0)
					{
						PlayerLocation newLocation = (PlayerLocation)player.KnownPosition.Clone();
						newLocation.Y++;

						player.Teleport(newLocation);
					}
				});

				gameLevel.AllowBreak = true;
				gameLevel.AllowBuild = true;
			});
		}

		public override void HandleLeave(GameLevel gameLevel, SkyPlayer player)
		{
			if (gameLevel.GetGamePlayerCount() <= 2)
			{
				//Not enough players for the game to continue!
				gameLevel.UpdateGameState(new BuildBattlePodiumState(null));
			}
		}

		public override GameState GetNextGameState(GameLevel gameLevel)
		{
			return new BuildBattleVoteState();
		}

		public override void OnTick(GameLevel gameLevel, int currentTick, out int outTick)
		{
			base.OnTick(gameLevel, currentTick, out outTick);

			int secondsLeft = (EndTick - currentTick) / 2;

			if (secondsLeft > (MaxGameTime / 2))
			{
				return; //Ignore until the ticker has finished
			}
			if (secondsLeft == 0)
			{
				gameLevel.UpdateGameState(GetNextGameState(gameLevel));
				return;
			}

			string neatRemaining = GetNeatTimeRemaining(secondsLeft);

			gameLevel.DoForAllPlayers(player =>
			{
				player.BarHandler.AddMajorLine($"§d§lTime Remaining:§r §e{neatRemaining} §f| §d§lCategory:§r §f{SelectedCategory.ThemeName}§r", 2);
			});
		}

		public override bool DoInteract(GameLevel gameLevel, int interactId, SkyPlayer player, SkyPlayer target)
		{
			return false; //Avoid cancelling to allow block placing
		}

		public const int PlotRadius = 13;
		public const int MaxHeight = 92;

		public override bool HandleBlockPlace(GameLevel gameLevel, SkyPlayer player, Block existingBlock, Block targetBlock)
		{
			BlockCoordinates centreLocation = ((BuildBattleTeam) player.GameTeam).SpawnLocation.GetCoordinates3D();
			BlockCoordinates interactLocation = existingBlock.Coordinates;

			return CanModifyAt(gameLevel, player, centreLocation, interactLocation);
		}

		public override bool HandleBlockBreak(GameLevel gameLevel, SkyPlayer player, Block block, List<Item> drops)
		{
			BlockCoordinates centreLocation = ((BuildBattleTeam)player.GameTeam).SpawnLocation.GetCoordinates3D();
			BlockCoordinates interactLocation = block.Coordinates;

			return CanModifyAt(gameLevel, player, centreLocation, interactLocation);
		}

		private bool CanModifyAt(GameLevel gameLevel, SkyPlayer player, BlockCoordinates centreLocation,
			BlockCoordinates interactLocation)
		{
			if (Math.Abs(centreLocation.X - interactLocation.X) > PlotRadius ||
			    Math.Abs(centreLocation.Z - interactLocation.Z) > PlotRadius ||
			    interactLocation.Y < (centreLocation.Y - 5) ||
			    interactLocation.Y > MaxHeight)
			{
				/*SkyUtil.log($"{interactLocation.X}:{interactLocation.Y}:{interactLocation.Z} vs {centreLocation.X}:{centreLocation.Y}:{centreLocation.Z} " +
				            $"({Math.Abs(centreLocation.X - interactLocation.X)}, {Math.Abs(centreLocation.Z - interactLocation.Z)}, " +
				            $"{interactLocation.Y < centreLocation.Y - 1}, {interactLocation.Y > 150})");*/
				player.BarHandler.AddMinorLine("§c§l(!)§r §cYou can only build within your build zone §c§l(!)§r");
				return true;
			}

			return false;
		}
	}
}
