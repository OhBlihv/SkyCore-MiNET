using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiNET.Utils;
using SkyCore.Game;
using SkyCore.Game.State;
using SkyCore.Games.BuildBattle.State;
using SkyCore.Player;

namespace SkyCore.Games.BuildBattle
{
	public class BuildBattleLevel : GameLevel
	{

		public readonly List<BuildBattleTeam> BuildTeams = new List<BuildBattleTeam>();

		public BuildBattleLevel(SkyCoreAPI plugin, string gameId, string levelPath) : base(plugin, "buildbattle", gameId, levelPath, new PlayerLocation(266, 11, 256))
		{

		}

		protected override void InitializeTeamMap()
		{
			for (int i = 0; i < GetMaxPlayers(); i++)
			{
				string teamName;
				PlayerLocation spawnLocation;
				switch (i)
				{
					case 0: teamName = "§aGreen";      spawnLocation = new PlayerLocation(213.5, 66, 297.5); break;
					case 1: teamName = "§bAqua";       spawnLocation = new PlayerLocation(213.5, 66, 255.5); break;
					case 2: teamName = "§cRed";        spawnLocation = new PlayerLocation(213.5, 66, 213.5); break;
					case 3: teamName = "§dPink";       spawnLocation = new PlayerLocation(255.5, 66, 213.5); break;
					case 4: teamName = "§eYellow";     spawnLocation = new PlayerLocation(255.5, 66, 255.5); break;
					case 5: teamName = "§fWhite";      spawnLocation = new PlayerLocation(255.5, 66, 297.5); break;
					case 6: teamName = "§5Purple";     spawnLocation = new PlayerLocation(297.5, 66, 297.5); break;
					case 7: teamName = "§6Gold";       spawnLocation = new PlayerLocation(297.5, 66, 255.5); break;
					case 8: teamName = "§7Grey";       spawnLocation = new PlayerLocation(297.5, 66, 213.5); break;
					case 9: teamName = "§9Indigo";     spawnLocation = new PlayerLocation(213.5, 66, 297.5); break;
					default: teamName = "§8Dark Grey"; spawnLocation = new PlayerLocation(213.5, 66, 297.5); break;
				}

				BuildBattleTeam colourTeam = new BuildBattleTeam(i, teamName, spawnLocation);
				BuildTeams.Add(colourTeam);
				TeamPlayerDict.Add(colourTeam, new List<SkyPlayer>());
			}
		}

		public override GameState GetInitialState()
		{
			return new BuildBattleLobbyState();
		}

		//Places the player in the first free build team
		public override GameTeam GetDefaultTeam()
		{
			GameTeam lastTeam = null;
			foreach (GameTeam buildTeam in BuildTeams)
			{
				List<SkyPlayer> teamPlayers = TeamPlayerDict[buildTeam];
				if (teamPlayers.Count == 0)
				{
					return buildTeam;
				}

				lastTeam = buildTeam;
			}

			return lastTeam;
		}

		public override int GetMaxPlayers()
		{
			return 9;
		}

		public override void GameTick(int tick)
		{
			
		}

	}
}
