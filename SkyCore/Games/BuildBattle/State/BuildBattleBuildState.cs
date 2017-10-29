using System;
using System.Collections.Generic;
using System.Threading;
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

		private const int MaxGameTime = 60 * 2;
		//private const int MaxGameTime = 300 * 2;
		private const int PreStartTime = 10;

		public BuildBattleTheme SelectedCategory { get; private set; }

		private int _endTick = -1; //Default value

		public override void EnterState(GameLevel gameLevel)
		{
			_endTick = gameLevel.Tick + MaxGameTime + PreStartTime;

			RunnableTask.RunTask(() =>
			{
				ICollection<MiNET.Player> players = new List<MiNET.Player>(gameLevel.Players.Values);

				foreach (BuildBattleTeam gameTeam in ((BuildBattleLevel) gameLevel).BuildTeams)
				{
					foreach (SkyPlayer player in gameLevel.GetPlayersInTeam(gameTeam))
					{
						player.IsWorldImmutable = true; //Allow breaking
						player.IsWorldBuilder = false;
						player.SendAdventureSettings();

						player.Teleport(gameTeam.SpawnLocation);

						player.SetNoAi(true);

						player.UseCreativeInventory = true;
						player.UpdateGameMode(GameMode.Creative, false);
					}
				}

				List<BuildBattleTheme> categoryRotation = ((BuildBattleLevel) gameLevel).ThemeList;
				for (int i = 0; i < 12; i++)
				{
					BuildBattleTheme category = categoryRotation[i % categoryRotation.Count];
					foreach (SkyPlayer player in players)
					{
						TitleUtil.SendCenteredSubtitle(player, category.ThemeName);

						//Poorly enforce speed
						if (i == 0 || i == 11)
						{
							player.SetNoAi(true);
						}
					}

					Thread.Sleep(250);
				}

				SelectedCategory = categoryRotation[new Random().Next(categoryRotation.Count)];
				gameLevel.DoForAllPlayers(player =>
				{
					player.SetNoAi(false);
					
					player.IsWorldImmutable = true; //Allow breaking
					player.IsWorldBuilder = false;
					player.SendAdventureSettings();
					
					player.UpdateGameMode(GameMode.Creative, true);

					TitleUtil.SendCenteredSubtitle(player, "§fCategory:\n" + TextUtils.Center(SelectedCategory.ThemeName, "§eCategory:".Length));
					
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

		public override GameState GetNextGameState(GameLevel gameLevel)
		{
			return new BuildBattleVoteState();
		}

		public override void OnTick(GameLevel gameLevel, int currentTick, out int outTick)
		{
			base.OnTick(gameLevel, currentTick, out outTick);

			int secondsLeft = (_endTick - currentTick) / 2;

			if (secondsLeft > (MaxGameTime / 2))
			{
				return; //Ignore until the ticker has finished
			}
			if (secondsLeft == 0)
			{
				gameLevel.UpdateGameState(GetNextGameState(gameLevel));
				return;
			}

			string neatRemaining;
			{
				int minutes = 0;
				while (secondsLeft >= 60)
				{
					secondsLeft -= 60;
					minutes++;
				}

				neatRemaining = minutes + ":";

				if (secondsLeft < 10)
				{
					neatRemaining += "0" + secondsLeft;
				}
				else
				{
					neatRemaining += secondsLeft;
				}
			}

			gameLevel.DoForAllPlayers(player =>
			{
				player.BarHandler.AddMajorLine($"§d§lTime Remaining:§r §e{neatRemaining} §f| §d§lCategory:§r §f{SelectedCategory.ThemeName}§r", 2);
			});
		}

		public override bool DoInteract(GameLevel gameLevel, int interactId, SkyPlayer player, SkyPlayer target)
		{
			SkyUtil.log($"{player.Username} attempted block interaction:{interactId} at ");
			
			return base.DoInteract(gameLevel, interactId, player, target);
		}

		public const int PlotRadius = 13;
		public const int MaxHeight = 92;

		public override bool HandleBlockPlace(GameLevel gameLevel, SkyPlayer player, Block existingBlock, Block targetBlock)
		{
			BlockCoordinates centreLocation = ((BuildBattleTeam) player.GameTeam).SpawnLocation.GetCoordinates3D();
			BlockCoordinates interactLocation = targetBlock.Coordinates;

			if (Math.Abs(centreLocation.X - interactLocation.X) > PlotRadius ||
			    Math.Abs(centreLocation.Z - interactLocation.Z) > PlotRadius ||
			    interactLocation.Y < (centreLocation.Y - 1) ||
			    interactLocation.Y > MaxHeight) //TODO: Check heights (spawn heights are ~66)
			{
				SkyUtil.log($"{interactLocation.X}:{interactLocation.Y}:{interactLocation.Z} vs {centreLocation.X}:{centreLocation.Y}:{centreLocation.Z} " +
				            $"({Math.Abs(centreLocation.X - interactLocation.X)}, {Math.Abs(centreLocation.Z - interactLocation.Z)}, " +
				            $"{interactLocation.Y < (centreLocation.Y - 1)}, {interactLocation.Y > 150})");
				player.BarHandler.AddMinorLine("§c§l(!)§r §cYou can only build within your build zone §c§l(!)§r");
				return true;
			}

			return false;
		}

		public override bool HandleBlockBreak(GameLevel gameLevel, SkyPlayer player, Block block, List<Item> drops)
		{
			BlockCoordinates centreLocation = ((BuildBattleTeam)player.GameTeam).SpawnLocation.GetCoordinates3D();
			BlockCoordinates interactLocation = block.Coordinates;

			if (Math.Abs(centreLocation.X - interactLocation.X) > PlotRadius ||
			    Math.Abs(centreLocation.Z - interactLocation.Z) > PlotRadius ||
			    interactLocation.Y < (centreLocation.Y - 1) ||
			    interactLocation.Y > MaxHeight) //TODO: Check heights (spawn heights are ~66)
			{
				SkyUtil.log($"{interactLocation.X}:{interactLocation.Y}:{interactLocation.Z} vs {centreLocation.X}:{centreLocation.Y}:{centreLocation.Z} " +
				            $"({Math.Abs(centreLocation.X - interactLocation.X)}, {Math.Abs(centreLocation.Z - interactLocation.Z)}, " +
				            $"{interactLocation.Y < centreLocation.Y - 1}, {interactLocation.Y > 150})");
				player.BarHandler.AddMinorLine("§c§l(!)§r §cYou can only build within your build zone §c§l(!)§r");
				return true;
			}

			return false;
		}
	}
}
