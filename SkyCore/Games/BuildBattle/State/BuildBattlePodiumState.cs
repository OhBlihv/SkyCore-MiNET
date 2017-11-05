using System.Collections.Concurrent;
using System.Collections.Generic;
using MiNET.Utils;
using SkyCore.Game.Level;
using SkyCore.Game.State;
using SkyCore.Game.State.Impl;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Games.BuildBattle.State
{

	class BuildBattlePodiumState : RunningState
	{

		private readonly ConcurrentDictionary<SkyPlayer, int> _voteTally;

		public BuildBattlePodiumState(ConcurrentDictionary<SkyPlayer, int> voteTally)
		{
			_voteTally = voteTally;
		}
		
		public override void EnterState(GameLevel gameLevel)
		{
			base.EnterState(gameLevel);

			gameLevel.DoForAllPlayers(player =>
			{
				player.Inventory.Clear();
			});

			List<KeyValuePair<SkyPlayer, int>> topPlayers = new List<KeyValuePair<SkyPlayer, int>>();
			foreach (SkyPlayer player in _voteTally.Keys)
			{
				topPlayers.Add(new KeyValuePair<SkyPlayer, int>(player, _voteTally[player]));
			}

			topPlayers.Sort((x, y) => 0 - x.Value.CompareTo(y.Value));

			SkyPlayer winningPlayer = null;

			int i = 0;
			foreach (KeyValuePair<SkyPlayer, int> topPlayer in topPlayers)
			{
				if (++i > 1) //Top 1 Player for now
				{
					break;
				}

				//If the player has left, skip them.
				if (!gameLevel.PlayerTeamDict.ContainsKey(topPlayer.Key.Username))
				{
					i = 0;
				}
				else
				{
					winningPlayer = topPlayer.Key;
				}
			}

			string winningPlayerName = winningPlayer == null ? "NO-ONE" : winningPlayer.Username;

			List<PlayerLocation> podiumLocations;
			if (winningPlayer == null)
			{
				//Pick a random player
				podiumLocations = ((BuildBattleLevel)gameLevel).GetVoteLocations((BuildBattleTeam)topPlayers[0].Key.GameTeam);
			}
			else
			{
				podiumLocations = ((BuildBattleLevel) gameLevel).GetVoteLocations((BuildBattleTeam) winningPlayer.GameTeam);
			}

			int j = 0;
			gameLevel.DoForAllPlayers(player =>
			{
				player.Inventory.Clear();

				TitleUtil.SendCenteredSubtitle(player, $"§a§lWinner:§r §d{winningPlayerName}");

				if (player == winningPlayer)
				{
					PlayerLocation teleportLocation = (PlayerLocation)((BuildBattleTeam)player.GameTeam).SpawnLocation.Clone();
					teleportLocation.Y += 10;

					player.Teleport(teleportLocation);
				}
				else
				{
					player.Teleport(podiumLocations[j]);

					j++;
				}
			});

			RunnableTask.RunTaskLater(() =>
			{
				gameLevel.UpdateGameState(GetNextGameState(gameLevel));
			}, 5000);
		}

		public override GameState GetNextGameState(GameLevel gameLevel)
		{
			return new BuildBattleEndState();
		}

	}

}
