using System;
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
							Url = "https://static.skytonia.com/dl/hubicon3.png"
						},
						ExecuteAction = delegate { ExternalGameHandler.AddPlayer(player, "hub"); }
					},
					new Button
					{
						Text = "Murder",
						Image = new Image
						{
							Type = "url",
							Url = "https://static.skytonia.com/dl/murdericon2.png"
						},
						ExecuteAction = delegate { ExternalGameHandler.AddPlayer(player, "murder"); }
					},
					new Button
					{
						Text = "Build Battle",
						Image = new Image
						{
							Type = "url",
							Url = "https://static.skytonia.com/dl/buildbattleicon6.png"
						},
						ExecuteAction = delegate { ExternalGameHandler.AddPlayer(player, "build-battle"); }
					},
					new Button
					{
						Text = "Coming Soon",
						Image = new Image
						{
							Type = "url",
							Url = "https://static.skytonia.com/dl/comingsoonicon2.png"
						},
						ExecuteAction = delegate {  } //Empty
					}
				}
			};

			player.SendForm(simpleForm);
		}

	}
}
