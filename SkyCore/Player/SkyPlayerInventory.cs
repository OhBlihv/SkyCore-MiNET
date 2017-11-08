using MiNET;

namespace SkyCore.Player
{
	public class SkyPlayerInventory : PlayerInventory
	{

		public SkyPlayerInventory(MiNET.Player player) : base(player)
		{
			
		}

		public override void SetHeldItemSlot(int selectedHotbarSlot, bool sendToPlayer = true)
		{
			base.SetHeldItemSlot(selectedHotbarSlot, sendToPlayer);

			if (Player is SkyPlayer player)
			{
				player.HandleHeldItemSlotChange(selectedHotbarSlot);
			}
		}
	}
}
