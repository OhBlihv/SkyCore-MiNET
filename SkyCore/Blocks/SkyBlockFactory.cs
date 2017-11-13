using MiNET.Blocks;
using SkyCore.Util;

namespace SkyCore.Blocks
{
	public class SkyBlockFactory : ICustomBlockFactory
	{

		public SkyBlockFactory()
		{
			BlockFactory.LuminousBlocks[191] = 15; //Item Frames
		}

		public Block GetBlockById(byte blockId)
		{
			if (blockId == 199) return new FullyLuminousItemFrame();

			return null;
		}

	}
}
