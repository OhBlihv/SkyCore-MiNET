using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using MiNET.Effects;
using MiNET.Utils;
using SkyCore.Entities;
using SkyCore.Game;
using SkyCore.Game.Level;
using SkyCore.Game.State;
using SkyCore.Game.State.Impl;
using SkyCore.Games.Hub.State;
using SkyCore.Player;

namespace SkyCore.Games.Hub
{
	public class HubLevel : GameLevel
	{
		
		public readonly ISet<string> CurrentlySpawnedNPCs = new HashSet<string>();
	
		public HubLevel(SkyCoreAPI plugin, string gameId, string levelPath, bool modifiable = false) : base(plugin, "hub", gameId, levelPath, modifiable)
		{
			//level.SpawnPoint = new PlayerLocation(0D, 36D, 10D, 0f, 0f, 90f);
			SpawnPoint = new PlayerLocation(256.5, 80, 264);
			GameLevelInfo.LobbyLocation = new PlayerLocation(256.5, 80, 264);

			SkyCoreAPI instance = SkyCoreAPI.Instance;

			BlockBreak += instance.LevelOnBlockBreak;
			BlockPlace += instance.LevelOnBlockPlace;

			CurrentWorldTime = 22000; //Sunrise?
			SkyUtil.log($"Set world time to {CurrentWorldTime}");

			instance.AddPendingTask(() =>
			{
				{
					PlayerLocation portalInfoLocation = new PlayerLocation(256.5, 79.5, 276.5);

					string hologramContent =
						"  §d§lSkytonia§r §f§lNetwork§r" + "\n" +
						" §7Enter the portal and§r" + "\n" +
						"§7enjoy your adventure!§r" + "\n" +
						"     §ewww.skytonia.com§r";

					Hologram portalInfoHologram = new Hologram(hologramContent, this, portalInfoLocation);

					portalInfoHologram.SpawnEntity();
				}
			});
		}

		protected override void InitializeTeamMap()
		{
			TeamPlayerDict.Add(HubTeam.Player, new List<SkyPlayer>());
			TeamPlayerDict.Add(HubTeam.Spectator, new List<SkyPlayer>());
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
			if (tick % 10 == 0 && PlayerCount > 0)
			{
				PlayerNPC.SpawnAllHubNPCs(this);
			}
		}

		public override Type GetGameLevelInfoType()
		{
			return typeof(GameLevelInfo);
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
