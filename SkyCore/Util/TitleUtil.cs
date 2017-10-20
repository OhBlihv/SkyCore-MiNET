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

			int longestLine = 0;
			List<string> centeredLines = new List<string>();
			foreach(string line in content.Split('\n'))
			{
				i++;

				centeredLines.Add(line);

				int lineLength = TextUtil.GetLineLength(line);
				if (lineLength > longestLine)
				{
					longestLine = lineLength;
				}
			}

			foreach (string line in content.Split('\n'))
			{
				string centeredLine = line;

				if (centreText)
				{
					//int j = centeredLine.Length;
					int j = TextUtil.GetLineLength(centeredLine);
					//SkyUtil.log($"({longestLine - centeredLine.Length} < {j}) - ({longestLine} - {centeredLine.Length})");
					if (j < longestLine)
					{
						//int spaceLength = TextUtil.GetCharLength(' ');

						do
						{
							//SkyUtil.log($"Adding space to ({j} < {longestLine - centeredLine.Length}) - ({longestLine} - {centeredLine.Length})");
							centeredLine = " " + centeredLine;

							//j += 3; //(3 is the length of the space character)
							//j += spaceLength; //(3 is the length of the space character)
							j += 6; //(3 is the length of the space character)
						} while (j < longestLine);

						centeredLine = " " + centeredLine; //Add one more space for good luck

						centeredLine = "§f" + centeredLine;
					}
				}

				subtitleString += centeredLine + "\n";
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
