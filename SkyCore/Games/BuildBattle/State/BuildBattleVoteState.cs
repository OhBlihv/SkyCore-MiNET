using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiNET.Utils;
using MiNET.Worlds;
using SkyCore.Game;
using SkyCore.Game.State;
using SkyCore.Game.State.Impl;
using SkyCore.Games.BuildBattle.Items;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Games.BuildBattle.State
{
	class BuildBattleVoteState : RunningState
	{

		private const int MaxVoteTime = 30 * 2;

		private int _currentVotingTeam = -1;
		private SkyPlayer _currentVotingPlayer = null;

		private int _endTick = -1; //Default value

		private ConcurrentDictionary<SkyPlayer, int> VoteTally = new ConcurrentDictionary<SkyPlayer, int>();

		public override void EnterState(GameLevel gameLevel)
		{
			base.EnterState(gameLevel);
		}

		public override GameState GetNextGameState(GameLevel gameLevel)
		{
			return new BuildBattlePodiumState(VoteTally);
		}

		public override void OnTick(GameLevel gameLevel, int currentTick, out int outTick)
		{
			base.OnTick(gameLevel, currentTick, out outTick);

			int secondsLeft;
			if (_endTick == -1)
			{
				secondsLeft = 0; //Trigger a initial refresh
			}
			else
			{
				secondsLeft = (_endTick - currentTick) / 2;
			}

			if (secondsLeft > (MaxVoteTime / 2))
			{
				return; //Ignore until the ticker has finished
			}
			if (secondsLeft == 0)
			{
				//Can't execute first run
				if (_currentVotingPlayer != null)
				{
					//Tally up votes

					int currentVoteCount = 0;

					gameLevel.DoForAllPlayers(player =>
					{
						switch (player.Inventory.InHandSlot)
						{
							case 2:
								currentVoteCount += 1;
								break;
							case 3:
								currentVoteCount += 2;
								break;
							case 4:
								currentVoteCount += 3;
								break;
							case 5:
								currentVoteCount += 4;
								break;
							case 6:
								currentVoteCount += 5;
								break;
							default:
								currentVoteCount += 3; //3/5 vote if invalid
								break;
						}
					});

					VoteTally.TryAdd(_currentVotingPlayer, currentVoteCount);
				}
				else
				{
					//Reset gamemode during voting phase
					gameLevel.DoForAllPlayers(player =>
					{
						player.UseCreativeInventory = false;
						player.SetGameMode(GameMode.Adventure);
					});
				}

				//Pick another player, or end the phase if all voting has finished
				BuildBattleLevel buildLevel = (BuildBattleLevel) gameLevel;

				SkyPlayer nextVotePlayer = null;
				BuildBattleTeam gameTeam;
				do
				{
					gameTeam = buildLevel.BuildTeams[++_currentVotingTeam];

					List<SkyPlayer> teamPlayer = buildLevel.GetPlayersInTeam(gameTeam);
					if (teamPlayer.Count > 0)
					{
						nextVotePlayer = teamPlayer[0];
					}
				} while (nextVotePlayer == null && _currentVotingTeam < buildLevel.BuildTeams.Count - 1);

				if (nextVotePlayer == null)
				{
					gameLevel.UpdateGameState(GetNextGameState(gameLevel));
					return;
				}

				_currentVotingPlayer = nextVotePlayer;

				_endTick = gameLevel.Tick + MaxVoteTime;

				List<PlayerLocation> voteLocations = ((BuildBattleLevel) gameLevel).GetVoteLocations((BuildBattleTeam) _currentVotingPlayer.GameTeam);

				int i = -1;
				gameLevel.DoForAllPlayers(player =>
				{
					player.SetAllowFly(true);
					player.IsFlying = true;
					player.SendAdventureSettings();

					if (player == _currentVotingPlayer)
					{
						player.Inventory.Clear(); //Remove voting possibility

						PlayerLocation teleportLocation = (PlayerLocation) ((BuildBattleTeam) player.GameTeam).SpawnLocation.Clone();
						teleportLocation.Y += 10;

						player.Teleport(teleportLocation);
					}
					else
					{
						i++;

						for (int j = 1; j < 6; j++)
						{
							player.Inventory.SetInventorySlot(1 + j, new ItemVote(j, _currentVotingPlayer.Username));
						}

						player.Inventory.SetHeldItemSlot(4); //Default to middle rating if AFK

						player.Teleport(voteLocations[i]);
					}

					TitleUtil.SendCenteredSubtitle(player, $"§d§lNow Voting for:\n§e{nextVotePlayer.Username}");
				});
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
				player.BarHandler.AddMinorLine($"§d§lVoting for {_currentVotingPlayer.Username}§r §e{neatRemaining}", 2);


				if (player != _currentVotingPlayer)
				{
					int heldSlot = player.Inventory.InHandSlot;
					if (heldSlot < 2)
					{
						heldSlot = 2;
						player.Inventory.SetHeldItemSlot(2);
					}
					else if (heldSlot > 6)
					{
						heldSlot = 6;
						player.Inventory.InHandSlot = 6;
					}

					string voteName = "N/A";
					switch (heldSlot)
					{
						case 2:
							voteName = ItemVote.GetVoteName(1);
							break;
						case 3:
							voteName = ItemVote.GetVoteName(2);
							break;
						case 4:
							voteName = ItemVote.GetVoteName(3);
							break;
						case 5:
							voteName = ItemVote.GetVoteName(4);
							break;
						case 6:
							voteName = ItemVote.GetVoteName(5);
							break;
					}

					player.BarHandler.AddMajorLine($"§r§d§lVote:§r {voteName}§r");
				}
			});
		}

	}
}
