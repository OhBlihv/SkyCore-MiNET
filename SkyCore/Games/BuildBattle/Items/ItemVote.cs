using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fNbt;
using MiNET.Items;

namespace SkyCore.Games.BuildBattle.Items
{
	public class ItemVote : ItemMap //TODO: Change to ItemWool
	{

		private readonly int _voteValue;
		private readonly string _votePlayer;

		public ItemVote(int voteValue, string votePlayer)
		{
			_voteValue = voteValue;
			if (_voteValue < 1)
			{
				_voteValue = 1;
			}
			else if (_voteValue > 5)
			{
				_voteValue = 5;
			}
			
			_votePlayer = votePlayer;
		}

		private static readonly NbtCompound[] _extraData = new NbtCompound[6]; //1-5

		public override NbtCompound ExtraData
		{
			get => _extraData[_voteValue] = UpdateExtraData();
			set => _extraData[_voteValue] = value;
		}

		private NbtCompound UpdateExtraData()
		{
			if (_extraData[_voteValue] == null)
			{
				string voteName = GetVoteName(_voteValue);

				_extraData[_voteValue] = new NbtCompound
				{
					new NbtCompound("display")
					{
						//new NbtString("Name", $"§r§d§lVote:§r {voteName}§r"),
						new NbtString("Name", "§r"),
						new NbtList("Lore")
						{
							new NbtString($"§r§7Hold this to vote §d{_votePlayer}'s§r"),
							new NbtString($"§r§7build§r {voteName}")
						}
					}
				};
			}

			return _extraData[_voteValue];
		}

		public static string GetVoteName(int i)
		{
			string voteName;
			switch (i)
			{
				case 1:
					voteName = "§4Poop";
					break;
				case 2:
					voteName = "§cPoor";
					break;
				case 3:
					voteName = "§6Alright";
					break;
				case 4:
					voteName = "§eGood";
					break;
				case 5:
					voteName = "§aAwesome!";
					break;
				default:
					voteName = "§dN/A";
					break;
			}

			return voteName;
		}

	}
}
