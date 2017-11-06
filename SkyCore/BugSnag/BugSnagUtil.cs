using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bugsnag;
using Bugsnag.Clients;

namespace SkyCore.BugSnag
{
	public class BugSnagUtil
	{

		private static BaseClient _bugSnagClient;

		private static readonly object BugSnagLock = new object();

		public static void Init()
		{
			_bugSnagClient = new BaseClient("189d7e1ed0d5c11045f91967f0f3f32b");
			
			/*lock (BugSnagLock)
			{
				_bugSnagClient.Notify(new ArgumentException("Non-fatal"));
				ReportBug(null, new ArgumentException("Non-Fatal-Util"));
			}*/
		}

		public static void ReportBug(IBugSnagMetadatable metadatable, Exception e)
		{
			//Console.WriteLine(e);

			Metadata metadata = new Metadata();

			//metadatable?.PopulateMetadata(metadata);

			lock (BugSnagLock)
			{
				_bugSnagClient.Notify(e, metadata);
			}
			
			//SkyUtil.log("Reported 1 Bug to BugSnag");
		}

	}
}
