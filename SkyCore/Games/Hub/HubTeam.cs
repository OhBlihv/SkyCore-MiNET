using SkyCore.Game.State;

namespace SkyCore.Games.Hub
{
	public class HubTeam : GameTeam
	{

		public static readonly HubTeam Player = new HubTeam(0, "Hub Player", "§a");
		public static readonly HubTeam Spectator = new HubTeam(1, "Hub Spectator", "§7", true);

		public string TeamName { get; }
		public string TeamPrefix { get; }

		private HubTeam(int value, string teamName, string teamPrefix, bool isSpectator = false) : base(value, teamName, isSpectator)
		{
			TeamName = teamName;
			TeamPrefix = teamPrefix;
		}

	}
}
