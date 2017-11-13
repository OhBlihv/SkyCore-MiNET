using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiNET.Net;
using MiNET.Plugins.Attributes;
using Newtonsoft.Json;

namespace SkyCore.Server
{

	public class WhitelistContent
	{

		public bool Enabled { get; set; } = true;

		public string WhitelistMessage { get; set; } = "§7What could this be...?";

		public ISet<string> WhitelistedNames { get; set; } = new HashSet<string>();

		public WhitelistContent()
		{

		}

	}

	public class Whitelist
	{

		private const string WhitelistFilename = @"C:\Users\Administrator\Desktop\config\whitelist.json";

		private static WhitelistContent _whitelist = new WhitelistContent();

		public Whitelist()
		{
			LoadWhitelist();
		}

		public static void LoadWhitelist()
		{
			if (!File.Exists(WhitelistFilename))
			{
				_whitelist.WhitelistedNames.Add("OhBlihv");
				SaveWhitelist();
			}

			_whitelist = JsonConvert.DeserializeObject<WhitelistContent>(File.ReadAllText(WhitelistFilename));
		}

		private static void SaveWhitelist()
		{
			File.WriteAllText(WhitelistFilename, JsonConvert.SerializeObject(_whitelist, Formatting.Indented));
		}

		public static bool IsEnabled()
		{
			return _whitelist.Enabled;
		}

		public static string GetWhitelistMessage()
		{
			return _whitelist.WhitelistMessage;
		}

		public static bool OnWhitelist(string username)
		{
			return _whitelist.WhitelistedNames.Contains(username);
		}

		public static bool AddToWhitelist(string username)
		{
			//Ensure the whitelist is syncronized with other servers before updating it
			LoadWhitelist();

			if (_whitelist.WhitelistedNames.Add(username))
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

			if (_whitelist.WhitelistedNames.Remove(username))
			{
				SaveWhitelist();
				return true;
			}

			return false;
		}

		//

		[Command(Name = "whitelist")]
		[Authorize(Permission = CommandPermission.Normal)]
		public void CommandGameEdit(MiNET.Player player, params string[] args)
		{
			if (player.CommandPermission < CommandPermission.Admin)
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
						return;
					}
					else
					{
						player.SendMessage("§e/whitelist add <name>");
						return;
					}
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
						return;
					}
					else
					{
						player.SendMessage("§e/whitelist remove <name>");
						return;
					}
				}
				else if (args[0].Equals("list"))
				{
					player.SendMessage($"§e§lWhitelist:\n{String.Join(",", _whitelist.WhitelistedNames.ToArray())}");
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

					if (_whitelist.Enabled)
					{
						player.SendMessage("§eWhitelist is already enabled.");
					}
					else
					{
						_whitelist.Enabled = true;
						SaveWhitelist();

						player.SendMessage("§eWhitelist enabled.");
					}
					return;
				}
				else if (args[0].Equals("off"))
				{
					LoadWhitelist();

					if (!_whitelist.Enabled)
					{
						player.SendMessage("§eWhitelist is already disabled.");
					}
					else
					{
						_whitelist.Enabled = false;
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
