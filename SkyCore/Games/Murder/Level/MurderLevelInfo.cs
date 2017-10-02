using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiNET.Utils;
using SkyCore.Game.Level;

namespace SkyCore.Games.Murder.Level
{
	public class MurderLevelInfo : GameLevelInfo
	{

		public List<PlayerLocation> PlayerSpawnLocations { get; set; }
		public List<PlayerLocation> GunPartLocations { get; set; }

		public MurderLevelInfo(string levelName, PlayerLocation lobbyLocation,
								List<PlayerLocation> playerSpawnLocations, List<PlayerLocation> gunPartLocations
								) : base("murder", levelName, lobbyLocation)
		{
			PlayerSpawnLocations = playerSpawnLocations;
			GunPartLocations = gunPartLocations;
		}

	}
}
