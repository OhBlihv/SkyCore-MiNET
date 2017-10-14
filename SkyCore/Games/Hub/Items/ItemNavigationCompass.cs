﻿using fNbt;
using MiNET.Items;

namespace SkyCore.Games.Hub.Items
{
	public class ItemNavigationCompass : ItemCompass
	{

		private NbtCompound _extraData;

		public override NbtCompound ExtraData
		{
			get => _extraData = UpdateExtraData();
			set => _extraData = value;
		}

		private NbtCompound UpdateExtraData()
		{
			if (_extraData == null)
			{
				_extraData = new NbtCompound
				{
					new NbtCompound("display")
					{
						new NbtString("Name", "§r§d§lNavigation§r"),
						new NbtList("Lore")
						{
							new NbtString("§r§7Use this compass to travel"),
							new NbtString("§r§7between Skytonia Games!")
						}
					}
				};
			}

			return _extraData;
		}


	}
}
