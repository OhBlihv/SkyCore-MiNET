using MiNET.Utils;
using SkyCore.Game.State;

namespace SkyCore.Games.BuildBattle
{
	public class BuildBattleTeam : GameTeam
	{
		
		public PlayerLocation SpawnLocation { get; }
		
		public BuildBattleTeam(int value, string name, PlayerLocation spawnLocation, bool isSpectator = false) : base(value, name, isSpectator)
		{
			SpawnLocation = spawnLocation;
		}
	}
}
