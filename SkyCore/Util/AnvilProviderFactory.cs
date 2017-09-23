using System.Collections.Generic;
using MiNET;
using MiNET.Utils;
using MiNET.Worlds;

namespace SkyCore.Util
{
    public class AnvilProviderFactory
	{
		private static readonly Dictionary<string, AnvilWorldProvider> ProviderCache = new Dictionary<string, AnvilWorldProvider>();

		public static AnvilWorldProvider GetLevelProvider(LevelManager levelManager, string levelDir, bool readOnly = true, bool insulate = true, bool cache = true)
		{
			AnvilWorldProvider provider;
			ProviderCache.TryGetValue(levelDir, out provider);

			if (provider != null) return readOnly ? provider : (AnvilWorldProvider)provider.Clone();

			provider = new AnvilWorldProvider(levelDir);

			var level = new CacheLevel(levelManager, "cache", provider, new EntityManager(), viewDistance: 20)
			{
				EnableBlockTicking = false,
				EnableChunkTicking = false
			};
			
			level.Initialize();

			RecalculateLight(level, provider);

			level.Close();

			provider.PruneAir();
			provider.MakeAirChunksAroundWorldToCompensateForBadRendering();

			if (insulate) InsulateChunks(provider);
			if (cache) ProviderCache.Add(levelDir, provider);

			return readOnly ? provider : (AnvilWorldProvider)provider.Clone();
		}

		private static void InsulateChunks(AnvilWorldProvider provider, int radius = 3)
		{
			var spawn = new ChunkCoordinates(provider.GetSpawnPoint());

			for (var x = -radius; x < radius; x++)
			{
				for (var z = -radius; z < radius; z++)
				{
					ChunkColumn column;
					var location = new ChunkCoordinates(spawn.X + x, spawn.Z + z);

					provider._chunkCache.TryGetValue(location, out column);

					if (column != null) continue;

					column = new ChunkColumn
					{
						isAllAir = true,
						x = location.X,
						z = location.Z
					};

					column.GetBatch();

					provider._chunkCache[location] = column;
				}
			}
		}

		private static void RecalculateLight(Level level, AnvilWorldProvider anvilWorldProvider)
		{
			SkyLightCalculations.Calculate(level);

			while (anvilWorldProvider.LightSources.Count > 0)
			{
				var block = anvilWorldProvider.LightSources.Dequeue();
				BlockLightCalculations.Calculate(level, block.Coordinates);
			}
		}
	}

	public class CacheLevel : Level
	{
		public CacheLevel(LevelManager levelManager, string levelId, IWorldProvider worldProvider, EntityManager entityManager, GameMode gameMode = GameMode.Survival, Difficulty difficulty = Difficulty.Normal, int viewDistance = 11) : base(levelManager, levelId, worldProvider, entityManager, gameMode, difficulty, viewDistance)
		{
		}

		public override void Close()
		{
			WorldProvider = null;

			base.Close();
		}
	}
}