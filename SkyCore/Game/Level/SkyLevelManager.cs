using System;
using System.Linq;
using MiNET;
using MiNET.Worlds;
using SkyCore.Player;

namespace SkyCore.Game.Level
{
	public class SkyLevelManager : LevelManager
	{

		private SkyCoreAPI SkyCoreApi { get; }

		public SkyLevelManager(SkyCoreAPI skyCoreApi)
		{
			SkyCoreApi = skyCoreApi;
		}

		//DOESN'T WORK BUT WOULD BE BENEFICIAL
		/*public override MiNET.Worlds.Level GetLevel(MiNET.Player player, string name)
		{
			MiNET.Worlds.Level level;

			//'Get Default World' - Find a game for the player
			if (name.Equals(Dimension.Overworld.ToString()))
			{
				//SkyUtil.log("Instantly queuing player using default GetLevel");

				level = SkyCoreApi.GameModes[SkyCoreApi.GameType].InstantQueuePlayer(player as SkyPlayer, false);
				if (level != null)
				{
					return level;
				}

				BugSnagUtil.ReportBug(new Exception($"No initial entry world found for {player.Username}"), player as SkyPlayer, player.Level as GameLevel);
			}

			//Select the first level in the collection
			level = Levels.FirstOrDefault(l => l.LevelId.Equals(name, StringComparison.InvariantCultureIgnoreCase));
			if (level == null && name.Equals(Dimension.Overworld.ToString()))
			{
				level = Levels[0];
			}

			return level;
		}*/

		public override MiNET.Worlds.Level GetLevel(MiNET.Player player, string name)
		{
			//'Get Default World' - Find a game for the player
			if (name.Equals(Dimension.Overworld.ToString()))
			{
				//SkyUtil.log("Instantly queuing player using default GetLevel");

				return SkyCoreApi.GameModes[SkyCoreApi.GameType].InstantQueuePlayer(player as SkyPlayer, false);
			}
			else
			{
				return Levels.FirstOrDefault(l => l.LevelId.Equals(name, StringComparison.InvariantCultureIgnoreCase));
			}
		}

	}
}
