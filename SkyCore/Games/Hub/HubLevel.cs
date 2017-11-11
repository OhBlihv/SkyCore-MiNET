using System;
using System.Collections.Generic;
using log4net;
using MiNET.Effects;
using MiNET.Entities;
using MiNET.Net;
using MiNET.Utils;
using SkyCore.Entities;
using SkyCore.Game.Level;
using SkyCore.Game.State;
using SkyCore.Games.Hub.State;
using SkyCore.Player;
using SkyCore.Util;
using Hologram = SkyCore.Entities.Hologram;

namespace SkyCore.Games.Hub
{
	public class HubLevel : GameLevel
	{

		private static readonly ILog Log = LogManager.GetLogger(typeof(HubLevel));

		public readonly ISet<string> CurrentlySpawnedNPCs = new HashSet<string>();
	
		public HubLevel(SkyCoreAPI plugin, string gameId, string levelPath, GameLevelInfo gameLevelInfo, bool modifiable = false) : 
			base(plugin, "hub", gameId, levelPath, gameLevelInfo, modifiable)
		{
			AddPendingTask(() =>
			{
				PlayerLocation portalInfoLocation = new PlayerLocation(256.5, 79.5, 276.5);

				const string hologramContent = "  §d§lSkytonia§r §f§lNetwork§r" + "\n" +
				                               " §7Enter the portal and§r" + "\n" +
				                               "§7enjoy your adventure!§r" + "\n" +
				                               "     §ewww.skytonia.com§r";

				new Hologram(hologramContent, this, portalInfoLocation).SpawnEntity();

				RunnableTask.RunTaskLater(() =>
				{
					try
					{
						SkyUtil.log("Spawning all NPCs for " + GameId);
						PlayerNPC.SpawnAllHubNPCs(this);
					}
					catch (Exception e)
					{
						Console.WriteLine(e);
					}
				}, 250);
				
			});
		}

		protected override void SetupWorldTime()
		{
			WorldTime = 22000; //Sunrise?
			SkyUtil.log($"Set world time to {WorldTime}");
			DoDaylightcycle = false; //Freeze Time
		}

		protected override void InitializeTeamMap()
		{
			TeamPlayerDict.Add(HubTeam.Player, new List<SkyPlayer>());
			TeamPlayerDict.Add(HubTeam.Spectator, new List<SkyPlayer>());
		}
		
		public override void AddPlayer(MiNET.Player player, bool spawn)
		{
			AddSpectator(player as SkyPlayer);

			base.AddPlayer(player, spawn);
		}

		public override void RemovePlayer(MiNET.Player player, bool removeFromWorld = false)
		{
			SkyUtil.log($"Attempting to remove {player.Username} from {GameId}");
			if (((SkyPlayer)player).GameTeam == null)
			{
				return; //Shouldn't be in the/any game.
			}

			CurrentState.HandleLeave(this, (SkyPlayer)player);

			PlayerTeamDict.TryGetValue(player.Username, out var gameTeam);

			if (gameTeam != null)
			{
				PlayerTeamDict.Remove(player.Username);
				TeamPlayerDict[gameTeam].Remove((SkyPlayer)player);
			}

			//Enforce removing the attached team
			((SkyPlayer)player).GameTeam = null;
		}

		public override void SetPlayerTeam(SkyPlayer player, GameTeam oldTeam, GameTeam team)
		{
			if (oldTeam != null)
			{
				TeamPlayerDict[oldTeam].Remove(player);
			}

			if (team != null)
			{
				TeamPlayerDict[team].Add(player);

				if (team.IsSpectator)
				{
					AddSpectator(player);
				}
				else
				{
					player.IsGameSpectator = false;

					//Re-update visible characteristics
					player.RemoveEffect(new Invisibility());
					player.BroadcastSetEntityData();
					player.SetNameTagVisibility(true);
					player.Inventory.SetHeldItemSlot(player.Inventory.InHandSlot, false);
				}
			}
		}

		public override GameState GetInitialState()
		{
			return new HubState();
		}

		public override GameTeam GetDefaultTeam()
		{
			return HubTeam.Spectator; //Default is spectator, until you move from the spawn position
		}

		public override GameTeam GetSpectatorTeam()
		{
			return HubTeam.Spectator;
		}

		public override void AddSpectator(SkyPlayer player)
		{
			player.IsGameSpectator = true;

			//Set an invisibily effect on top of the scale to completely 'remove' the player
			player.SetEffect(new Invisibility { Duration = int.MaxValue, Particles = false });

			player.SetNameTagVisibility(false);

			McpeSetEntityData mcpeSetEntityData = McpeSetEntityData.CreateObject();
			mcpeSetEntityData.runtimeEntityId = player.EntityId;
			mcpeSetEntityData.metadata = player.GetMetadata();
			mcpeSetEntityData.metadata[(int) Entity.MetadataFlags.Scale] = new MetadataFloat(0.5f); // Scale

			//Avoid changing the local player's scale
			foreach (SkyPlayer gamePlayer in GetAllPlayers())
			{
				if (gamePlayer == player)
				{
					continue;
				}

				gamePlayer.SendPackage(mcpeSetEntityData);
			}

			//Update slot held for other players
			player.Inventory.SetHeldItemSlot(player.Inventory.InHandSlot, false);
		}

		public override int GetMaxPlayers()
		{
			return 100;
		}

		public override void GameTick(int tick)
		{
			
		}

		public override string GetEndOfGameContent(SkyPlayer player)
		{
			return "";
		}

		public override string GetGameModalTitle()
		{
			return "";
		}
	}
}
