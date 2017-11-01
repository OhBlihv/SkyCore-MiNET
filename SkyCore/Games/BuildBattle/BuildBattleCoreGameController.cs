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

namespace SkyCore.Games.BuildBattle
{

	public class BuildBattleTheme
	{
	
		public String ThemeName { get; }
	
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
	
	public class BuildBattleCoreGameController : CoreGameController
	{

		private readonly List<BuildBattleTheme> _themeList;
		
		public BuildBattleCoreGameController(SkyCoreAPI plugin) : base(plugin, "build-battle", "Build Battle", 
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

	}
}
