using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiNET.Blocks;
using SkyCore.Util;

namespace SkyCore.Blocks
{
	public class SkyBlockFactory : ICustomBlockFactory
	{

		public SkyBlockFactory()
		{
			BlockFactory.LuminousBlocks.Add(199, 15); //Item Frames
		}

		public Block GetBlockById(byte blockId)
		{
			if (blockId == 199) return new FullyLuminousItemFrame();

			return null;
		}

	}
}
