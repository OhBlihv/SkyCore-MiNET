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
						Text = "Murder",
						Image = new Image
						{
							Type = "url",
							Url = "https://cdn.discordapp.com/attachments/192533470608621570/363945144992399362/TestMiNetIcon.png"
						},
						ExecuteAction = delegate { ExternalGameHandler.AddPlayer(player, "murder"); }
					},
					new Button
					{
						Text = "Build Battle",
						Image = new Image
						{
							Type = "url",
							Url = "https://cdn.discordapp.com/attachments/192533470608621570/363945144992399362/TestMiNetIcon.png"
						},
						ExecuteAction = delegate { ExternalGameHandler.AddPlayer(player, "build-battle"); }
					},
					new Button
					{
						Text = "Return to Hub",
						Image = new Image
						{
							Type = "url",
							Url = "https://cdn.discordapp.com/attachments/192533470608621570/363945144992399362/TestMiNetIcon.png"
						},
						ExecuteAction = delegate { ExternalGameHandler.AddPlayer(player, "hub"); }
					}
				}
			};

			player.SendForm(simpleForm);
		}

	}
}
