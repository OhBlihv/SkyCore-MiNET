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
		public void CommandBan(MiNET.Player player, string playerName, params string[] args)
		{
			if (!(player is SkyPlayer skyPlayer) || !skyPlayer.PlayerGroup.IsAtLeast(PlayerGroup.Mod))
			{
				player.SendMessage("§c§l(!)§r §cYou do not have permission for this command.");
				return;
			}

			RunnableTask.RunTask(() =>
			{
				RunPunishmentCommand(player, PunishmentType.Ban, playerName, args);
			});
		}

		[Command(Name = "unban")]
		[Authorize(Permission = CommandPermission.Operator)]
		public void CommandUnban(MiNET.Player player, string playerName)
		{
			if (!(player is SkyPlayer skyPlayer) || !skyPlayer.PlayerGroup.IsAtLeast(PlayerGroup.Mod))
			{
				player.SendMessage("§c§l(!)§r §cYou do not have permission for this command.");
				return;
			}

			RunnableTask.RunTask(() =>
			{
				string targetXuid = StatisticsCore.GetXuidForPlayername(playerName);
				if (targetXuid == null)
				{
					player.SendMessage($"§f[PUNISH] §7{playerName} §cis not a Skytonia user.");
					return;
				}

				if (PunishCore.GetPunishmentsFor(targetXuid).RemoveActive(PunishmentType.Ban))
				{
					player.SendMessage($"§f[PUNISH] §7{playerName} §chas been unbanned.");
				}
				else
				{
					player.SendMessage($"§f[PUNISH] §7{playerName} §cis not currently banned.");
				}
			});
		}

		[Command(Name = "kick")]
		[Authorize(Permission = CommandPermission.Operator)]
		public void CommandKick(MiNET.Player player, string playerName, string[] reason)
		{
			if (!(player is SkyPlayer skyPlayer) || !skyPlayer.PlayerGroup.IsAtLeast(PlayerGroup.Helper))
			{
				player.SendMessage("§c§l(!)§r §cYou do not have permission for this command.");
				return;
			}

			RunnableTask.RunTask(() =>
			{
				string targetXuid = StatisticsCore.GetXuidForPlayername(playerName);
				if (targetXuid == null)
				{
					player.SendMessage($"§f[PUNISH] §7{playerName} §cis not a Skytonia user.");
					return;
				}

				string punishReason = GetReasonFromArgs(reason);

				PunishCore.AddKick(targetXuid, punishReason, player.CertificateData.ExtraData.Xuid);

				SkyPlayer target = SkyCoreAPI.Instance.GetPlayer(playerName);
				target?.Disconnect($"§cYou have been kicked from the server.\n" +
				                   $"§6Reason: {punishReason}");
				
				player.SendMessage($"§f[PUNISH] §7{playerName} §chas been kicked for: §f\"{punishReason}\"");
			});
		}

		[Command(Name = "mute")]
		[Authorize(Permission = CommandPermission.Operator)]
		public void CommandMute(MiNET.Player player, string playerName, params string[] args)
		{
			if (!(player is SkyPlayer skyPlayer) || !skyPlayer.PlayerGroup.IsAtLeast(PlayerGroup.Helper))
			{
				player.SendMessage("§c§l(!)§r §cYou do not have permission for this command.");
				return;
			}

			RunnableTask.RunTask(() =>
			{
				RunPunishmentCommand(player, PunishmentType.Mute, playerName, args);
			});
		}

		[Command(Name = "unmute")]
		[Authorize(Permission = CommandPermission.Operator)]
		public void CommandUnmute(MiNET.Player player, string playerName)
		{
			if (!(player is SkyPlayer skyPlayer) || !skyPlayer.PlayerGroup.IsAtLeast(PlayerGroup.Mod))
			{
				player.SendMessage("§c§l(!)§r §cYou do not have permission for this command.");
				return;
			}

			RunnableTask.RunTask(() =>
			{
				string targetXuid = StatisticsCore.GetXuidForPlayername(playerName);
				if (targetXuid == null)
				{
					player.SendMessage($"§f[PUNISH] §7{playerName} §cis not a Skytonia user.");
					return;
				}

				if (PunishCore.GetPunishmentsFor(targetXuid).RemoveActive(PunishmentType.Mute))
				{
					player.SendMessage($"§f[PUNISH] §7{playerName} §chas been unmuted.");
				}
				else
				{
					player.SendMessage($"§f[PUNISH] §7{playerName} §ch9is not currently muted.");
				}
			});
		}

		private static void RunPunishmentCommand(MiNET.Player player, PunishmentType punishmentType, String playerName, string[] args)
		{
			string targetXuid = StatisticsCore.GetXuidForPlayername(playerName);
			if (targetXuid == null)
			{
				player.SendMessage($"§f[PUNISH] §7{playerName} §cis not a Skytonia user.");
				return;
			}

			args = ParseExpiryTime(player, args, out DurationUnit durationUnit, out int durationAmount);
			if (args == null)
			{
				return; //Message printed to player
			}

			string punishReason = GetReasonFromArgs(args);

			DateTime expiry = UpdateExpiryTime(durationUnit, durationAmount);

			Punishment punishment = new Punishment(punishReason, player.CertificateData.ExtraData.Xuid, true, durationAmount, durationUnit, expiry);
			PunishCore.AddPunishment(targetXuid, punishmentType, punishment);

			if (punishmentType == PunishmentType.Ban)
			{
				SkyPlayer target = SkyCoreAPI.Instance.GetPlayer(playerName);
				if (durationUnit == DurationUnit.Permanent)
				{
					player.SendMessage($"§f[PUNISH] §7{playerName} §chas been banned permanently for: \"{punishReason}\"");

					target?.Disconnect(PunishmentMessages.GetPunishmentMessage(target, punishmentType, punishment));
				}
				else
				{
					player.SendMessage($"§f[PUNISH] §7{playerName} §chas been banned for: §f{GetNeatDuration(durationAmount, durationUnit)} \"{punishReason}\"");

					target?.Disconnect(PunishmentMessages.GetPunishmentMessage(target, punishmentType, punishment));
				}
			}
			else if (punishmentType == PunishmentType.Mute)
			{
				SkyPlayer target = SkyCoreAPI.Instance.GetPlayer(playerName);
				if (durationUnit == DurationUnit.Permanent)
				{
					player.SendMessage($"§f[PUNISH] §7{playerName} §chas been muted permanently for: \"{punishReason}\"");
					
					target?.SendMessage(PunishmentMessages.GetPunishmentMessage(target, PunishmentType.Mute, punishment));
				}
				else
				{
					player.SendMessage($"§f[PUNISH] §7{playerName} §chas been muted for: §f{GetNeatDuration(durationAmount, durationUnit)} \"{punishReason}\"");
					
					target?.SendMessage(PunishmentMessages.GetPunishmentMessage(target, PunishmentType.Mute, punishment));
				}
			}
		}

		private static string GetNeatDuration(int durationAmount, DurationUnit durationUnit)
		{
			if (durationAmount != 1)
			{
				return $"{durationAmount} {durationUnit}";
			}
			else
			{
				string unitString = durationUnit.ToString();
				if (unitString.EndsWith("s"))
				{
					unitString = unitString.Substring(0, unitString.Length - 1);
				}

				return $"{durationAmount} {unitString}";
			}
		}

		private static DateTime UpdateExpiryTime(DurationUnit durationUnit, int durationAmount)
		{
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

			return expiry;
		}

		private static string[] ParseExpiryTime(MiNET.Player player, string[] args, out DurationUnit durationUnit, out int durationAmount)
		{
			durationAmount = -1;
			durationUnit = DurationUnit.Permanent;
			if (args.Length > 0)
			{
				string expiryString = args[0];
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
							if (args[0].Any(char.IsDigit))
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

									string remainingUnitString = expiryString.Substring(idx, expiryString.Length - idx);
									if (Enum.TryParse(remainingUnitString, out durationUnit))
									{
										break;
									}
									switch (remainingUnitString.ToLower())
									{
										case "m":
											durationUnit = DurationUnit.Minutes;
											break;
										case "h":
											durationUnit = DurationUnit.Hours;
											break;
										case "d":
											durationUnit = DurationUnit.Days;
											break;
										case "w":
											durationUnit = DurationUnit.Weeks;
											break;
										case "mon":
											durationUnit = DurationUnit.Months;
											break;
										case "y":
											durationUnit = DurationUnit.Years;
											break;
									}

									if (durationUnit == DurationUnit.Permanent)
									{
										player.SendMessage($"§cUnable to parse expiryString unit '{expiryString.Substring(idx, expiryString.Length)}' from '{expiryString}'.");
										return null;
									}
								}

								if (!int.TryParse(amountString, out durationAmount))
								{
									player.SendMessage($"§cUnable to parse expiryString amount '{amountString}' from '{expiryString}'.");
									return null;
								}

								if (durationAmount <= 0)
								{
									player.SendMessage($"§cInvalid duration: '{durationAmount} {durationUnit}'.");
									return null;
								}
							}
							break;
						}
					}

					if (durationAmount > 0)
					{
						//Update args to remove the first entry
						if (args.Length > 1)
						{
							args = args.Skip(1).ToArray(); //Skip the first entry
						}
						else
						{
							args = new string[0]; //No more args
						}
					}
				}

				//Handles Perm ban amount
				if (durationAmount == -1)
				{
					durationAmount = 0;
				}
			}
			
			return args;
		}

		private static string GetReasonFromArgs(string[] reasonArgs)
		{
			string punishReason = null;
			if (reasonArgs != null)
			{
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
			}

			if (punishReason == null)
			{
				punishReason = "No Reason Provided.";
			}

			return punishReason;
		}

	}
}
