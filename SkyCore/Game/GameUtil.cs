using System;
using System.Collections.Generic;
using MiNET.UI;
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
					Title = "Game list",
					Content = "",
					Buttons = new List<Button>
					{
						new Button
						{
							Text = $"Hub",
							Image = new Image
							{
								Type = "url",
								Url = "https://static.skytonia.com/dl/hubiconmenu.png"
							},
							ExecuteAction = delegate { ExternalGameHandler.AddPlayer(player, "hub"); }
						},
						new Button
						{
							Text = $"Murder\n{GetFormattedPlayerCount("murder")}",
							Image = new Image
							{
								Type = "url",
								Url = "https://static.skytonia.com/dl/murdericonmenu.png"
							},
							ExecuteAction = delegate { ExternalGameHandler.AddPlayer(player, "murder"); }
						},
						new Button
						{
							Text = $"Build Battle\n{GetFormattedPlayerCount("build-battle")}",
							Image = new Image
							{
								Type = "url",
								Url = "https://static.skytonia.com/dl/buildbattleiconmenu.png"
							},
							ExecuteAction = delegate { ExternalGameHandler.AddPlayer(player, "build-battle"); }
						},
						new Button
						{
							Text = $"Coming Soon",
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
				Console.WriteLine(e);
				throw;
			}
		}

		public static string GetFormattedPlayerCount(string gameName)
		{
			int playerCount = -1;
			if(ExternalGameHandler.GameRegistrations.TryGetValue(gameName, out var gamePool))
			{
				playerCount = gamePool.GetCurrentPlayers();
			}

			return $"§6({playerCount})";
		}

	}
}
