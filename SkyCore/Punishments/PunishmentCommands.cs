using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiNET.Net;
using MiNET.Plugins.Attributes;
using SkyCore.Permissions;
using SkyCore.Player;
using SkyCore.Statistics;
using SkyCore.Util;

namespace SkyCore.Punishments
{
	public class PunishmentCommands
	{

		[Command(Name = "ban")]
		[Authorize(Permission = CommandPermission.Operator)]
		public void CommandBan(MiNET.Player player, string playerName, string expiry = "", params string[] reason) //TODO: Change to string if this doesn't work
		{
			string targetXuid = StatisticsCore.GetXuidForPlayername(playerName);
			if (targetXuid == null)
			{
				player.SendMessage($"§c{playerName} has never played before.");
				return;
			}

			RunnableTask.RunTask(() =>
			{
				//TODO: Decide whether to hold the issuers xuid of username
				//TODO: Reason
				PunishCore.AddPunishment(targetXuid, PunishmentType.Ban, new Punishment("", player.CertificateData.ExtraData.Xuid, null));

				SkyPlayer target = SkyCoreAPI.Instance.GetPlayer(playerName);
				target?.Disconnect("§cYou have been banned.\n" +
				                   $"§6Reason: {null}");
			});
		}

		[Command(Name = "unban")]
		[Authorize(Permission = CommandPermission.Operator)]
		public void CommandUnban(MiNET.Player player, string playerName)
		{
			string targetXuid = StatisticsCore.GetXuidForPlayername(playerName);
			if (targetXuid == null)
			{
				player.SendMessage($"§c{playerName} has never played before.");
				return;
			}


		}

	}
}
