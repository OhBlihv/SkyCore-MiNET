using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiNET.Utils;
using MySql.Data.MySqlClient;
using SkyCore.Database;
using SkyCore.Statistics;
using SkyCore.Util;

namespace SkyCore.Punishments
{

	public enum PunishmentType
	{
		
		Ban,
		Kick,
		Mute

	}

	public enum DurationUnit
	{
		
		Minutes,
		Hours,
		Days,
		Weeks,
		Months,
		Years,
		Permanent

	}

	public class Punishment : IComparable
	{

		public bool Dirty { get; set; }

		//

		public string PunishReason { get; }

		public string Issuer { get; }

		public bool Active { get; set; }

		public int DurationAmount { get; }

		public DurationUnit DurationUnit { get; }

		public DateTime Expiry { get; } //Expiry == Issue date for Perm bans

		public Punishment(string punishReason, string issuer, bool active, int durationAmount, DurationUnit durationUnit, DateTime expiry)
		{
			PunishReason = punishReason;
			Issuer = issuer;
			Active = active;
			DurationAmount = durationAmount;
			DurationUnit = durationUnit;
			Expiry = expiry;
		}

		public bool IsActive()
		{
			if (!Active)
			{
				return false;
			}

			if (DurationUnit == DurationUnit.Permanent)
			{
				return true;
			}

			bool isActive = Expiry.CompareTo(DateTime.Now) >= 0;
			if (!isActive)
			{
				Active = false;
			}

			return isActive;
		}

		public DateTime GetIssueDate()
		{
			switch (DurationUnit)
			{
				case DurationUnit.Permanent:
				{
					return Expiry;
				}
				case DurationUnit.Minutes:
				{
					return Expiry.AddMinutes(-DurationAmount);
				}
				case DurationUnit.Hours:
				{
					return Expiry.AddHours(-DurationAmount);
				}
				case DurationUnit.Days:
				{
					return Expiry.AddDays(-DurationAmount);
				}
				case DurationUnit.Weeks:
				{
					return Expiry.AddDays(-(DurationAmount * 7));
				}
				case DurationUnit.Months:
				{
					return Expiry.AddMonths(-DurationAmount);
				}
				case DurationUnit.Years:
				{
					return Expiry.AddYears(-DurationAmount);
				}
				default:
				{
					return Expiry;
				}
			}
		}

		public int CompareTo(object obj)
		{
			if (!(obj is Punishment pObj))
			{
				return 0;
			}

			if (Active && !pObj.Active)
			{
				return -1; //Order Before
			}
			if (!Active && pObj.Active)
			{
				return 1; //Order after
			}
			
			if (DurationUnit == DurationUnit.Permanent)
			{
				if (pObj.DurationUnit == DurationUnit.Permanent)
				{
					//Order by issue date
					return Expiry.CompareTo(pObj.Expiry);
				}
				return -1; //Order First;
			}

			return Expiry.CompareTo(pObj.Expiry);
		}

		public override string ToString()
		{
			return $"Reason: '{PunishReason}' Active: {IsActive()} -> Duration: {DurationAmount} {DurationUnit}. Expiry: {Expiry}";
		}
	}

	public class PlayerPunishments
	{

		//Holds active/most recent punishments at the start of the set
		public readonly Dictionary<PunishmentType, SortedSet<Punishment>> Punishments;

		public PlayerPunishments()
		{
			Punishments = new Dictionary<PunishmentType, SortedSet<Punishment>>();
		}

		public PlayerPunishments(Dictionary<PunishmentType, SortedSet<Punishment>> punishments)
		{
			Punishments = punishments;
		}

		public void AddPunishment(PunishmentType punishmentType, Punishment punishment)
		{
			//Deactivate existing punishment if exists
			RemoveActive(punishmentType);
			
			SortedSet<Punishment> punishments;
			if (Punishments.ContainsKey(punishmentType))
			{
				punishments = Punishments[punishmentType];
			}
			else
			{
				punishments = new SortedSet<Punishment>();
				Punishments.Add(punishmentType, punishments);
			}

			punishments.Add(punishment);
		}

		public bool RemoveActive(PunishmentType punishmentType)
		{
			Punishment activePunishment = GetActive(punishmentType);
			if (activePunishment != null)
			{
				if (activePunishment.Active)
				{
					activePunishment.Active = false;
					activePunishment.Dirty = true;
				}
				
				return true;
			}

			return false;
		}

