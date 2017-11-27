using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using MiNET;
using MiNET.BlockEntities;
using MiNET.Blocks;
using MiNET.Entities;
using MiNET.Entities.ImageProviders;
using MiNET.Entities.World;
using MiNET.Items;
using MiNET.Net;
using MiNET.Utils;
using MiNET.Worlds;
using SkyCore.BugSnag;
using SkyCore.Game.Level;

namespace SkyCore.Util
{

	class CachedMap
	{
		
		public Bitmap CachedImage { get; }

		public IDictionary<Tuple<int, int>, byte[]> CachedBitmaps { get; }

		public bool IsBuilding { get; set; } = true;

		public CachedMap(Bitmap cachedImage) : this(cachedImage, null)
		{
			CachedBitmaps = new Dictionary<Tuple<int, int>, byte[]>();
		}

		public CachedMap(Bitmap cachedImage, IDictionary<Tuple<int, int>, byte[]> cachedBitmaps)
		{
			CachedImage = cachedImage;
			CachedBitmaps = cachedBitmaps;
		}

	}

	public class MapUtil
	{

		public enum MapDirection
		{
			
			North = 0,
			East = 1,
			South = 2,
			West = 3

		}

		private static readonly ConcurrentDictionary<string, CachedMap> CachedMaps = new ConcurrentDictionary<string, CachedMap>();

		private static readonly ConcurrentDictionary<string, HashSet<long>> LevelMapIds = new ConcurrentDictionary<string, HashSet<long>>();

		public static ISet<long> GetLevelMapIds(Level level)
		{
			if (LevelMapIds.TryGetValue(level is GameLevel gameLevel ? gameLevel.GameId : level.LevelId, out var mapIds))
			{
				return mapIds;
			}

			return null;
		}

		/**
		 * Credit to @gurun, as what is below is based on his work.
		 */
		public static List<Entity> SpawnMapImage(string imageLocation, int width, int height, Level level, BlockCoordinates spawnLocation, MapDirection mapDirection = MapDirection.South)
		{
			var spawnedEntities = new List<Entity>();
			try
			{
				Bitmap image;
				CachedMap cachedMap;

				if (CachedMaps.ContainsKey(imageLocation))
				{
					cachedMap = CachedMaps[imageLocation];
					image = cachedMap.CachedImage;

					//Dodgily ensure the building flag is disabled
					cachedMap.IsBuilding = false;
					//SkyUtil.log($"Using Cached Image for {imageLocation}");
				}
				else
				{
					if (!File.Exists(imageLocation))
					{
						SkyUtil.log($"File doesn't exist for Map ({imageLocation})");
						return spawnedEntities;
					}

					image = new Bitmap((Bitmap)Image.FromFile(imageLocation), width * 128, height * 128);
					cachedMap = new CachedMap(image);

					//SkyUtil.log($"Loading Image for {imageLocation}");
				}

				BlockCoordinates center = spawnLocation;

				for (int x = 0; x < width; x++)
				{
					int xSpawnLoc = center.X + x;
					for (int y = 0; y < height; y++)
					{
						byte[] bitmapToBytes;
						if (cachedMap.IsBuilding)
						{
							var croppedImage = CropImage(image, new Rectangle(new Point(x * 128, y * 128), new Size(128, 128)));
							bitmapToBytes = BitmapToBytes(croppedImage, true);

							if (bitmapToBytes.Length != 128 * 128 * 4)
							{
								return spawnedEntities; //TODO: Throw Exception/Alert Log?
							}

							cachedMap.CachedBitmaps.Add(new Tuple<int, int>(x, y), bitmapToBytes);
						}
						else
						{
							bitmapToBytes = cachedMap.CachedBitmaps[new Tuple<int, int>(x, y)];
						}

						MapEntity frame = new MapEntity(level);
						frame.ImageProvider = new MapImageProvider { Batch = CreateCachedPacket(frame.EntityId, bitmapToBytes) };
						frame.SpawnEntity();

						AddMapIdToDictionary(level is GameLevel gameLevel ? gameLevel.GameId : level.LevelId, frame.EntityId);

						BlockCoordinates frambc = new BlockCoordinates(xSpawnLoc, center.Y + height - y - 2, center.Z);
						ItemFrameBlockEntity itemFrameBlockEntity = new ItemFrameBlockEntity
						{
							Coordinates = frambc
						};

						var itemFrame = new FullyLuminousItemFrame(frame, itemFrameBlockEntity, level)
						{
							Coordinates = frambc,
							Metadata = (byte)mapDirection,
						};
						level.SetBlock(itemFrame, true, false);
						level.SetBlockEntity(itemFrameBlockEntity);

						spawnedEntities.Add(frame);
					}
				}

				if (cachedMap.IsBuilding)
				{
					CachedMaps.TryAdd(imageLocation, cachedMap);
					cachedMap.IsBuilding = false; //Completely Cached
				}
			}
			catch (Exception e)
			{
				SkyUtil.log("Aborted image generation");
				BugSnagUtil.ReportBug(e);
			}

			return spawnedEntities;
		}

