using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
