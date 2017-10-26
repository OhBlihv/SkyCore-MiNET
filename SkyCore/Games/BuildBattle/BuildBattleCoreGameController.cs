using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SkyCore.Game;
using SkyCore.Game.Level;

namespace SkyCore.Games.BuildBattle
{
	public class BuildBattleCoreGameController : CoreGameController
	{

		private readonly List<string> _themeList = new List<string>();
		
		public BuildBattleCoreGameController(SkyCoreAPI plugin) : base(plugin, "build-battle", "Build Battle", 
			new List<string>{"build-battle-template"})
		{
			foreach (string themeName in File.ReadAllLines(
				$"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\\config\\build-battle-themes.cfg"))
			{
				_themeList.Add(themeName);
			}
			
			SkyUtil.log($"Initialized {_themeList.Count} Themes");
		}

		protected override GameLevel _initializeNewGame()
		{
			string selelectedLevel = GetRandomLevelName();

			return new BuildBattleLevel(Plugin, GetNextGameId(), selelectedLevel, GetGameLevelInfo(selelectedLevel), _themeList);
		}

		public override Type GetGameLevelInfoType()
		{
			return typeof(GameLevelInfo); //Nothing Custom
		}

	}
}
