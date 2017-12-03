using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MiNET.Utils;
using MiNET.Worlds;
using SkyCore.Game.Level;
using SkyCore.Game.State;
using SkyCore.Game.State.Impl;
using SkyCore.Games.BuildBattle.Items;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Games.BuildBattle.State
{

	class BuildBattleVoteTickableInformation : ITickableInformation
	{

		public SkyPlayer BuildingPlayer { get; set; }

		public string NeatTimeRemaining { get; set; }

	}

	class BuildBattleVoteState : RunningState, IMessageTickableState
	{

		private const int MaxVoteTime = 15 * 2;

		private int _currentVotingTeam = -1;
		private SkyPlayer _currentVotingPlayer;

		private readonly ConcurrentDictionary<SkyPlayer, int> _voteTally = new ConcurrentDictionary<SkyPlayer, int>();

		public override void EnterState(GameLevel gameLevel)
		{
			base.EnterState(gameLevel);

			gameLevel.AllowBreak = false;
			gameLevel.AllowBuild = false;

			gameLevel.DoForAllPlayers(player =>
			{
				player.Inventory.Clear();

				player.SetNameTagVisibility(true);

				//Toggle Flight
				player.SetAllowFly(true);
				player.IsFlying = true;

				player.SendAdventureSettings();
			});
		}

		public override GameState GetNextGameState(GameLevel gameLevel)
		{
			return new BuildBattlePodiumState(_voteTally);
		}

		public override void OnTick(GameLevel gameLevel, int currentTick, out int outTick)
		{
			base.OnTick(gameLevel, currentTick, out outTick);

			int secondsLeft;
			if (EndTick == -1)
			{
				secondsLeft = 0; //Trigger a initial refresh
			}
			else
			{
				secondsLeft = (EndTick - currentTick) / 2;
			}

			if (secondsLeft > MaxVoteTime / 2)
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
								if (player.Inventory.InHandSlot < 2)
								{
									currentVoteCount += 1; //2
								}
								else if (player.Inventory.InHandSlot > 6)
								{
									currentVoteCount += 5;
								}
								break;
						}
					});

					_voteTally.TryAdd(_currentVotingPlayer, currentVoteCount);
				}
				else
				{
					//Reset gamemode during voting phase
					gameLevel.DoForAllPlayers(player =>
					{
						//player.UseCreativeInventory = false;
						player.UpdateGameMode(GameMode.Adventure, true);
					});
				}

				//Pick another player, or end the phase if all voting has finished
				BuildBattleLevel buildLevel = (BuildBattleLevel) gameLevel;

				SkyPlayer nextVotePlayer = null;
				do
				{
					var gameTeam = buildLevel.BuildTeams[++_currentVotingTeam];

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

				EndTick = gameLevel.Tick + MaxVoteTime;

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

						PlayerLocation voteSpawnLocation = voteLocations[i];
						BlockCoordinates voteSpawnCoordinates = voteSpawnLocation.GetCoordinates3D();
						while (!player.Level.IsAir(voteSpawnCoordinates))
						{
							voteSpawnCoordinates.Y++;
						}
						
						voteSpawnLocation = new PlayerLocation(voteSpawnCoordinates.X, voteSpawnCoordinates.Y, voteSpawnCoordinates.Z,
																voteSpawnLocation.HeadYaw, voteSpawnLocation.Yaw, voteSpawnLocation.Pitch);
						
						player.Teleport(voteSpawnLocation);
					}

					TitleUtil.SendCenteredSubtitle(player, $"§d§lVoting for:\n§7{nextVotePlayer.Username}");
				});
			}

			gameLevel.DoForAllPlayers(player =>
			{
				SendTickableMessage(gameLevel, player, GetTickableInformation(_currentVotingPlayer));
			});
		}

		public void SendTickableMessage(GameLevel gameLevel, SkyPlayer player, ITickableInformation tickableInformation)
		{
			BuildBattleVoteTickableInformation voteInformation = tickableInformation as BuildBattleVoteTickableInformation ??
				GetTickableInformation(_currentVotingPlayer) as BuildBattleVoteTickableInformation;

			if (voteInformation == null)
			{
				SkyUtil.log("Unable to process TickableInformation. == null");
				return;
			}

			string voteString = "";
			if (player != voteInformation.BuildingPlayer) 
			{
				//Do not set the held item slot, since this will cause a recursive loop.
				int heldSlot = player.Inventory.InHandSlot;
				if (heldSlot < 2)
				{
					heldSlot = 2;
				}
				else if (heldSlot > 6)
				{
					heldSlot = 6;
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

				try
				{
					voteString = $" | {voteName ?? "N/A"} §fSelected...";
				}
				catch (Exception e)
				{
					//Console.WriteLine(e);
					//Ignore this dumb problem. Only happens on the first tick(?)
				}

				player.BarHandler.AddMinorLine("§6(Please hold your vote selection)");
			}

			player.BarHandler.AddMajorLine($"§d§lBUILDER§r §f{voteInformation.BuildingPlayer.Username}§r §7| {voteInformation.NeatTimeRemaining} §fRemaining{voteString}", 2);
		}

		public ITickableInformation GetTickableInformation(SkyPlayer player)
		{
			return new BuildBattleVoteTickableInformation
			{
				NeatTimeRemaining = GetNeatTimeRemaining(GetSecondsLeft()),
				BuildingPlayer = player
			};
		}

	}
}
