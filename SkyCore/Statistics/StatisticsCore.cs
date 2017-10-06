using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
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

		private static readonly ISet<KeyValuePair<string, string>> PendingNameUpdates = new HashSet<KeyValuePair<string, string>>();

		private static readonly Thread StatisticUpdateThread;

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

			StatisticUpdateThread = new Thread(() =>
			{
				Thread.CurrentThread.IsBackground = true;

				while (!SkyCoreAPI.IsDisabled)
				{
					Thread.Sleep(15000); //15 Second Delay

					if (PendingNameUpdates.Count > 0)
					{
						new DatabaseBatch<KeyValuePair<string, string>>(
						"INSERT INTO `player_info`\n" +
							"  (`player_xuid`, `current_name`)\n" +
						"VALUES\n" +
							"  (@player_xuid, @current_name)\n" +
						"ON DUPLICATE KEY UPDATE\n" +
							"  `player_xuid`		= VALUES(`player_xuid`),\n" +
							"  `current_name`		= VALUES(`current_name`);",
						"player_info",
						(parameters) =>
						{
							parameters.Add("@player_xuid", MySqlDbType.VarChar, 32, "player_xuid");
							parameters.Add("@current_name", MySqlDbType.VarChar, 16, "current_name");
						},
						(dataRow, batchItem) =>
						{
							dataRow["player_xuid"] = batchItem.Key;
							dataRow["punish_type"] = batchItem.Value;
							return true;
						},
						null,
						new HashSet<KeyValuePair<string, string>>(PendingNameUpdates)
						).ExecuteBatch();

						PendingNameUpdates.Clear(); //Reset
					}
				}
			});
			StatisticUpdateThread.Start();
		}

		public static void Close()
		{
			StatisticUpdateThread.Abort();
		}

		public static void AddPlayer(string xuid, string currentName)
		{
			if(XuidToName.ContainsKey(xuid))
			{
				if (XuidToName[xuid].Equals(currentName))
				{
					return; //No update.
				}

				NameToXuid.TryRemove(XuidToName[xuid], out _);

				XuidToName[xuid] = currentName;
				NameToXuid.TryAdd(currentName, xuid);
			}
			else
			{
				XuidToName.TryAdd(xuid, currentName);
				NameToXuid.TryAdd(currentName, xuid);
			}

			//Only updates when the xuid->name pair changes
			PendingNameUpdates.Add(new KeyValuePair<string, string>(xuid, currentName));
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
