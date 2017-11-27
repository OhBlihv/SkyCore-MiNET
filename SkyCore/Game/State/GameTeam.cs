using Bugsnag;
using SkyCore.BugSnag;
using SkyCore.Util;

namespace SkyCore.Game.State
{
    public class GameTeam : Enumeration, IBugSnagMetadatable
    {

        public bool IsSpectator { get; }

        public GameTeam(int value, string name, bool isSpectator = false) : base(value, name)
        {
            IsSpectator = isSpectator;
        }

	    public void PopulateMetadata(Metadata metadata)
	    {
			metadata.AddToTab("GameTeam", "Username", DisplayName);
			metadata.AddToTab("GameTeam", "Value", Value);
			metadata.AddToTab("GameTeam", "IsSpectator", IsSpectator);
		}
    }
}
