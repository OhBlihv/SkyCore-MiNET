using Bugsnag;

namespace SkyCore.BugSnag
{
	public interface IBugSnagMetadatable
	{

		void PopulateMetadata(Metadata metadata);

	}
}
