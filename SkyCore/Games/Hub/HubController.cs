using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Policy;
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

	public class HubController : GameController
	{
		public const int MaxHubCount = 5;
		
		private readonly HubWorldRandomiser _hubWorldRandomiser = new HubWorldRandomiser();

		public int NextGameId;

		public HubController(SkyCoreAPI plugin) : base(plugin, "hub", "Hub", new List<string>{"hub"})
		{
			Tick = 1;
			
			//Register all hubs
			for (int i = 0; i < MaxHubCount; i++)
			{
				InitializeNewGame();
			}
		}

		public override string GetNextGameId()
		{
			return $"{RawName}{++NextGameId}";
		}

		protected override GameLevel _initializeNewGame()
		{
			string selelectedLevel = GetRandomLevelName();

			return new HubLevel(Plugin, GetNextGameId(), selelectedLevel, GetGameLevelInfo(selelectedLevel));
		}

		protected override GameLevel _initializeNewGame(string levelName)
		{
			return new HubLevel(Plugin, GetNextGameId(), levelName, GetGameLevelInfo(levelName));
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
