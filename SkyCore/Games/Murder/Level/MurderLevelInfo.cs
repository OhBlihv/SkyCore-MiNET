using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MiNET.Utils;
using SkyCore.Game.Level;

namespace SkyCore.Games.Murder.Level
{

	[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
	public class MurderLevelInfo : GameLevelInfo
	{

		public List<PlayerLocation> PlayerSpawnLocations { get; set; }
		public List<PlayerLocation> GunPartLocations { get; set; }

		//JSON Loading
		public MurderLevelInfo()
		{
			
		}

		public MurderLevelInfo(string levelName, int worldTime, PlayerLocation lobbyLocation, 
								List<PlayerLocation> playerSpawnLocations, List<PlayerLocation> gunPartLocations) : base("murder", levelName, worldTime, lobbyLocation)
		{
			PlayerSpawnLocations = playerSpawnLocations;
			GunPartLocations = gunPartLocations;
		}

		public override object Clone()
		{
			//Shallow Clone
			return new MurderLevelInfo(LevelName, WorldTime, (PlayerLocation)LobbyLocation.Clone(),
				PlayerSpawnLocations.ToList(), GunPartLocations.ToList());
		}

	}
}
