using MiNET;
using MiNET.Items;

namespace SkyCore.Player
{
	class SkyFoodManager : HungerManager
	{
		public SkyFoodManager(MiNET.Player player) : base(player)
		{

		}

		public override void IncreaseExhaustion(float amount)
		{
		}

		public override void IncreaseFoodAndSaturation(Item item, int foodPoints, double saturationRestore)
		{
		}

		public override void Move(double distance)
		{
		}

	}
}
