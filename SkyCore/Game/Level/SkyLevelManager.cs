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
