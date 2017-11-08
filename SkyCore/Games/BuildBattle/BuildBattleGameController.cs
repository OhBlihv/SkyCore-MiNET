using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MiNET.Blocks;
using MiNET.Items;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkyCore.Game;
using SkyCore.Game.Level;
using SkyCore.Player;

namespace SkyCore.Games.BuildBattle
{

	public class BuildBattleTheme
	{
	
		public String ThemeName { get; set; }
	
		public List<CachedItem> TemplateItems { get; }

		public BuildBattleTheme(string themeName, List<CachedItem> templateItems)
		{
			ThemeName = themeName;
			TemplateItems = templateItems;
		}
	
	}

	public class CachedItem
	{
		
		public short Id { get; }
		
		public short Damage { get; }

		[JsonIgnore]
		public Item PreLoadedItem { get; private set; }

		public CachedItem(short id, short damage)
		{
			Id = id;
			Damage = damage;
		}

		public Item GetItem()
		{
			if (PreLoadedItem != null)
			{
				return PreLoadedItem;
			}

			PreLoadedItem = ItemFactory.GetItem(Id, Damage) ?? new ItemBlock(new Block((byte) Id), Damage);

			return PreLoadedItem;
		}
		
	}
	
	public class BuildBattleGameController : GameController
	{

		private readonly List<BuildBattleTheme> _themeList;
		
		public BuildBattleGameController(SkyCoreAPI plugin) : base(plugin, "build-battle", "Build Battle", 
			new List<string>{"build-battle-template"})
		{
			string themeFilePath =
				$"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\\config\\build-battle-themes.json";
			
			//Generate Example Config
			if (!File.Exists(themeFilePath))
			{
				List<BuildBattleTheme> tempThemeList = new List<BuildBattleTheme>
				{
					new BuildBattleTheme("Theme #1",
						new List<CachedItem>{new CachedItem(1, 1), new CachedItem(5, 0)}),
					new BuildBattleTheme("Theme #2",
						new List<CachedItem>{new CachedItem(1, 1), new CachedItem(5, 0)})
				};

				File.WriteAllText(themeFilePath, JsonConvert.SerializeObject(tempThemeList, Formatting.Indented));
			}

			object jObject = JsonConvert.DeserializeObject(File.ReadAllText(themeFilePath));

			if (jObject is JArray array)
			{
				try
				{
					_themeList = array.ToObject<List<BuildBattleTheme>>();

					foreach (BuildBattleTheme theme in _themeList)
					{
						//Automatic Bolding
						if (theme.ThemeName.StartsWith("§"))
						{
							theme.ThemeName = theme.ThemeName.Substring(0, 2) + "§l" +
							                  theme.ThemeName.Substring(2, theme.ThemeName.Length - 2);
						}
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
			}
			else
			{
				SkyUtil.log($"Unable to load theme list. Parsed Object was of type {(jObject == null ? "null" : $"{jObject.GetType()}")}");
			}

			SkyUtil.log($"Initialized {_themeList.Count} Themes");
		}

		protected override GameLevel _initializeNewGame()
		{
			string selelectedLevel = GetRandomLevelName();

			return new BuildBattleLevel(Plugin, GetNextGameId(), selelectedLevel, GetGameLevelInfo(selelectedLevel), _themeList);
		}

		protected override GameLevel _initializeNewGame(string levelName)
		{
			return new BuildBattleLevel(Plugin, GetNextGameId(), levelName, GetGameLevelInfo(levelName), _themeList);
		}

		public override Type GetGameLevelInfoType()
		{
			return typeof(GameLevelInfo); //Nothing Custom
		}

		/*[Command(Name = "gameedit")]
		[Authorize(Permission = CommandPermission.Normal)]
		public void CommandGameEdit(MiNET.Player player, params string[] args)
		{
			if (player.CommandPermission < CommandPermission.Admin)
			{
				player.SendMessage("§c§l(!)§r §cYou do not have permission for this command.");
				return;
			}

			if (!(player.Level is GameLevel level))
			{
				player.SendMessage($"§cYou must be in a {GameName} game to use this command!");
				return;
			}

			if (!(level.GameLevelInfo is GameLevelInfo gameLevelInfo))
			{
				player.SendMessage("§cThe current level's information could not be loaded.");
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

				string fullyQualifiedName = $"C:\\Users\\Administrator\\Desktop\\worlds\\{RawName}\\{args[1]}";
				GameLevel gameLevel;
				if (!LevelNames.Contains(fullyQualifiedName) || (gameLevel = InitializeNewGame(fullyQualifiedName)) == null)
				{
					player.SendMessage($"§cInvalid level name ({args[1]})");
					player.SendMessage($"§cBad Args: \n§c- {string.Join("\n§c- ", LevelNames.Select(x => _removeQualification(x.ToString())).ToArray())}");
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
				if (!HandleGameEditCommand(player as SkyPlayer, level, gameLevelInfo, args))
				{
					player.SendMessage("§c/gameedit timeleft");
					player.SendMessage("§c/gameedit tp");
					player.SendMessage("§c/gameedit nextstate");
					player.SendMessage("§c/gameedit level <level-name>");
					{
						string subCommandHelp = GetGameEditCommandHelp(player as SkyPlayer);
						if (subCommandHelp != null)
						{
							player.SendMessage(subCommandHelp);
						}
					}
					player.SendMessage($"§cBad Args: {string.Join(",", args.Select(x => x.ToString()).ToArray())}");
				}
			}
		}*/

		public override bool HandleGameEditCommand(SkyPlayer player, GameLevel gameLevel, GameLevelInfo gameLevelInfo, params string[] args)
		{
			if (args[0].Equals("tp"))
			{
				if (!(gameLevel is BuildBattleLevel level))
				{
					player.SendMessage("§eWorld is not BuildBattle world!");
					return false;
				}

				player.SendMessage("§eTeleporting to a random spawn location");
				player.Teleport(level.BuildTeams[Random.Next(level.BuildTeams.Count)].SpawnLocation);
			}
			else
			{
				return false;
			}

			return true;
		}

		public override string GetGameEditCommandHelp(SkyPlayer player)
		{
			return null;
		}
	}
}
