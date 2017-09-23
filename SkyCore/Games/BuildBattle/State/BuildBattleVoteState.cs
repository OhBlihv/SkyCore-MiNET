using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiNET.Utils;
using SkyCore.Game;
using SkyCore.Game.State;
using SkyCore.Game.State.Impl;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Games.BuildBattle.State
{
	class BuildBattleVoteState : RunningState
	{

		private const int MaxVoteTime = 10 * 2;

		private int _currentVotingTeam = -1;
		private SkyPlayer _currentVotingPlayer = null;

		private int _endTick = -1; //Default value

		public override void EnterState(GameLevel gameLevel)
		{
			base.EnterState(gameLevel);
		}

		public override GameState GetNextGameState(GameLevel gameLevel)
		{
			//return new VoidGameState();
			return new BuildBattleLobbyState(); //TODO: TEMP
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
				} while (nextVotePlayer == null && _currentVotingTeam < 10);

				if (nextVotePlayer == null)
				{
					gameLevel.UpdateGameState(GetNextGameState(gameLevel));
					return;
				}

				_currentVotingPlayer = nextVotePlayer;

				_endTick = gameLevel.Tick + MaxVoteTime;

				int i = -1;
				gameLevel.DoForAllPlayers(player =>
				{
					player.SetAllowFly(true);
					player.IsFlying = true;
					player.SendAdventureSettings();

					int innerI;
					if (player == _currentVotingPlayer)
					{
						innerI = -1;
					}
					else
					{
						i++;
						innerI = i;
					}
					
					PlayerLocation voteLocation = (PlayerLocation) gameTeam.SpawnLocation.Clone();
					voteLocation.Y += 10;
					switch (innerI)
					{
						case 0:
							voteLocation.X -= 5;
							break;	
						case 1:
							voteLocation.X -= 2.5f;
							voteLocation.Z -= 2.5f;
							break;
						case 2:
							voteLocation.Z -= 5;
							break;
						case 3:
							voteLocation.X += 2.5f;
							voteLocation.Z -= 2.5f;
							break;
						case 4:
							voteLocation.X += 5;
							break;
						case 5:
							voteLocation.X += 2.5f;
							voteLocation.Z += 2.5f;
							break;
						case 6:
							voteLocation.Z += 5;
							break;
						case 7:
							voteLocation.X -= 2.5f;
							voteLocation.Z += 2.5f;
							break;
					}
					
					player.Teleport(voteLocation);

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
				player.BarHandler.AddMajorLine($"§d§lVoting for {_currentVotingPlayer.Username}§r §e{neatRemaining}", 2);
			});
		}

	}
}
