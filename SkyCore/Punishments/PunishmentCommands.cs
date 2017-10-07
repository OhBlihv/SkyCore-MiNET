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
		public void CommandBan(MiNET.Player player, string playerName, string expiryString = "", params string[] reason) //TODO: Change to string if this doesn't work
		{
			if (!((SkyPlayer) player).PlayerGroup.isAtLeast(PlayerGroup.Mod))
			{
				player.SendMessage("§cInsufficient Permission.");
				return;
			}

			RunnableTask.RunTask(() =>
			{
				string targetXuid = StatisticsCore.GetXuidForPlayername(playerName);
				if (targetXuid == null)
				{
					player.SendMessage($"§c{playerName} has never played before.");
					return;
				}

				int durationAmount = -1;
				DurationUnit durationUnit = DurationUnit.Permanent;
				if (expiryString.Length > 0)
				{
					switch (expiryString.ToUpper())
					{
						case "FOREVER":
						case "PERM":
						case "PERMANENT":
						{
							durationAmount = 0;
							//DurationUnit is already correct.	
							break;
						}
						default:
						{
							string amountString = "";
							for (int idx = 0; idx < expiryString.Length; idx++)
							{
								char charAt = expiryString[idx];
								if (durationAmount == -1 && Char.IsDigit(charAt))
								{
									amountString += charAt;
									continue;
								}

								if (Enum.TryParse(expiryString.Substring(idx, expiryString.Length), out durationUnit))
								{
									break;
								}
								else
								{
									player.SendMessage($"§cUnable to parse expiryString unit '{expiryString.Substring(idx, expiryString.Length)}'.");
									return;
								}
							}

							if (!int.TryParse(amountString, out durationAmount))
							{
								player.SendMessage($"§cUnable to parse expiryString amount '{amountString}'.");
								return;
							}
							break;
						}
					}
				}

				if (durationAmount == -1)
				{
					durationAmount = 0;
				}

				string punishReason = GetReasonFromArgs(reason);

				DateTime expiry = DateTime.Now;
				switch (durationUnit)
				{
					case DurationUnit.Permanent:
					{
						break; //Expiry is issue date
					}
					case DurationUnit.Minutes:
					{
						expiry = expiry.AddMinutes(durationAmount);
						break;
					}
					case DurationUnit.Hours:
					{
						expiry = expiry.AddHours(durationAmount);
						break;
					}
					case DurationUnit.Days:
					{
						expiry = expiry.AddDays(durationAmount);
						break;
					}
					case DurationUnit.Weeks:
					{
						expiry = expiry.AddDays(durationAmount * 7);
						break;
					}
					case DurationUnit.Months:
					{
						expiry = expiry.AddMonths(durationAmount);
						break;
					}
					case DurationUnit.Years:
					{
						expiry = expiry.AddYears(durationAmount);
						break;
					}
				}

				PunishCore.AddPunishment(targetXuid, PunishmentType.Ban, new Punishment(punishReason, player.CertificateData.ExtraData.Xuid, true, durationAmount, durationUnit, expiry));

				SkyPlayer target = SkyCoreAPI.Instance.GetPlayer(playerName);
				if (durationUnit == DurationUnit.Permanent)
				{
					target?.Disconnect($"§cYou have been banned permanently.\n" +
					                   $"§6Reason: {punishReason}");
				}
				else
				{
					target?.Disconnect($"§cYou have been banned for {durationAmount} {durationUnit}\n" +
					                   $"§6Reason: {punishReason}");
				}
			});
		}

		[Command(Name = "unban")]
		[Authorize(Permission = CommandPermission.Operator)]
		public void CommandUnban(MiNET.Player player, string playerName)
		{
			if (!((SkyPlayer)player).PlayerGroup.isAtLeast(PlayerGroup.Mod))
			{
				player.SendMessage("§cInsufficient Permission.");
				return;
			}

			RunnableTask.RunTask(() =>
			{
				string targetXuid = StatisticsCore.GetXuidForPlayername(playerName);
				if (targetXuid == null)
				{
					player.SendMessage($"§c{playerName} has never played before.");
					return;
				}

				if (PunishCore.GetPunishmentsFor(targetXuid).RemoveActive(PunishmentType.Ban))
				{
					player.SendMessage($"§eUnbanned {playerName}");
				}
				else
				{
					player.SendMessage($"§c{playerName} is not currently banned.");
				}
			});
		}

		[Command(Name = "kick")]
		[Authorize(Permission = CommandPermission.Operator)]
		public void CommandKick(MiNET.Player player, string playerName, string[] reason)
		{
			if (!((SkyPlayer)player).PlayerGroup.isAtLeast(PlayerGroup.Mod))
			{
				player.SendMessage("§cInsufficient Permission.");
				return;
			}

			RunnableTask.RunTask(() =>
			{
				string targetXuid = StatisticsCore.GetXuidForPlayername(playerName);
				if (targetXuid == null)
				{
					player.SendMessage($"§c{playerName} has never played before.");
					return;
				}

				string punishReason = GetReasonFromArgs(reason);

				PunishCore.AddKick(targetXuid, punishReason, player.CertificateData.ExtraData.Xuid);

				SkyPlayer target = SkyCoreAPI.Instance.GetPlayer(playerName);
				target?.Disconnect($"§cYou have been kicked from the server.\n" +
				                   $"§6Reason: {punishReason}");
			});
		}

		private string GetReasonFromArgs(string[] reasonArgs)
		{
			string punishReason;
			if (reasonArgs.Length > 1)
			{
				punishReason = "";
				int length = reasonArgs.Length;
				for (int idx = 0; idx < length; idx++)
				{
					string word = reasonArgs[idx];

					punishReason += word;
					if (idx < (length - 1))
					{
						punishReason += " ";
					}
				}
			}
			else if (reasonArgs.Length == 1)
			{
				punishReason = reasonArgs[0];
			}
			else
			{
				punishReason = "No Reason Provided.";
			}

			return punishReason;
		}

	}
}
