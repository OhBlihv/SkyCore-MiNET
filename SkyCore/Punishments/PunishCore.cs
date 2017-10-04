using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkyCore.Database;
using SkyCore.Util;

namespace SkyCore.Punishments
{

	public enum PunishmentType
	{
		
		Ban,
		Kick,
		Mute

	}

	public class Punishment : IComparable
	{

		public string PunishReason { get; }

		public string PunishingPlayer { get; }

		public DateTime? Expiry { get; }

		public Punishment(string punishReason, string punishingPlayer, DateTime? expiry)
		{
			PunishReason = punishReason;
			PunishingPlayer = punishingPlayer;
			Expiry = expiry;
		}

		public bool IsActive()
		{
			return Expiry?.CompareTo(DateTime.Now) >= 0;
		}

		public int CompareTo(object obj)
		{
			if (!(obj is Punishment pObj))
			{
				return 0;
			}

			if (Expiry == null)
			{
				return -1; //Order First
			}

			return Expiry.Value.CompareTo(pObj.Expiry);
		}
	}

	public class PlayerPunishments
	{

		//Holds active/most recent punishments at the start of the set
		private readonly Dictionary<PunishmentType, SortedSet<Punishment>> _punishments;

		public PlayerPunishments()
		{
			_punishments = new Dictionary<PunishmentType, SortedSet<Punishment>>();
		}

		public PlayerPunishments(Dictionary<PunishmentType, SortedSet<Punishment>> punishments)
		{
			_punishments = punishments;
		}

		public void AddPunishment(PunishmentType punishmentType, Punishment punishment)
		{
			SortedSet<Punishment> punishments;
			if (_punishments.ContainsKey(punishmentType))
			{
				punishments = _punishments[punishmentType];
			}
			else
			{
				punishments = new SortedSet<Punishment>();
				_punishments.Add(punishmentType, punishments);
			}

			punishments.Add(punishment);
		}

		public Punishment GetActive(PunishmentType punishmentType)
		{
			if (_punishments.ContainsKey(punishmentType))
			{
				SortedSet<Punishment> punishments = _punishments[punishmentType];
				if (punishments.Count == 0)
				{
					return null;
				}

				//Dodgy way to get 1st entry
				return punishments.GetEnumerator().Current;	
			}

			return null;
		}

		public bool HasActive(PunishmentType punishmentType)
		{
			var punishment = GetActive(punishmentType);
			return punishment != null && punishment.IsActive();
		}

	}

	public class PunishCore
	{

		//xuid -> Punishments
		private static readonly ConcurrentDictionary<string, PlayerPunishments> PlayerPunishmentCache = new ConcurrentDictionary<string, PlayerPunishments>();

		static PunishCore()
		{
			RunnableTask.RunTask(() =>
			{
				new DatabaseAction().Query(
					"CREATE TABLE IF NOT EXISTS `punishments` (\n" +
					"`player_xuid`       varchar(50),\n" +
					"`punish_type`       varchar(16),\n" +
					"`executing_player`  varchar(16),\n" +
					"`reason`			 varchar(128),\n" +
					"`expiry_time`       DATETIME(1),\n" +
					" PRIMARY KEY(`player_xuid`, `punish_type`, `expiry_time`)\n" +
					");",
					null, null, null);
			});
		}

		public static void AddPunishment(string xuid, PunishmentType punishmentType, Punishment punishment)
		{
			PlayerPunishments playerPunishments = GetPunishmentsFor(xuid);

			playerPunishments.AddPunishment(punishmentType, punishment);
		}

		public static PlayerPunishments GetPunishmentsFor(string xuid)
		{
			if (PlayerPunishmentCache.ContainsKey(xuid))
			{
				return PlayerPunishmentCache[xuid];
			}

			PlayerPunishments playerPunishments = null;
			new DatabaseAction().Query(
				"SELECT `punish_type`, `executing_player`, `reason`, `expiry_time` FROM `punishments` WHERE `player_xuid`=@xuid;",
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
							try
							{
								PunishmentType.TryParse(reader.GetString(0), out PunishmentType punishmentType);

								SortedSet<Punishment> punishments;
								if (punishmentMap.ContainsKey(punishmentType))
								{
									punishments = punishmentMap[punishmentType];
								}
								else
								{
									punishments = new SortedSet<Punishment>();
								}

								punishments.Add(new Punishment(reader.GetString(2), reader.GetString(1), reader.GetDateTime(3)));

								punishmentMap.Add(punishmentType, punishments);
							}
							catch (Exception e)
							{
								SkyUtil.log($"Failed to read punishment row for xuid='{xuid}'");
								Console.WriteLine(e);
							}
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

	}
}
