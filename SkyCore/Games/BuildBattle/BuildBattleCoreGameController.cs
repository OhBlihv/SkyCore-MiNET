using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkyCore.Game;

namespace SkyCore.Games.BuildBattle
{
	public class BuildBattleCoreGameController : CoreGameController
	{
		public BuildBattleCoreGameController(SkyCoreAPI plugin) : base(plugin, "build-battle", "Build Battle", 
			new List<string>{"build-battle-template"})
		{

		}

		protected override GameLevel _getGameController()
		{
			return new BuildBattleLevel(Plugin, GetNextGameId(), GetRandomLevelName());
		}
	}
}
