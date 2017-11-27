using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bugsnag;
using SkyCore.Game.Level;

namespace SkyCore.BugSnag
{

	public class AnonMetadatable : IBugSnagMetadatable
	{

		private readonly PopulateMetadata _populator;

		public AnonMetadatable(PopulateMetadata populator)
		{
			_populator = populator;
		}

		public void PopulateMetadata(Metadata metadata)
		{
			_populator.Invoke(metadata);
		}
	}

	public delegate void PopulateMetadata(Metadata metadata);

}
