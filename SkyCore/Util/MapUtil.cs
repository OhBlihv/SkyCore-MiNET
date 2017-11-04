using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Threading.Tasks;
using MiNET;
using MiNET.BlockEntities;
using MiNET.Blocks;
using MiNET.Entities.ImageProviders;
using MiNET.Entities.World;
using MiNET.Items;
using MiNET.Net;
using MiNET.Utils;
using MiNET.Worlds;

namespace SkyCore.Util
{
	public class MapUtil
	{

		/**
		 * Credit to @gurun, as what is below is based on his work.
		 */
		public static void SpawnMapImage(string imageLocation, int width, int height, Level level, BlockCoordinates spawnLocation)
		{
			Task.Run(delegate
			{
				try
				{
					if (!File.Exists(imageLocation))
					{
						return;
					}

					Bitmap image = new Bitmap((Bitmap)Image.FromFile(imageLocation), width * 128, height * 128);

					BlockCoordinates center = spawnLocation;

					for (int x = 0; x < width; x++)
					{
						int xSpawnLoc = center.X + x;
						for (int y = 0; y < height; y++)
						{
							var croppedImage = CropImage(image, new Rectangle(new Point(x * 128, y * 128), new Size(128, 128)));
							byte[] bitmapToBytes = BitmapToBytes(croppedImage, true);

							if (bitmapToBytes.Length != 128 * 128 * 4)
							{
								return;
							}

							MapEntity frame = new MapEntity(level);
							frame.ImageProvider = new MapImageProvider { Batch = CreateCachedPacket(frame.EntityId, bitmapToBytes) };
							frame.SpawnEntity();

							BlockCoordinates frambc = new BlockCoordinates(xSpawnLoc, center.Y + height - y - 2, center.Z);
							ItemFrameBlockEntity itemFrameBlockEntity = new ItemFrameBlockEntity
							{
								Coordinates = frambc
							};

							var itemFrame = new CustomItemFrame(frame, itemFrameBlockEntity, level)
							{
								Coordinates = frambc,
								Metadata = 2,
								BlockLight = 15
							};
							level.SetBlock(itemFrame);
							level.SetBlockEntity(itemFrameBlockEntity);
						}
					}
				}
				catch (Exception e)
				{
					SkyUtil.log("Aborted image generation");
					Console.WriteLine(e);
				}
			});
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

	public class CustomItemItemFrame : ItemItemFrame
	{

		private readonly MapEntity _frame;

		public CustomItemItemFrame(MapEntity frame)
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

			CustomItemFrame itemFrame = new CustomItemFrame(_frame, itemFrameBlockEntity, world)
			{
				Coordinates = coor,
			};

			if (!itemFrame.CanPlace(world, blockCoordinates, face)) return;

			itemFrame.PlaceBlock(world, player, coor, face, faceCoords);

			// Then we create and set the block entity that has all the intersting data

			world.SetBlockEntity(itemFrameBlockEntity);
		}
	}

	public class CustomItemFrame : ItemFrame
	{
		public CustomItemFrame(MapEntity frame, ItemFrameBlockEntity itemFrameBlockEntity, Level level)
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
