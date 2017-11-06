using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bugsnag;

namespace SkyCore.BugSnag
{
	public interface IBugSnagMetadatable
	{

		void PopulateMetadata(Metadata metadata);

	}
}
