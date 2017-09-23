using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyCore.Util
{

	class TextUtil
	{

		public static int GetCharLength(char character)
		{
			switch (character)
			{
				case 'f': return 4;
				case 'I': return 3;
				case 'i': return 1;
				case 'k': return 4;
				case 'l': return 1;
				case 't': return 4;
				case '!': return 1;
				case '@': return 6;
				case '(': return 4;
				case ')': return 4;
				case '{': return 4;
				case '}': return 4;
				case '[': return 3;
				case ']': return 3;
				case ';': return 1;
				case ':': return 1;
				case '"': return 3;
				case '\'': return 1;
				case '<': return 4;
				case '>': return 4;
				case '|': return 1;
				case '`': return 1;
				case '.': return 1;
				case ',': return 1;
				//case ' ': return 3;
				case ' ': return 3;
				default: return 5;
			}
		}

		public static int GetLineLength(string line)
		{
			int length = 0;

			bool isColour = false;
			bool isBold = false;
			foreach (char character in line)
			{
				if (character == '§')
				{
					isColour = true;
					continue;
				}
				if (isColour)
				{
					if (character == 'l')
					{
						isBold = true;
					}
					else if (character == 'r')
					{
						isBold = false;
					}

					isColour = false;
					continue;
				}

				length += GetCharLength(character);

				if (isBold/* && character != ' '*/)
				{
					length += 2;
				}
			}

			return length;
		}

	}
}
