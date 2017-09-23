﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiNET;
using MiNET.Blocks;
using MiNET.Entities;
using MiNET.Items;
using MiNET.Utils;
using SkyCore.Game;
using SkyCore.Game.State;
using SkyCore.Game.State.Impl;
using SkyCore.Games.Murder;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Games.BuildBattle.State
{
	class BuildBattleBuildState : RunningState
	{

		//private const int MaxGameTime = 120;
		private const int MaxGameTime = 30;
		private const int PreStartTime = 10;

		public string SelectedCategory { get; private set; }

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
						player.Teleport(gameTeam.SpawnLocation);
						player.MovementSpeed = 0f;
						player.SendUpdateAttributes();
					}
				}
				
				List<string> categoryRotation = new List<string> { "§7Castle", "§cCar", "§6House", "§aTree", "§cVolcano", "§aPark", "§bLake" };
				for (int i = 0; i < 12; i++)
				{
					string category = categoryRotation[i % categoryRotation.Count];
					foreach (MiNET.Player player in players)
					{
						TitleUtil.SendCenteredSubtitle(player, category);
					}

					Thread.Sleep(250);
				}

				SelectedCategory = categoryRotation[new Random().Next(categoryRotation.Count)];
				gameLevel.DoForAllPlayers(player =>
				{
					player.MovementSpeed = 0.1f;
					player.SendUpdateAttributes();

					TitleUtil.SendCenteredSubtitle(player, "§fCategory:\n" + SelectedCategory);
				});
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
				player.BarHandler.AddMajorLine($"§d§lTime Remaining:§r §e{neatRemaining} §f| §d§lCategory:§r {SelectedCategory}", 2);
			});
		}

		public override bool HandleBlockPlace(GameLevel gameLevel, SkyPlayer player, Block existingBlock, Block targetBlock)
		{
			BlockCoordinates centreLocation = ((BuildBattleTeam) player.GameTeam).SpawnLocation.GetCoordinates3D();
			BlockCoordinates interactLocation = targetBlock.Coordinates;

			if (Math.Abs(centreLocation.X - interactLocation.X) > 10 ||
			    Math.Abs(centreLocation.Z - interactLocation.Z) > 10 ||
				interactLocation.Y < centreLocation.Y || 
				interactLocation.Y > 150) //TODO: Check heights
			{
				player.BarHandler.AddMinorLine("§c§l(!)§r §cYou can only build within your build zone §c§l(!)§r");
				return true;
			}

			return false;
		}

		public override bool HandleBlockBreak(GameLevel gameLevel, SkyPlayer player, Block block, List<Item> drops)
		{
			BlockCoordinates centreLocation = ((BuildBattleTeam)player.GameTeam).SpawnLocation.GetCoordinates3D();
			BlockCoordinates interactLocation = block.Coordinates;

			if (Math.Abs(centreLocation.X - interactLocation.X) > 20 ||
			    Math.Abs(centreLocation.Z - interactLocation.Z) > 20 ||
			    interactLocation.Y < (centreLocation.Y - 1) ||
			    interactLocation.Y > 106) //TODO: Check heights (spawn heights are ~66)
			{
				SkyUtil.log($"{interactLocation.X}:{interactLocation.Y}:{interactLocation.Z} vs {centreLocation.X}:{centreLocation.Y}:{centreLocation.Z} " +
				            $"({Math.Abs(centreLocation.X - interactLocation.X)}, {Math.Abs(centreLocation.Z - interactLocation.Z)}, " +
				            $"{interactLocation.Y < (centreLocation.Y - 1)}, {interactLocation.Y > 150})");
				player.BarHandler.AddMinorLine("§c§l(!)§r §cYou can only build within your build zone §c§l(!)§r");
				return true;
			}

			return false;
		}
	}
}