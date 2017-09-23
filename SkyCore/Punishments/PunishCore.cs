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

	public class Punishment
	{

		public string PunishReason { get; }

		public DateTime Expiry { get; }

		public Punishment(string punishReason, DateTime expiry)
		{
			PunishReason = punishReason;
			Expiry = expiry;
		}

		public bool IsActive()
		{
			return false; //TODO
		}

	}

	public class PlayerPunishments
	{

		private readonly Dictionary<PunishmentType, Punishment> Punishments;

	}

	public class PunishCore
	{

		private readonly ConcurrentDictionary<string, Punishment> _playerPunishmentCache = new ConcurrentDictionary<string, Punishment>();

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
					"`expiry_time`       DATETIME2(1),\n" + //Lowest precision type for lowest storage usage
					" PRIMARY KEY(`player_xuid`, `punish_type`, `expiry_time`)\n" +
					");",
					null, null, null);
			});
		}

		public static void GetPunishmentsFor(string xuid)
		{
			
		}

	}
}