		public Punishment GetActive(PunishmentType punishmentType)
		{
			if (Punishments.ContainsKey(punishmentType))
			{
				SkyUtil.log("Contains at least one ban");
				SortedSet<Punishment> punishments = Punishments[punishmentType];
				SkyUtil.log($"Punishments (in order):\n -{string.Join("\n -", (from o in punishments select o.ToString()).ToArray())}");
				if (punishments.Count == 0)
				{
					SkyUtil.log("Doesnt?");
					return null;
				}

				//Dodgy way to get 1st entry
				Punishment punishment = punishments.Min;
				if (punishment != null && punishment.IsActive())
				{
					SkyUtil.log("Returning active ban");
					return punishment;
				}

				if (punishment != null)
				{
					SkyUtil.log("Ban Found, not active? " + punishment);
				}
			}

			SkyUtil.log("Nothing found. Keys: " + Punishments.Keys.ToArray());

			return null;
		}

		public bool HasActive(PunishmentType punishmentType)
		{
			var punishment = GetActive(punishmentType);
			return punishment != null && punishment.IsActive();
		}

	}

	class PendingUpdatePunishment
	{
		

		public string PlayerXuid { get; }
		
		public PunishmentType PunishmentType { get; }

		public Punishment Punishment { get; }

		public PendingUpdatePunishment(string playerXuid, PunishmentType punishmentType, Punishment punishment)
		{
			PlayerXuid = playerXuid;
			PunishmentType = punishmentType;
			Punishment = punishment;
		}

	}

	public class PunishCore
	{

		//xuid -> Punishments
		private static readonly ConcurrentDictionary<string, PlayerPunishments> PlayerPunishmentCache = new ConcurrentDictionary<string, PlayerPunishments>();

		private static readonly Thread PunishmentUpdateThread;

		static PunishCore()
		{
			SkyCoreAPI.Instance.Context.PluginManager.LoadCommands(new PunishmentCommands());  //Initialize Punishment Commands

			RunnableTask.RunTask(() =>
			{
				//TODO: Remove
				/*new DatabaseAction().Query(
					"DROP TABLE `punishments`;",
					null, null, null);*/

				//TODO: Un-delay once db is finalized
				RunnableTask.RunTaskLater(() =>
				{
					new DatabaseAction().Query(
						"CREATE TABLE IF NOT EXISTS `punishments` (\n" +
							"`player_xuid`       varchar(50) NOT NULL,\n" +
							"`punish_type`       ENUM\n" +
							"					 ('Ban', 'Kick', 'Mute') DEFAULT 'Ban' NOT NULL,\n" +
							"`issuer`			 varchar(50) NOT NULL,\n" + //Issuer XUID
							"`reason`			 varchar(128),\n" +
							"`active`			 BOOLEAN,\n" +
							"`duration_amount`	 TINYINT(1) UNSIGNED DEFAULT 0,\n" + //0-255 possible units
							"`duration_unit`  	 ENUM\n" +
							"					 ('minutes', 'Hours', 'Days', 'Weeks', 'Months', 'Years', 'Permanent') DEFAULT 'Permanent' NOT NULL,\n" +
							"`issue_time`        DATETIME(1) NOT NULL,\n" +
							" PRIMARY KEY(`player_xuid`, `punish_type`, `issue_time`)\n" +
						");",
						null, null, null);
				}, 1000);
			});

			/*
			 * Loops through all players and ensures active punishments
			 * are actually active.
			 * 
			 * (Active is a boolean, separate from the Expiry DateTime)
			 */
			PunishmentUpdateThread = new Thread(() =>
			{
				Thread.CurrentThread.IsBackground = true;

				while (!SkyCoreAPI.IsDisabled)
				{
					Thread.Sleep(60000); //60 Second Delay

					RunUpdateTask();
				}
			});
			PunishmentUpdateThread.Start();
		}

