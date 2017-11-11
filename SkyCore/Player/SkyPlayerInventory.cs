using MiNET;
using MiNET.Items;
using MiNET.Net;
using MiNET.Worlds;
using SkyCore.Games.Hub;

namespace SkyCore.Player
{
	public class SkyPlayerInventory : PlayerInventory
	{

		public SkyPlayerInventory(MiNET.Player player) : base(player)
		{
			
		}

		public override void SetHeldItemSlot(int selectedHotbarSlot, bool sendToPlayer = true)
		{
			if (Player is SkyPlayer player)
			{
				if (player.IsGameSpectator && Player.Level is HubLevel)
				{
					InHandSlot = selectedHotbarSlot;

					//Don't send any changes to the player
					//This avoids the noticable 'flick' to the 0th slot that gets in the way
					if (sendToPlayer)
					{
						McpeMobEquipment order = McpeMobEquipment.CreateObject();
						order.runtimeEntityId = EntityManager.EntityIdSelf;
						order.item = GetItemInHand();
						order.selectedSlot = (byte)selectedHotbarSlot;
						order.slot = (byte)ItemHotbar[InHandSlot];
						Player.SendPackage(order);
					}

					McpeMobEquipment broadcast = McpeMobEquipment.CreateObject();
					broadcast.runtimeEntityId = Player.EntityId;
					broadcast.item = new ItemAir();
					broadcast.selectedSlot = 0;
					broadcast.slot = (byte)ItemHotbar[InHandSlot];
					Player.Level?.RelayBroadcast(Player, broadcast);
				}
				else
				{
					base.SetHeldItemSlot(selectedHotbarSlot, sendToPlayer);
				}

				player.HandleHeldItemSlotChange(selectedHotbarSlot);
			}
			else
			{
				base.SetHeldItemSlot(selectedHotbarSlot, sendToPlayer);
			}
		}
	}
}
