﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiNET;
using MiNET.Utils;

namespace SkyCore.Util
{
	public class TitleUtil
	{

		public static void SendCenteredSubtitle(MiNET.Player player, string content, bool centerText = true)
		{
			SendCenteredSubtitle(player, content, centerText, 5, 5, 100);
		}

		public static void SendCenteredSubtitle(MiNET.Player player, string content, bool centreText, int fadein, int fadeOut, int stayTime)
		{
			string subtitleString = "";
			int i = 0;
			while (i++ < 3)
			{
				subtitleString += "§r\n";
			}

			if (centreText)
			{
				subtitleString += TextUtils.Center(content);
			}

			while (i++ < 10)
			{
				subtitleString += "§f\n";
			}

			player.SendTitle("§f", TitleType.AnimationTimes, fadein, fadeOut, stayTime);
			player.SendTitle(subtitleString, TitleType.SubTitle);
			player.SendTitle("§f", TitleType.Title);
		}

	}
}