		private static void RunUpdateTask()
		{
			List<PendingUpdatePunishment> pendingUpdates = new List<PendingUpdatePunishment>();

			DateTime currentTime = DateTime.Now;
			foreach (string playerXuid in PlayerPunishmentCache.Keys)
			{
				PlayerPunishments punishments = PlayerPunishmentCache[playerXuid];
				foreach (PunishmentType punishmentType in punishments.Punishments.Keys)
				{
					foreach (Punishment punishment in punishments.Punishments[punishmentType])
					{
						if (punishment.Dirty)
						{
							punishment.Dirty = false;
							pendingUpdates.Add(new PendingUpdatePunishment(playerXuid, punishmentType, punishment));
							SkyUtil.log($"Marking {StatisticsCore.GetPlayerNameFromXuid(playerXuid)}'s {punishmentType} as non-dirty (saved)");
							continue;
						}

						if (!punishment.IsActive())
						{
							continue;
						}

						//Ensure this punishment is still active
						if (punishment.DurationUnit != DurationUnit.Permanent &&
							currentTime.CompareTo(punishment.Expiry) >= 0) //TODO: Check if this is correct
						{
							SkyUtil.log($"Marking {StatisticsCore.GetPlayerNameFromXuid(playerXuid)}'s active {punishmentType} as inactive (expired)");
							punishment.Active = false;

							pendingUpdates.Add(new PendingUpdatePunishment(playerXuid, punishmentType, punishment));
						}
						else
						{
							if (punishment.DurationUnit == DurationUnit.Permanent)
							{
								SkyUtil.log($"{StatisticsCore.GetPlayerNameFromXuid(playerXuid)}'s active {punishmentType} is still active (PERMANENT)");
							}
							else
							{
								SkyUtil.log($"{StatisticsCore.GetPlayerNameFromXuid(playerXuid)}'s active {punishmentType} is still active ({punishment.Expiry.Subtract(currentTime).ToString()} Remaining)");
							}
						}
					}
				}
			}

			if (pendingUpdates.Count > 0)
			{
				new DatabaseBatch<PendingUpdatePunishment>(
				"INSERT INTO `punishments`\n" +
					"  (`player_xuid`, `punish_type`, `issuer`, `reason`, `active`, `duration_amount`, `duration_unit`, `issue_time`)\n" +
				"VALUES\n" +
					"  (@player_xuid, @punish_type, @issuer, @reason, @active, @duration_amount, @duration_unit, @issue_time)\n" +
				"ON DUPLICATE KEY UPDATE\n" +
					"  `player_xuid`		= VALUES(`player_xuid`),\n" +
					"  `punish_type`		= VALUES(`punish_type`),\n" +
					"  `issuer`				= VALUES(`issuer`),\n" +
					"  `reason`				= VALUES(`reason`),\n" +
					"  `active`				= VALUES(`active`),\n" +
					"  `duration_amount`    = VALUES(`duration_amount`),\n" +
					"  `duration_unit`      = VALUES(`duration_unit`),\n" +
					"  `issue_time`			= VALUES(`issue_time`);",
				"punishments",
				(parameters) =>
				{
					parameters.Add("@player_xuid", MySqlDbType.VarChar, 50, "player_xuid");
					parameters.Add("@punish_type", MySqlDbType.VarChar, 4, "punish_type");
					parameters.Add("@issuer", MySqlDbType.VarChar, 50, "issuer");
					parameters.Add("@reason", MySqlDbType.VarChar, 128, "reason");
					parameters.Add("@active", MySqlDbType.Int16, 1, "active");
					parameters.Add("@duration_amount", MySqlDbType.Int16, 2, "duration_amount");
					parameters.Add("@duration_unit", MySqlDbType.VarChar, 10, "duration_unit");
					parameters.Add("@issue_time", MySqlDbType.DateTime, 10, "issue_time");
				},
				(dataRow, batchItem) =>
				{
					dataRow["player_xuid"] = batchItem.PlayerXuid;
					dataRow["punish_type"] = batchItem.PunishmentType.ToString();
					dataRow["issuer"] = batchItem.Punishment.Issuer;
					dataRow["reason"] = batchItem.Punishment.PunishReason;
					dataRow["active"] = batchItem.Punishment.Active;
					dataRow["duration_amount"] = batchItem.Punishment.DurationAmount;
					dataRow["duration_unit"] = batchItem.Punishment.DurationUnit;
					dataRow["issue_time"] = batchItem.Punishment.GetIssueDate();
					return true;
				},
				null,
				pendingUpdates
				).ExecuteBatch();
			}
		}

