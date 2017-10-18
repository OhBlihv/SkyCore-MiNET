using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiNET.Effects;
using SkyCore.Game;
using SkyCore.Game.Level;
using SkyCore.Game.State;
using SkyCore.Games.Hub.Items;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Games.Hub.State
{
	public class HubState : GameState
	{
		public override void EnterState(GameLevel gameLevel)
		{
			//
		}

		public override void LeaveState(GameLevel gameLevel)
		{
			//
		}

		public override bool CanAddPlayer(GameLevel gameLevel)
		{
			return gameLevel.GetPlayerCount() < 100; //TODO: Add Constant
		}

		public override void InitializePlayer(GameLevel gameLevel, SkyPlayer player)
		{
			NightVision nightVision = new NightVision
			{
				Duration = int.MaxValue,
				Level = 0,
				Particles = false
			};
			player.SetEffect(nightVision);

			player.Inventory.SetInventorySlot(4, new ItemNavigationCompass());

			player.Inventory.SetHeldItemSlot(4);

			RunnableTask.RunTaskLater(() =>
			{
				player.Inventory.SetHeldItemSlot(4);
			}, 2000);
		}

		public override void OnTick(GameLevel gameLevel, int currentTick, out int outTick)
		{
			outTick = currentTick;

			//Update BarHandlers for all online players every 500 milliseconds (1 tick)
			gameLevel.DoForAllPlayers(player => player.BarHandler?.DoTick());
		}

		public override GameState GetNextGameState(GameLevel gameLevel)
		{
			return this; //No state changing
		}

		public override StateType GetEnumState(GameLevel gameLevel)
		{
			return StateType.PreGame; //Allow all joins
		}

		public override bool DoInteract(GameLevel gameLevel, int interactId, SkyPlayer player, SkyPlayer target)
		{
			SkyUtil.log($"Handling Hub Interacting from {player.Username} ID:{interactId}");
			if (player.Inventory.GetItemInHand() is ItemNavigationCompass)
			{
				GameUtil.ShowGameList(player);
			}

			return false;
		}
	}
}
