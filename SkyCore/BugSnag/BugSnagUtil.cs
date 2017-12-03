using System;using Bugsnag;using Bugsnag.Clients;namespace SkyCore.BugSnag
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

		public static void ReportBug(Exception e, params IBugSnagMetadatable[] metadatables)
		{
			try
			{
				Console.WriteLine(e);

				Metadata metadata = new Metadata();

				int i = 0;
				foreach (IBugSnagMetadatable metadatable in metadatables)
				{
					i++;
					try
					{
						if (metadatable == null)
						{
							throw new ArgumentNullException();
						}

						metadatable.PopulateMetadata(metadata);
					}
					catch (Exception exception)
					{
						metadata.AddToTab($"Metadatable #{i}", "Exception", exception);
					}
				}

				lock (BugSnagLock)
				{
					_bugSnagClient.Notify(e, metadata);
				}

				//SkyUtil.log("Reported 1 Bug to BugSnag");
			}
			catch (Exception exception)
			{
				Console.WriteLine(exception);
			}
		}

	}
}