		public static void Close()
		{
			RunUpdateTask(); //Run the update task on the plugin thread before closing
			PunishmentUpdateThread.Abort();
		}

		/// <summary>
		/// Simplified version of AddPunishment with only paramaters
		/// relevant to Kicks
		/// </summary>
		/// <param name="xuid"></param>
		/// <param name="reason"></param>
		/// <param name="issuerXuid"></param>
		public static void AddKick(string xuid, string reason, string issuerXuid)
		{
			AddPunishment(xuid, PunishmentType.Kick, new Punishment(
				reason, issuerXuid, false, 0, DurationUnit.Permanent, DateTime.Now
			));
		}

		public static void AddPunishment(string xuid, PunishmentType punishmentType, Punishment punishment)
		{
			PlayerPunishments playerPunishments = GetPunishmentsFor(xuid);

			playerPunishments.AddPunishment(punishmentType, punishment);
			punishment.Dirty = true; //Flag for db saving

			//Ban/Mute have durations, must be held active
			if (punishmentType != PunishmentType.Kick)
			{
				punishment.Active = true; //Set active by default
			}
		}

		public static PlayerPunishments GetPunishmentsFor(string xuid)
		{
			if (PlayerPunishmentCache.ContainsKey(xuid))
			{
				return PlayerPunishmentCache[xuid];
			}

			PlayerPunishments playerPunishments = null;
			new DatabaseAction().Query(
				"SELECT `punish_type`, `issuer`, `reason`, `active`, `duration_amount`, `duration_unit`, `issue_time` FROM `punishments` WHERE `player_xuid`=@xuid;",
				(command) =>
				{
					command.Parameters.AddWithValue("@xuid", xuid);
				},
				(reader) =>
				{
					if (reader.HasRows)
					{
						Dictionary<PunishmentType, SortedSet<Punishment>> punishmentMap = new Dictionary<PunishmentType, SortedSet<Punishment>>();
						while (reader.HasRows)
						{
							do
							{
								try
								{
									Enum.TryParse(reader.GetString(0), out PunishmentType punishmentType);

									SortedSet<Punishment> punishments;
									if (punishmentMap.ContainsKey(punishmentType))
									{
										punishments = punishmentMap[punishmentType];
									}
									else
									{
										punishments = new SortedSet<Punishment>();
										punishmentMap.Add(punishmentType, punishments);
									}

									int durationAmount = reader.GetInt16(4);
									Enum.TryParse(reader.GetString(5), out DurationUnit durationUnit);

									punishments.Add(new Punishment(reader.GetString(2), reader.GetString(1), reader.GetBoolean(3), durationAmount, durationUnit,
										GetExpiryFromIssueDate(reader.GetDateTime(6), durationAmount, durationUnit)));
								}
								catch (Exception e)
								{
									SkyUtil.log($"Failed to read punishment row for xuid='{xuid}'");
									Console.WriteLine(e);
								}
							} while (reader.Read());

							reader.NextResult();
						}

						if (punishmentMap.Count > 0)
						{
							playerPunishments = new PlayerPunishments(punishmentMap);
						}
					}
				},
				null);

			if (playerPunishments == null)
			{
				SkyUtil.log($"No punishments for '{xuid}'. Providing fresh object.");
				playerPunishments = new PlayerPunishments();
			}

			PlayerPunishmentCache.TryAdd(xuid, playerPunishments);

			return playerPunishments;
		}

		private static DateTime GetExpiryFromIssueDate(DateTime issueDate, int durationAmount, DurationUnit durationUnit)
		{
			switch (durationUnit)
			{
				case DurationUnit.Permanent:
				{
					return issueDate;
				}
				case DurationUnit.Minutes:
				{
					return issueDate.AddMinutes(durationAmount);
				}
				case DurationUnit.Hours:
				{
					return issueDate.AddHours(durationAmount);
				}
				case DurationUnit.Days:
				{
					return issueDate.AddDays(durationAmount);
				}
				case DurationUnit.Weeks:
				{
					return issueDate.AddDays(durationAmount * 7);
				}
				case DurationUnit.Months:
				{
					return issueDate.AddMonths(durationAmount);
				}
				case DurationUnit.Years:
				{
					return issueDate.AddYears(durationAmount);
				}
				default:
				{
					return issueDate;
				}
			}
		}
	}
}
