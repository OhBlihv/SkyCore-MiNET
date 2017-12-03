using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiNET.Plugins.Attributes;
using Newtonsoft.Json;
using SkyCore.Permissions;
using SkyCore.Player;

namespace SkyCore.Server
{

	public class WhitelistContent
	{

		public bool Enabled { get; set; } = true;

		public string WhitelistMessage { get; set; } = SkyCoreAPI.IsDevelopmentServer 
			? "§cThis server is restricted to whitelisted players only."
			: "§7What could this be...?";

		public ISet<string> WhitelistedNames { get; set; } = new HashSet<string>();

	}

	public class Whitelist
	{

		private static string WhitelistFilename { get; }

		static Whitelist()
		{
			//Separate Dev/Live whitelists for obvious reasons
			WhitelistFilename = $@"C:\Users\Administrator\Desktop\config\{(SkyCoreAPI.IsDevelopmentServer ? "dev-" : "")}whitelist.json";
		}

		public static WhitelistContent WhitelistContent { get; private set; } = new WhitelistContent();

		public Whitelist()
		{
			LoadWhitelist();
		}

		public static void LoadWhitelist()
		{
			if (!File.Exists(WhitelistFilename))
			{
				WhitelistContent.WhitelistedNames.Add("OhBlihv");
				SaveWhitelist();
			}

			WhitelistContent = JsonConvert.DeserializeObject<WhitelistContent>(File.ReadAllText(WhitelistFilename));
		}

		public static void SaveWhitelist()
		{
			File.WriteAllText(WhitelistFilename, JsonConvert.SerializeObject(WhitelistContent, Formatting.Indented));
		}

		public static bool IsEnabled()
		{
			return WhitelistContent.Enabled;
		}

		public static string GetWhitelistMessage()
		{
			return WhitelistContent.WhitelistMessage;
		}

		public static bool OnWhitelist(string username)
		{
			return WhitelistContent.WhitelistedNames.Contains(username);
		}

		public static bool AddToWhitelist(string username)
		{
			//Ensure the whitelist is syncronized with other servers before updating it
			LoadWhitelist();

			if (WhitelistContent.WhitelistedNames.Add(username))
			{
				SaveWhitelist();
				return true;
			}

			return false;
		}

		public static bool RemoveFromWhitelist(string username)
		{
			//Ensure the whitelist is syncronized with other servers before updating it
			LoadWhitelist();

			if (WhitelistContent.WhitelistedNames.Remove(username))
			{
				SaveWhitelist();
				return true;
			}

			return false;
		}

		//

		[Command(Name = "whitelist")]
		[Authorize(Permission = (int)PlayerGroupCommandPermissions.Admin)]
		public void CommandGameEdit(MiNET.Player player, params string[] args)
		{
			if (!(player is SkyPlayer skyPlayer) || !skyPlayer.PlayerGroup.IsAtLeast(PlayerGroup.Admin))
			{
				player.SendMessage("§c§l(!)§r §cYou do not have permission for this command.");
				return;
			}

			if (args.Length > 0)
			{
				if (args[0].Equals("add"))
				{
					if (args.Length > 1)
					{
						if (AddToWhitelist(args[1]))
						{
							player.SendMessage($"§eAdded '{args[1]}' to the whitelist.");
						}
						else
						{
							player.SendMessage($"§e'{args[1]}' is already on the whitelist.");
						}
					}
					else
					{
						player.SendMessage("§e/whitelist add <name>");
					}

					return;
				}
				else if (args[0].Equals("remove"))
				{
					if (args.Length > 1)
					{
						if (RemoveFromWhitelist(args[1]))
						{
							player.SendMessage($"§eRemoved '{args[1]}' from the whitelist.");
						}
						else
						{
							player.SendMessage($"§e'{args[1]}' was not on the whitelist.");
						}
					}
					else
					{
						player.SendMessage("§e/whitelist remove <name>");
					}

					return;
				}
				else if (args[0].Equals("list"))
				{
					player.SendMessage($"§e§lWhitelist:\n{String.Join(",", WhitelistContent.WhitelistedNames.ToArray())}");
					return;
				}
				else if (args[0].Equals("reload"))
				{
					LoadWhitelist();
					player.SendMessage("§eReloaded Whitelist.");
					return;
				}
				else if (args[0].Equals("on"))
				{
					LoadWhitelist();

					if (WhitelistContent.Enabled)
					{
						player.SendMessage("§eWhitelist is already enabled.");
					}
					else
					{
						WhitelistContent.Enabled = true;
						SaveWhitelist();

						player.SendMessage("§eWhitelist enabled.");
					}
					return;
				}
				else if (args[0].Equals("off"))
				{
					LoadWhitelist();

					if (!WhitelistContent.Enabled)
					{
						player.SendMessage("§eWhitelist is already disabled.");
					}
					else
					{
						WhitelistContent.Enabled = false;
						SaveWhitelist();

						player.SendMessage("§eWhitelist disabled.");
					}
					return;
				}
			}

			player.SendMessage("§e/whitelist <add/remove/list/reload>");
		}
	}
}
