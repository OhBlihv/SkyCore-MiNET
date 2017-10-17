using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiNET.Utils;
using MiNET.Worlds;
using SkyCore.Game;
using SkyCore.Game.Level;
using SkyCore.Game.State;
using SkyCore.Games.BuildBattle.State;
using SkyCore.Player;

namespace SkyCore.Games.BuildBattle
{
	public class BuildBattleLevel : GameLevel
	{

		public readonly List<BuildBattleTeam> BuildTeams = new List<BuildBattleTeam>();

		public BuildBattleLevel(SkyCoreAPI plugin, string gameId, string levelPath) : base(plugin, "build-battle", gameId, levelPath, true)
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
			foreach (BuildBattleTeam buildTeam in BuildTeams)
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

		public List<PlayerLocation> GetVoteLocations(BuildBattleTeam buildTeam)
		{
			List<PlayerLocation> voteLocations = new List<PlayerLocation>();
			for (int i = 0; i < BuildTeams.Count;i++)
			{
				PlayerLocation voteLocation = (PlayerLocation)buildTeam.SpawnLocation.Clone();
				voteLocation.Y += 10;
				voteLocation.Pitch = -15F; //Pitch down to look at the 'build'
				switch (i)
				{
					case 0:
						voteLocation.X -= 5;
						voteLocation.HeadYaw = 270F;
						break;
					case 1:
						voteLocation.X -= 2.5f;
						voteLocation.Z -= 2.5f;
						voteLocation.HeadYaw = 315F;
						break;
					case 2:
						voteLocation.Z -= 5;
						voteLocation.HeadYaw = 0F;
						break;
					case 3:
						voteLocation.X += 2.5f;
						voteLocation.Z -= 2.5f;
						voteLocation.HeadYaw = 45F;
						break;
					case 4:
						voteLocation.X += 5;
						voteLocation.HeadYaw = 90F;
						break;
					case 5:
						voteLocation.X += 2.5f;
						voteLocation.Z += 2.5f;
						voteLocation.HeadYaw = 135F;
						break;
					case 6:
						voteLocation.Z += 5;
						voteLocation.HeadYaw = 180F;
						break;
					case 7:
						voteLocation.X -= 2.5f;
						voteLocation.Z += 2.5f;
						voteLocation.HeadYaw = 225F;
						break;
				}

				voteLocations.Add(voteLocation);
			}

			return voteLocations;
		}

		public override Type GetGameLevelInfoType()
		{
			return typeof(GameLevelInfo); //Nothing Custom
		}

		public override string GetGameModalTitle()
		{
			return "§5§lBUILD BATTLE";
		}

		public override string GetEndOfGameContent(SkyPlayer player)
		{
			return "TODO";
		}
	}
}
