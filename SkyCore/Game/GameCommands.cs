using System.Linq;
using MiNET.Plugins.Attributes;
using SkyCore.Game.Level;
using SkyCore.Game.State;
using SkyCore.Game.State.Impl;
using SkyCore.Permissions;
using SkyCore.Player;

namespace SkyCore.Game
{
	public class GameCommands
	{

		[Command(Name = "gameedit")]
		[Authorize(Permission = (int)PlayerGroupCommandPermissions.Admin)]
		public void CommandGameEdit(MiNET.Player player, params string[] args)
		{
			if (!(player is SkyPlayer skyPlayer) || !skyPlayer.PlayerGroup.IsAtLeast(PlayerGroup.Admin))
			{
				player.SendMessage("§c§l(!)§r §cYou do not have permission for this command.");
				return;
			}

			if (!(player.Level is GameLevel level))
			{
				player.SendMessage($"§cYou must be in a game to use this command!");
				return;
			}

			if (!(level.GameLevelInfo is GameLevelInfo gameLevelInfo))
			{
				player.SendMessage("§cThe current level's information could not be loaded.");
				return;
			}

			if(!SkyCoreAPI.Instance.GameModes.TryGetValue(level.GameType, out var gameController))
			{
				player.SendMessage($"§cCouldn't retrieve GameController for {level.GameType}");
				return;
			}

			if (args[0].Equals("timeleft"))
			{
				if (args.Length < 2)
				{
					player.SendMessage("§c/gameedit timeleft <time>");
					return;
				}

				if (!int.TryParse(args[1], out var timeRemaining))
				{
					player.SendMessage($"§cInvalid time remaining ({args[1]})");
					return;
				}

				level.Tick = 0;
				((RunningState)level.CurrentState).EndTick = timeRemaining * 2;

				player.SendMessage($"§eReset in-game timer, and updated end-time to {timeRemaining} seconds");
			}
			else if (args[0].Equals("level"))
			{
				if (args.Length < 2)
				{
					player.SendMessage("§c/gameedit level <levelname>");
					return;
				}

				string fullyQualifiedName = $"C:\\Users\\Administrator\\Desktop\\worlds\\{level.GameType}\\{args[1]}";
				GameLevel gameLevel;
				if (!gameController.LevelNames.Contains(fullyQualifiedName) || (gameLevel = gameController.InitializeNewGame(fullyQualifiedName)) == null)
				{
					player.SendMessage($"§cInvalid level name ({args[1]})");
					player.SendMessage($"§cBad Args: \n§c- {string.Join("\n§c- ", gameController.LevelNames.Select(x => _removeQualification(x.ToString())).ToArray())}");
					return;
				}

				foreach (SkyPlayer gamePlayer in level.GetAllPlayers())
				{
					gameLevel.AddPlayer(gamePlayer);
				}

				level.UpdateGameState(new VoidGameState()); //'Close' the game eventually

				player.SendMessage($"§cUpdating game level to {args[1]}");
			}
			else if (args[0].Equals("nextstate"))
			{
				GameState nextState = level.CurrentState.GetNextGameState(level);
				if (nextState is VoidGameState)
				{
					player.SendMessage("§cNo Next Available State Available.");
					return;
				}

				player.SendMessage($"§cProgressing to next state ({level.CurrentState.GetType()} -> {nextState.GetType()})");
				level.UpdateGameState(nextState);
			}
			else
			{
				if (!gameController.HandleGameEditCommand(player as SkyPlayer, level, gameLevelInfo, args))
				{
					player.SendMessage("§c/gameedit timeleft");
					player.SendMessage("§c/gameedit tp");
					player.SendMessage("§c/gameedit nextstate");
					player.SendMessage("§c/gameedit level <level-name>");
					{
						string subCommandHelp = gameController.GetGameEditCommandHelp(player as SkyPlayer);
						if (subCommandHelp != null)
						{
							player.SendMessage(subCommandHelp);
						}
					}
					player.SendMessage($"§cBad Args: {string.Join(",", args.Select(x => x.ToString()).ToArray())}");
				}
			}
		}

		protected string _removeQualification(string fullyQualifiedName)
		{
			string levelName;
			{
				string[] split = fullyQualifiedName.Split('\\');
				levelName = split[split.Length - 1];
			}
			return levelName;
		}

	}
}
