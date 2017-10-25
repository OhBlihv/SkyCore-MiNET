﻿using System;
using System.Collections.Generic;
using System.Numerics;
using MiNET.Net;
using MiNET.Particles;
using MiNET.Utils;
using MiNET.Worlds;
using SkyCore.Game;
using SkyCore.Game.Level;
using SkyCore.Games.Hub.Items;
using SkyCore.Games.Murder;
using SkyCore.Player;

namespace SkyCore.Games.Hub
{
	
	class HubWorldRandomiser : IComparer<GameLevel>
	{

		private static readonly Random Random = new Random();

		public int Compare(GameLevel x, GameLevel y)
		{
			int rolledNumber = Random.Next(2);
			if (rolledNumber == 0)
			{
				return 1;
			}

			return (Random.Next(2) == 0 ? 1 : -1) * rolledNumber;
		}
	}

	public class HubCoreController : CoreGameController
	{
		public const int MaxHubCount = 5;
		
		private readonly HubWorldRandomiser _hubWorldRandomiser = new HubWorldRandomiser();

		public HubCoreController(SkyCoreAPI plugin) : base(plugin, "hub", "Hub", new List<string>{"hub"})
		{
			Tick = 1;
			
			ExternalGameHandler.RegisterGameIntent("murder");
			ExternalGameHandler.RegisterGameIntent("build-battle");
			ExternalGameHandler.RegisterGameIntent("block-hunt");
			ExternalGameHandler.RegisterGameIntent("bed-wars");
			
			//Register all hubs
			for (int i = 0; i < MaxHubCount; i++)
			{
				GetGameController();
			}
		}

		protected override GameLevel _getGameController()
		{
			return new HubLevel(Plugin, GetNextGameId(), GetRandomLevelName());
		}

		public override SortedSet<GameLevel> GetMostViableGames()
		{
			SortedSet<GameLevel> mostViableGames = new SortedSet<GameLevel>(_hubWorldRandomiser);

			foreach (GameLevel gameLevel in GameLevels.Values)
			{
				if (gameLevel.CurrentState.GetEnumState(gameLevel).IsJoinable())
				{
					mostViableGames.Add(gameLevel);
				}
			}

			return mostViableGames;
		}

		public override void CheckCapacity()
		{
			//Ignored. Static Hub Count
		}

		public override Type GetGameLevelInfoType()
		{
			return typeof(GameLevelInfo); //Nothing Custom
		}

	}
}
