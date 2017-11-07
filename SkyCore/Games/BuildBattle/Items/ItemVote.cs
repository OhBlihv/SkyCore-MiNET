using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fNbt;
using MiNET.Blocks;
using MiNET.Items;

namespace SkyCore.Games.BuildBattle.Items
{
	public class ItemVote : Item
	{

		private readonly int _voteValue;
		private readonly string _votePlayer;

		public ItemVote(int voteValue, string votePlayer) : base(160, (short) GetVoteId(_getCorrectedVoteValue(voteValue)))
		{
			_voteValue = _getCorrectedVoteValue(voteValue);

			_votePlayer = votePlayer;

			Durability = GetVoteId(voteValue);
		}

		private static int _getCorrectedVoteValue(int voteValue)
		{
			if (voteValue < 1)
			{
				voteValue = 1;
			}
			else if (voteValue > 5)
			{
				voteValue = 5;
			}

			return voteValue;
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

		public static int GetVoteId(int i)
		{
			int damage;
			switch (i)
			{
				case 1: //Terrible
					damage = 14;
					break;
				case 2: //Poor
					damage = 1;
					break;
				case 3: //Alright
					damage = 13;
					break;
				case 4: //Great
					damage = 3;
					break;
				case 5: //Amazing
					damage = 2;
					break;
				default:
					damage = 0;
					break;
			}

			return damage;
		}

		public static string GetVoteName(int i)
		{
			string voteName;
			switch (i)
			{
				case 1:
					voteName = "§cTerrible";
					break;
				case 2:
					voteName = "§6Poor";
					break;
				case 3:
					voteName = "§2Alright";
					break;
				case 4:
					voteName = "§bGreat";
					break;
				case 5:
					voteName = "§dAmazing";
					break;
				default:
					voteName = "§dN/A";
					break;
			}

			return voteName;
		}

	}
}
