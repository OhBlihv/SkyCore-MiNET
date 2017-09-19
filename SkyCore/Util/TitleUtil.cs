using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiNET;

namespace SkyCore.Util
{
	public class TitleUtil
	{

		public static void SendCenteredSubtitle(MiNET.Player player, string content)
		{
			SendCenteredSubtitle(player, content, 5, 5, 100);
		}

		public static void SendCenteredSubtitle(MiNET.Player player, string content, int fadein, int fadeOut, int stayTime)
		{
			string subtitleString = "";
			int i = 0;
			while (i++ < 3)
			{
				subtitleString += "\n";
			}

			foreach(string line in content.Split('\n'))
			{
				i++;

				subtitleString += line + "\n";
			}

			while (i++ < 10)
			{
				subtitleString += "\n";
			}

			player.SendTitle("§f", TitleType.AnimationTimes, fadein, fadeOut, stayTime);
			player.SendTitle(subtitleString, TitleType.SubTitle);
			player.SendTitle("§f", TitleType.Title);
		}

	}
}