		private static void AddMapIdToDictionary(string levelId, long mapId)
		{
			LevelMapIds.GetOrAdd(levelId, new HashSet<long>()).Add(mapId);
		}

		private static McpeWrapper CreateCachedPacket(long mapId, byte[] bitmapToBytes)
		{
			MapInfo mapInfo = new MapInfo
			{
				MapId = mapId,
				UpdateType = 2,
				Scale = 0,
				X = 0,
				Z = 0,
				Col = 128,
				Row = 128,
				XOffset = 0,
				ZOffset = 0,
				Data = bitmapToBytes,
			};

			McpeClientboundMapItemData packet = McpeClientboundMapItemData.CreateObject();
			packet.mapinfo = mapInfo;
			var batch = CreateMcpeBatch(packet.Encode());

			return batch;
		}

		internal static McpeWrapper CreateMcpeBatch(byte[] bytes)
		{
			McpeWrapper batch = BatchUtils.CreateBatchPacket(bytes, 0, bytes.Length, CompressionLevel.Optimal, true);
			batch.MarkPermanent();
			batch.Encode();
			return batch;
		}

		public static Bitmap CropImage(Bitmap img, Rectangle cropArea)
		{
			return img.Clone(cropArea, img.PixelFormat);
		}

		public static byte[] BitmapToBytes(Bitmap bitmap, bool useColor = false)
		{
			byte[] bytes = new byte[bitmap.Height * bitmap.Width * 4];

			int i = 0;
			for (int y = 0; y < bitmap.Height; y++)
			{
				for (int x = 0; x < bitmap.Width; x++)
				{
					Color color = bitmap.GetPixel(x, y);
					bytes[i++] = color.R;
					bytes[i++] = color.G;
					bytes[i++] = color.B;
					bytes[i++] = 0xff;
				}
			}
			return bytes;
		}

	}

	public class FullyLuminousItemFrameItem : ItemItemFrame
	{

		private readonly MapEntity _frame;

		public FullyLuminousItemFrameItem(MapEntity frame)
		{
			_frame = frame;
		}

		public override void PlaceBlock(Level world, MiNET.Player player, BlockCoordinates blockCoordinates, BlockFace face, Vector3 faceCoords)
		{
			var coor = GetNewCoordinatesFromFace(blockCoordinates, face);

			ItemFrameBlockEntity itemFrameBlockEntity = new ItemFrameBlockEntity
			{
				Coordinates = coor
			};

			FullyLuminousItemFrame itemFrame = new FullyLuminousItemFrame(_frame, itemFrameBlockEntity, world)
			{
				Coordinates = coor,
			};

			if (!itemFrame.CanPlace(world, blockCoordinates, face)) return;

			itemFrame.PlaceBlock(world, player, coor, face, faceCoords);

			// Then we create and set the block entity that has all the intersting data

			world.SetBlockEntity(itemFrameBlockEntity);
		}

	}

	public class FullyLuminousItemFrame : ItemFrame
	{

		public FullyLuminousItemFrame()
		{
			IsTransparent = true;
			LightLevel = 15; //Full Bright
		}

		public FullyLuminousItemFrame(MapEntity frame, ItemFrameBlockEntity itemFrameBlockEntity, Level level) : this()
		{
			ItemMap map = new ItemMap(frame.EntityId);

			ItemFrameBlockEntity blockEntity = itemFrameBlockEntity;
			if (blockEntity != null)
			{
				blockEntity.SetItem(map);
				level.SetBlockEntity(blockEntity);
			}
		}

		public override bool PlaceBlock(Level world, MiNET.Player player, BlockCoordinates blockCoordinates, BlockFace face,
			Vector3 faceCoords)
		{
			switch (face)
			{
				case BlockFace.South: // ok
					Metadata = 0;
					break;
				case BlockFace.North:
					Metadata = 1;
					break;
				case BlockFace.West:
					Metadata = 2;
					break;
				case BlockFace.East: // ok
					Metadata = 3;
					break;
			}

			world.SetBlock(this);

			return true;
		}

	}

}
