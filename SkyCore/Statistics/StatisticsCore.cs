using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkyCore.Database;
using SkyCore.Util;

namespace SkyCore.Statistics
{
	public class StatisticsCore
	{

		//TODO: Past Names
		//TODO: Read Cached names from SQL
		private static readonly ConcurrentDictionary<string, string> XuidToName = new ConcurrentDictionary<string, string>();
		private static readonly ConcurrentDictionary<string, string> NameToXuid = new ConcurrentDictionary<string, string>();

		static StatisticsCore()
		{
			RunnableTask.RunTask(() =>
			{
				new DatabaseAction().Query(
					"CREATE TABLE IF NOT EXISTS `player_info` (\n" +
						"`player_xuid`       varchar(32),\n" +
						"`current_name`      varchar(16),\n" +
						" PRIMARY KEY(`player_xuid`)\n" +
					");",
					null, null, null);
			});
		}

		public static void AddPlayer(string xuid, string currentName)
		{
			if(XuidToName.ContainsKey(xuid))
			{
				NameToXuid.TryRemove(XuidToName[xuid], out _);

				XuidToName[xuid] = currentName;
				NameToXuid.TryAdd(currentName, xuid);
			}
			else
			{
				XuidToName.TryAdd(xuid, currentName);
				NameToXuid.TryAdd(currentName, xuid);
			}
		}

		public static string GetPlayerNameFromXuid(string xuid)
		{
			if (XuidToName.ContainsKey(xuid))
			{
				return XuidToName[xuid];
			}

			return null;
		}

		public static string GetXuidForPlayername(string currentName)
		{
			if (NameToXuid.ContainsKey(currentName))
			{
				return NameToXuid[currentName];
			}

			return null;
		}

	}
}
