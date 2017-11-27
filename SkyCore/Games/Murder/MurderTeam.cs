using SkyCore.Game.State;

namespace SkyCore.Games.Murder
{
    
    public class MurderTeam : GameTeam
    {
        
        public static readonly MurderTeam Innocent = new MurderTeam(0, "Innocent", "§a");
        public static readonly MurderTeam Detective = new MurderTeam(1, "Detective", "§9");
        public static readonly MurderTeam Murderer = new MurderTeam(2, "Murderer", "§c");
        public static readonly MurderTeam Spectator = new MurderTeam(3, "Spectator", "§7", true);

        public string TeamName { get; }
        public string TeamPrefix { get; }

        private MurderTeam(int value, string teamName, string teamPrefix, bool isSpectator = false) : base(value, teamName, isSpectator)
        {
            TeamName = teamName;
            TeamPrefix = teamPrefix;
        }
        
    }
    
}