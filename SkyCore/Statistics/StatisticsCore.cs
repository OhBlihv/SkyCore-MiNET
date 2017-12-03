using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
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

		//TODO: Temp - First Join
		private static readonly ISet<string> PendingFirstJoinXuids = new HashSet<string>();

		private static readonly Thread StatisticUpdateThread;

		static StatisticsCore()
		{
			RunnableTask.RunTask(() =>
			{
				new DatabaseAction().Query(
					"CREATE TABLE IF NOT EXISTS `player_info` (\n" +
						"`player_xuid`       varchar(32),\n" +
						"`current_name`      varchar(16),\n" +
						"PRIMARY KEY(`player_xuid`)\n" +
					");",
					null, null, null);

				new DatabaseAction().Query(
					"CREATE TABLE IF NOT EXISTS `player_global_stats` (\n" +
						"`player_xuid`       varchar(32),\n" +
						"`first_join`        DATETIME(1) NOT NULL,\n" +
						"`experience`        INT(4) DEFAULT 0,\n" +
						"`coins`             INT(4) DEFAULT 0,\n" +
						"PRIMARY KEY(`player_xuid`)\n" +
					");",
					null, null, null);
			});

			StatisticUpdateThread = new Thread(() =>
			{
				Thread.CurrentThread.IsBackground = true;

				while (!SkyCoreAPI.IsDisabled)
				{
					Thread.Sleep(15000); //15 Second Delay

					RunUpdateTask();
				}
			});
			StatisticUpdateThread.Start();
		}

		private static void RunUpdateTask()
		{
			if (PendingNameUpdates.Count > 0)
			{
				new DatabaseBatch<KeyValuePair<string, string>>(
					"INSERT INTO `player_info`\n" +
					"  (`player_xuid`, `current_name`)\n" +
					"VALUES\n" +
					"  (@player_xuid, @current_name)\n" +
					"ON DUPLICATE KEY UPDATE\n" +
					"  `player_xuid`	= VALUES(`player_xuid`),\n" +
					"  `current_name`	= VALUES(`current_name`);",
					"player_info",
					(parameters) =>
					{
						parameters.Add("@player_xuid", MySqlDbType.VarChar, 32, "player_xuid");
						parameters.Add("@current_name", MySqlDbType.VarChar, 16, "current_name");
					},
					(dataRow, batchItem) =>
					{
						dataRow["player_xuid"] = batchItem.Key;
						dataRow["current_name"] = batchItem.Value;
						return true;
					},
					null,
					new HashSet<KeyValuePair<string, string>>(PendingNameUpdates)
				).ExecuteBatch();

				PendingNameUpdates.Clear(); //Reset
			}

			if (PendingFirstJoinXuids.Count > 0)
			{
				new DatabaseBatch<string>(
					"INSERT IGNORE INTO `player_global_stats`\n" +
					"  (`player_xuid`, `first_join`)\n" +
					"VALUES\n" +
					"  (@player_xuid, @first_join);",
					"player_info",
					(parameters) =>
					{
						parameters.Add("@player_xuid", MySqlDbType.VarChar, 32, "player_xuid");
						parameters.Add("@first_join", MySqlDbType.DateTime, 32, "first_join");
					},
					(dataRow, batchItem) =>
					{
						dataRow["player_xuid"] = batchItem;
						dataRow["first_join"] = DateTime.Now;
						return true;
					},
					null,
					new HashSet<string>(PendingFirstJoinXuids)
				).ExecuteBatch();

				PendingFirstJoinXuids.Clear(); //Reset
			}
		}

		public static void Close()
		{
			RunUpdateTask(); //Force run the update task on the plugin thread before quitting
			StatisticUpdateThread.Abort();
		}

		public static void AddPlayer(string xuid, string currentName)
		{
			AddPlayer(xuid, currentName, true);
		}

		public static void AddPlayer(string xuid, string currentName, bool updateDb)
		{
			if (XuidToName.ContainsKey(xuid))
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
				
				//Attempt to store first join
				PendingFirstJoinXuids.Add(xuid);
			}

			if (updateDb)
			{
				//Only updates when the xuid->name pair changes
				PendingNameUpdates.Add(new KeyValuePair<string, string>(xuid, currentName));
			}
		}

		/// <summary>
		/// Retrieves the player name for a given xuid,
		/// assuming the xuid->playername is cached.
		/// Otherwise this will call the db sync to retrieve the name
		/// if not cached.
		/// </summary>
		/// <param name="xuid"></param>
		/// <returns>string containing the playername associated with that xuid, otherwise null</returns>
		public static string GetPlayerNameFromXuid(string xuid)
		{
			if (XuidToName.ContainsKey(xuid))
			{
				return XuidToName[xuid];
			}

			string currentName = null;
			new DatabaseAction().Query(
				"SELECT `current_name` FROM `player_info` WHERE `player_xuid`=@player_xuid;",
				(command) =>
				{
					command.Parameters.AddWithValue("@player_xuid", xuid);
				},
				(reader) =>
				{
					currentName = reader.GetString(0);
				},
				new Action(delegate
				{
					SkyUtil.log($"Finished query to find names for {xuid}->{currentName}");	
				})
			);

			if (currentName != null)
			{
				AddPlayer(xuid, currentName, false);
			}
			else
			{
				SkyUtil.log($"No name found associated with {xuid}");
			}

			return currentName;
		}

		/// <summary>
		/// Retrieves the xuid for a given player,
		/// assuming the playername->xuid is cached.
		/// Otherwise this will call the db sync to retrieve the xuid
		/// if not cached.
		/// </summary>
		/// <param name="currentName"></param>
		/// <returns>string containing the playername associated with that xuid, otherwise null</returns>
		public static string GetXuidForPlayername(string currentName)
		{
			if (NameToXuid.ContainsKey(currentName))
			{
				return NameToXuid[currentName];
			}

			string playerXuid = null;
			new DatabaseAction().Query(
				"SELECT `player_xuid` FROM `player_info` WHERE `current_name`=@current_name;",
				(command) =>
				{
					command.Parameters.AddWithValue("@current_name", currentName);
				},
				(reader) =>
				{
					playerXuid = reader.GetString(0);
				},
				new Action(delegate
				{
					SkyUtil.log($"Finished query to find xuid for {currentName}->{playerXuid}");
				})
			);

			if (playerXuid != null)
			{
				AddPlayer(playerXuid, currentName, false);
			}
			else
			{
				SkyUtil.log($"No xuid found associated with {playerXuid}");
			}

			return null;
		}

	}
}
