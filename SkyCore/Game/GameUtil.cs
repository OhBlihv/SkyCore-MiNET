﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
				if (player == null)
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
							Text = "Hub",
							Image = new Image
							{
								Type = "url",
								Url = "https://static.skytonia.com/dl/hubicon.png"
							},
							ExecuteAction = delegate { ExternalGameHandler.AddPlayer(player, "hub"); }
						},
						new Button
						{
							Text = "Murder",
							Image = new Image
							{
								Type = "url",
								Url = "https://static.skytonia.com/dl/murdericon.png"
							},
							ExecuteAction = delegate { ExternalGameHandler.AddPlayer(player, "murder"); }
						},
						new Button
						{
							Text = "Build Battle",
							Image = new Image
							{
								Type = "url",
								Url = "https://static.skytonia.com/dl/buildbattleicon.png"
							},
							ExecuteAction = delegate { ExternalGameHandler.AddPlayer(player, "build-battle"); }
						},
						new Button
						{
							Text = "Coming Soon",
							Image = new Image
							{
								Type = "url",
								Url = "https://static.skytonia.com/dl/comingsoonicon.png"
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

	}
}
