﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using MiNET.Effects;
using MiNET.Net;
using MiNET.Utils;
using SkyCore.Entities;
using SkyCore.Game;
using SkyCore.Game.Level;
using SkyCore.Game.State;
using SkyCore.Game.State.Impl;
using SkyCore.Games.Hub.State;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Games.Hub
{
	public class HubLevel : GameLevel
	{
		
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

			List<MiNET.Player> gamePlayers = new List<MiNET.Player>();
			DoForAllPlayers(gamePlayer =>
			{
				if (!gamePlayer.IsGameSpectator)
				{
					gamePlayers.Add(gamePlayer);
				}
			});

			SkyUtil.log($"Despawning {player.Username} from {string.Join(",", gamePlayers.Select(x => x.ToString()).ToArray())}");
			player.DespawnFromPlayers(gamePlayers.ToArray());
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
