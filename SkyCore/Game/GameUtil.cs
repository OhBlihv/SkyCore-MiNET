using System;
using System.Collections.Generic;
using MiNET.UI;
using SkyCore.BugSnag;
using SkyCore.Player;

namespace SkyCore.Game
{
	public class GameUtil
	{

		public static void ShowGameList(SkyPlayer player)
		{
			try
			{
				if (player == null || !player.IsConnected || player.KnownPosition == null)
				{
					SkyUtil.log("Attempted to show GameList to a null player");
					return;
				}

				var simpleForm = new SimpleForm
				{
					Title = "§lSkytonia Network",
					Content = "",
					Buttons = new List<Button>
					{
						new Button
						{
							Text = $"§3§lNetwork Lobby\n{GetFormattedPlayerCount("hub")}",
							Image = new Image
							{
								Type = "url",
								Url = "https://static.skytonia.com/dl/hubiconmenu.png"
							},
							ExecuteAction = delegate { ExternalGameHandler.AddPlayer(player, "hub"); }
						},
						new Button
						{
							Text = $"§c§lMurder Mystery\n{GetFormattedPlayerCount("murder")}",
							Image = new Image
							{
								Type = "url",
								Url = "https://static.skytonia.com/dl/murdericonmenu.png"
							},
							ExecuteAction = delegate { ExternalGameHandler.AddPlayer(player, "murder"); }
						},
						new Button
						{
							Text = $"§6§l Build Battle\n{GetFormattedPlayerCount("build-battle")}",
							Image = new Image
							{
								Type = "url",
								Url = "https://static.skytonia.com/dl/buildbattleiconmenu.png"
							},
							ExecuteAction = delegate { ExternalGameHandler.AddPlayer(player, "build-battle"); }
						},
						new Button
						{
							Text = $"§d§lComing Soon...",
							Image = new Image
							{
								Type = "url",
								Url = "https://static.skytonia.com/dl/comingsooniconmenu.png"
							},
							ExecuteAction = delegate {  } //Empty
						}
					}
				};

				player.SendForm(simpleForm);
			}
			catch (Exception e)
			{
				BugSnagUtil.ReportBug(e);
			}
		}

		public static string GetFormattedPlayerCount(string gameName)
		{
			int playerCount = -1;
			if(ExternalGameHandler.GameRegistrations.TryGetValue(gameName, out var gamePool))
			{
				playerCount = gamePool.GetCurrentPlayers();
			}

			return $"§r§8({playerCount} Player{(playerCount != 1 ? "s" : "")})";
		}

	}
}
