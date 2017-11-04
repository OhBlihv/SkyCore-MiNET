using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Threading.Tasks;
using MiNET;
using MiNET.BlockEntities;
using MiNET.Blocks;
using MiNET.Entities;
using MiNET.Entities.Hostile;
using MiNET.Entities.ImageProviders;
using MiNET.Entities.Passive;
using MiNET.Entities.World;
using MiNET.Items;
using MiNET.Net;
using MiNET.Plugins.Attributes;
using MiNET.Utils;
using MiNET.Worlds;
using SkyCore.Game;
using SkyCore.Game.Level;
using SkyCore.Permissions;
using SkyCore.Player;
using System.Threading;
using log4net;

namespace SkyCore.Commands
{
	public class SkyCommands
	{

		public static SkyCommands Instance { get; private set; }

		private readonly SkyCoreAPI _skyCoreApi;

		public SkyCommands(SkyCoreAPI skyCoreApi)
		{
			Instance = this;

			_skyCoreApi = skyCoreApi;
		}

		[Command(Name = "findworld")]
		[Authorize(Permission = CommandPermission.Normal)]
		public void CommandGiveIronIngot(MiNET.Player player)
		{
			player.SendMessage($"In Game World {((GameLevel) player.Level).GameId}");
		}

		[Command(Name = "hub")]
		[Authorize(Permission = CommandPermission.Normal)]
		public void CommandHub(MiNET.Player player, int hub = 0)
		{
			MoveToLobby(player, hub);
		}

		[Command(Name = "lobby")]
		[Authorize(Permission = CommandPermission.Normal)]
		public void CommandLobby(MiNET.Player player, int hub = 0)
		{
			MoveToLobby(player, hub);
		}

		public void MoveToLobby(MiNET.Player player, int hub = 0)
		{
			if (_skyCoreApi.GameType.Equals("hub"))
			{
				if (hub == 0)
				{
					player.SendMessage("§c§l(!)§r §cYou are already connected to a hub.");
					return;
				}

				string hubNum = hub.ToString();

				CoreGameController gameController = _skyCoreApi.GameModes["hub"];
				foreach (GameLevel hubLevel in gameController.GameLevels.Values)
				{
					if (hubLevel.GameId.Replace("hub", "").Equals(hubNum))
					{
						hubLevel.AddPlayer((SkyPlayer) player);
						return;
					}
				}

				player.SendMessage($"§c§l(!)§r §cHub{hubNum} does not exist.");
				return;
			}

			player.SendMessage("§e§l(!)§r §eMoving to Hub...");

			ExternalGameHandler.AddPlayer((SkyPlayer) player, "hub");
		}

		[Command(Name = "popuptest")]
		[Authorize(Permission = CommandPermission.Normal)]
		public void CommandPopupTest(MiNET.Player player, string popup, string actionbar)
		{
			if (player.CommandPermission < CommandPermission.Admin)
			{
				player.SendMessage("§c§l(!)§r §cYou do not have permission for this command.");
				return;
			}

			SkyPlayer skyPlayer = (SkyPlayer) player;

			skyPlayer.BarHandler.AddMajorLine(popup);
			skyPlayer.BarHandler.AddMinorLine(actionbar);
		}

		[Command(Name = "subtitletest")]
		[Authorize(Permission = CommandPermission.Normal)]
		public void CommandSubtitle(MiNET.Player player, int lineCount)
		{
			if (player.CommandPermission < CommandPermission.Admin)
			{
				player.SendMessage("§c§l(!)§r §cYou do not have permission for this command.");
				return;
			}

			string lines = "";
			for (int i = 0; i < lineCount; i++)
			{
				lines += $"{i}\n";
			}

			player.SendTitle(lines, TitleType.SubTitle);
			//player.SendTitle("1\n2\n3\n4\n5", TitleType.SubTitle);
			player.SendTitle(" ");
		}

		[Command(Name = "actionbartest")]
		[Authorize(Permission = CommandPermission.Normal)]
		public void CommandActionBar(MiNET.Player player, int lineCount)
		{
			if (player.CommandPermission < CommandPermission.Admin)
			{
				player.SendMessage("§c§l(!)§r §cYou do not have permission for this command.");
				return;
			}

			string lines = "";
			for (int i = 0; i < lineCount; i++)
			{
				lines += $"{i}\n";
			}

			player.SendTitle(lines, TitleType.ActionBar);
			//player.SendTitle("1\n2\n3\n4\n5", TitleType.ActionBar);
			player.SendTitle(" ");
		}

		[Command(Name = "titletest")]
		[Authorize(Permission = CommandPermission.Normal)]
		public void CommandTitle(MiNET.Player player, int lineCount)
		{
			if (player.CommandPermission < CommandPermission.Admin)
			{
				player.SendMessage("§c§l(!)§r §cYou do not have permission for this command.");
				return;
			}

			//player.SendTitle("1\n2\n3\n4\n5\n6\n7\n8\n9\n10", TitleType.SubTitle);
			string lines = "";
			for (int i = 0; i < lineCount; i++)
			{
				lines += $"{i}\n";
			}

			player.SendTitle(lines, TitleType.Title);
			//player.SendTitle(" ");
		}

		[Command(Name = "speed")]
		[Authorize(Permission = CommandPermission.Normal)]
		public void CommandSpeed(MiNET.Player player, float speed = 0.1f)
		{
			if (player.CommandPermission < CommandPermission.Admin)
			{
				player.SendMessage("§c§l(!)§r §cYou do not have permission for this command.");
				return;
			}

			player.MovementSpeed = speed;
			player.SendAdventureSettings();

			player.SendMessage($"§eUpdated movement speed to {speed}");
		}

		[Command(Name = "time")]
		[Authorize(Permission = CommandPermission.Normal)]
		public void CommandTime(MiNET.Player player, string timeString)
		{
			if (player.CommandPermission < CommandPermission.Admin)
			{
				player.SendMessage("§c§l(!)§r §cYou do not have permission for this command.");
				return;
			}

			int time = 0;
			switch (timeString.ToLower())
			{
				case "day":
					time = 6000;
					break;
				case "night":
					time = 12000;
					break;
				default:
					player.SendMessage("Unknown time string.");
					return;
			}

			//player.Level.CurrentWorldTime = time;
			player.Level.WorldTime = time;
		}

		[Command(Name = "stop")]
		[Authorize(Permission = CommandPermission.Admin)]
		public void CommandStop(MiNET.Player player)
		{
			if (((SkyPlayer) player).PlayerGroup != PlayerGroup.Admin)
			{
				return;
			}

			SkyCoreAPI.Instance.OnDisable();
			SkyCoreAPI.Instance.Server.PluginManager.Plugins.Remove(SkyCoreAPI.Instance);
			SkyCoreAPI.Instance.Server.StopServer();

			Environment.Exit(0);
		}

		[Command(Name = "gamemode", Aliases = new[] {"gm"})]
		[Authorize(Permission = CommandPermission.Normal)]
		public void CommandGamemode(MiNET.Player player, int gamemodeId = 0)
		{
			CommandGamemode(player, player.Username, gamemodeId);
		}

		[Command(Name = "gamemode", Aliases = new[] {"gm"})]
		[Authorize(Permission = CommandPermission.Normal)]
		public void CommandGamemode(MiNET.Player player, string targetName = "", int gamemodeId = 0)
		{
			if (player.CommandPermission < CommandPermission.Admin)
			{
				player.SendMessage("§c§l(!)§r §cYou do not have permission for this command.");
				return;
			}

			MiNET.Player target;
			if (String.IsNullOrEmpty(targetName))
			{
				player.SendMessage($"{ChatColors.Red}Enter a valid player name.");
				return;
			}

			target = _skyCoreApi.GetPlayer(targetName);

			if (target == null || !target.IsConnected)
			{
				player.SendMessage($"{ChatColors.Red}Target player is not online.");
				return;
			}

			GameMode gamemode;
			switch (gamemodeId)
			{
				case 0:
					gamemode = GameMode.Survival;
					break;
				case 1:
					gamemode = GameMode.Creative;
					break;
				case 2:
					gamemode = GameMode.Adventure;
					break;
				case 3:
					gamemode = GameMode.Spectator;
					//Avoid having regular players noclipping
					if (((SkyPlayer) target).PlayerGroup.IsAtLeast(PlayerGroup.Youtuber))
					{
						target.IsNoClip = true;
					}
					else
					{
						target.IsNoClip = true;
					}
					break;
				default:
				{
					player.SendMessage($"{ChatColors.Red}Invalid gamemode id {gamemodeId}.");
					return;
				}
			}

			target.SetGameMode(gamemode);

			player.SendMessage($"{ChatColors.Yellow}Updated {target.Username}'s Gamemode to {gamemode}");
		}

		[Command(Name = "fly")]
		[Authorize(Permission = CommandPermission.Admin)]
		public void CommandFly(MiNET.Player player, string targetName = "")
		{
			if (player.CommandPermission < PlayerGroup.Mvp.PermissionLevel)
			{
				player.SendMessage("§c§l(!)§r §cYou do not have permission for this command.");
				return;
			}

			MiNET.Player targetPlayer;
			if (String.IsNullOrEmpty(targetName))
			{
				targetPlayer = player;
			}
			else
			{
				targetPlayer = _skyCoreApi.GetPlayer(targetName);
				if (targetPlayer == null)
				{
					player.SendMessage($"§c§l(!) §r§c{targetName} is not online.");
					return;
				}
			}

			if (targetPlayer.GameMode == GameMode.Creative || targetPlayer.GameMode == GameMode.Spectator)
			{
				if (player.Username.Equals(targetPlayer.Username))
				{
					player.SendMessage($"§c§l(!) §r§cFlight cannot be toggled in {targetPlayer.GameMode}");
				}
				else
				{
					player.SendMessage(
						$"§c§l(!) §r§cFlight cannot be toggled for {targetPlayer.Username} since they are in {targetPlayer.GameMode} mode.");
				}
				return;
			}

			if (targetPlayer.AllowFly)
			{
				targetPlayer.IsFlying = false;
				targetPlayer.SetAllowFly(false);

				player.SendMessage($"§e§l(!) §r§eFlight §c§lDISABLED §r§efor {targetPlayer.Username}");
			}
			else
			{
				targetPlayer.SetAllowFly(true);
				player.SendMessage($"§e§l(!) §r§eFlight §a§lENABLED §r§efor {targetPlayer.Username}");
			}
		}

		/*[Command(Name = "hologram")]
		[Authorize(Permission = CommandPermission.Admin)]
		public void CommandHologram(MiNET.Player player, string hologramText)
		{
		    try
		    {
		        if (String.IsNullOrEmpty(hologramText))
		        {
		            player.SendMessage("§c§l(!) §r§cInvalid hologram text. /hologram <text>");
		            return;
		        }

		        hologramText = hologramText.Replace("_", " ").Replace("&", "§");

		        string[] lineSplit = new string[] {hologramText};
		        //if (!hologramText.StartsWith("NULL"))
		        {
		            lineSplit = hologramText.Split(new[] {"\\n"}, StringSplitOptions.None);
		        }

		        string newLine = "";
		        foreach (string line in lineSplit)
		        {
		            newLine += line + "\n";
		        }

		        lineSplit = new string[] {newLine};

		        PlayerLocation currentLocation = (PlayerLocation) player.KnownPosition.Clone();
		        foreach (string line in lineSplit)
		        {
		            SkyUtil.log($"Processing Line: '{line}'");
		            player.Level.AddEntity(new Entities.Hologram(TextUtils.Center(line), player.Level,
		                currentLocation));
		            (currentLocation = (PlayerLocation) currentLocation.Clone()).Y -= 0.3f;
		        }

		        player.SendMessage($"§e§l(!) §r§eSpawned Hologram with text '{hologramText}§r'");
		    }
		    catch (Exception e)
		    {
		        Console.WriteLine(e);
		        throw;
		    }
		}*/

		[Command(Name = "join")]
		[Authorize(Permission = CommandPermission.Normal)]
		public void CommandJoin(MiNET.Player player, string gameName)
		{
			try
			{
				switch (gameName.ToLower())
				{
					case "murder":
					{
						ExternalGameHandler.AddPlayer((SkyPlayer) player, "murder");
						break;
					}
					case "build-battle":
					{
						ExternalGameHandler.AddPlayer((SkyPlayer) player, "build-battle");
						break;
					}
					case "hub":
					{
						ExternalGameHandler.AddPlayer((SkyPlayer) player, "hub");
						break;
					}
					default:
					{
						player.SendMessage($"{ChatColors.Red}Unable to resolve game '{gameName}'.");
						return;
					}
				}

				player.SendMessage($"{ChatColors.Yellow}Joining {gameName}...");
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				player.SendMessage("Unable to join " + gameName);
			}
		}

		/*[Command(Name = "npc")]
		[Authorize(Permission = CommandPermission.Admin)]
		public void CommandNPC(MiNET.Player executingPlayer, string npcName, string command = "")
		{
		    try
		    {
		        if (String.IsNullOrEmpty(npcName))
		        {
		            executingPlayer.SendMessage("§c§l(!) §r§cInvalid NPC text. /hologram <text>");
		            return;
		        }

		        npcName = npcName.Replace("_", " ").Replace("&", "§");

		        if (npcName.Equals("\"\""))
		        {
		            npcName = "";
		        }

		        PlayerNPC.onInteract action = null;
		        if (!String.IsNullOrEmpty(command))
		        {
		            if (command.StartsWith("GID:"))
		            {
		                action = player =>
		                {
		                    switch (command)
		                    {
		                        case "GID:murder":
		                            executingPlayer.SendMessage("Queueing for Murder");
		                            skyCoreApi.GameModes["murder"].QueuePlayer(player);
		                            break;
		                        default:
		                            executingPlayer.SendMessage($"Unable to process game command {command}");
		                            break;
		                    }
		                };
		            }
		            else
		            {
		                //TODO:
		            }
		        }

		        PlayerNPC npc = new PlayerNPC(npcName, executingPlayer.Level, executingPlayer.KnownPosition, action);

		        executingPlayer.Level.AddEntity(npc);

		        npc.Scale = 1.2; //Ensure this NPC can be seen

		        npc.BroadcastEntityEvent();
		        npc.BroadcastSetEntityData();

		        executingPlayer.SendMessage($"§e§l(!) §r§eSpawned NPC with text '{npcName}§r'");
		    }
		    catch (Exception e)
		    {
		        Console.WriteLine(e);
		        throw;
		    }
		}*/

		[Command(Name = "scale")]
		[Authorize(Permission = CommandPermission.Normal)]
		public void CommandScale(MiNET.Player player, string scaleString, string targetName = "")
		{
			if (player.CommandPermission < CommandPermission.Admin)
			{
				player.SendMessage("§c§l(!)§r §cYou do not have permission for this command.");
				return;
			}

			MiNET.Player targetPlayer;
			if (String.IsNullOrEmpty(targetName))
			{
				targetPlayer = player;
			}
			else
			{
				targetPlayer = _skyCoreApi.GetPlayer(targetName);
				if (targetPlayer == null)
				{
					player.SendMessage($"§c§l(!) §r§c{targetName} is not online.");
					return;
				}
			}

			try
			{
				targetPlayer.Scale = Double.Parse(scaleString);
				player.SendMessage($"§e§l(!) §r§eUpdated {targetPlayer.Username}'s scale to {targetPlayer.Scale}");
			}
			catch (Exception e)
			{
				player.SendMessage($"§c§l(!) §r§cUnable to parse scale {scaleString}");
				Console.WriteLine(e);
				throw;
			}

		}

		[Command(Name = "spawnmob")]
		[Authorize(Permission = CommandPermission.Normal)]
		public void CommandSpawnMob(MiNET.Player player, string entityName, string mobName = "", string mobScale = "")
		{
			entityName = entityName.ToLower();
			entityName = entityName.Substring(0, 1).ToUpper() + entityName.Substring(1);

			EntityType entityType;
			if (!EntityType.TryParse(entityName, out entityType))
			{
				player.SendMessage($"§c§l(!) §r§cUnknown EntityType '{entityName}'");
				return;
			}

			Level level = player.Level;

			Mob mob = null;
			switch (entityType)
			{
				case EntityType.Chicken:
					mob = new Chicken(level);
					break;
				case EntityType.Cow:
					mob = new Cow(level);
					break;
				case EntityType.Pig:
					mob = new Pig(level);
					break;
				case EntityType.Sheep:
					mob = new Sheep(level);
					break;
				case EntityType.Wolf:
					mob = new Wolf(level) {Owner = player};
					break;
				case EntityType.Villager:
					mob = new Villager(level);
					break;
				case EntityType.MushroomCow:
					mob = new MushroomCow(level);
					break;
				case EntityType.Squid:
					mob = new Squid(level);
					break;
				case EntityType.Rabbit:
					mob = new Rabbit(level);
					break;
				case EntityType.Bat:
					mob = new Bat(level);
					break;
				case EntityType.IronGolem:
					mob = new IronGolem(level);
					break;
				case EntityType.SnowGolem:
					mob = new SnowGolem(level);
					break;
				case EntityType.Ocelot:
					mob = new Ocelot(level);
					break;
				case EntityType.Zombie:
					mob = new Zombie(level);
					break;
				case EntityType.Creeper:
					mob = new Creeper(level);
					break;
				case EntityType.Skeleton:
					mob = new Skeleton(level);
					break;
				case EntityType.Spider:
					mob = new Spider(level);
					break;
				case EntityType.ZombiePigman:
					mob = new ZombiePigman(level);
					break;
				case EntityType.Slime:
					mob = new MiNET.Entities.Hostile.Slime(level);
					break;
				case EntityType.Enderman:
					mob = new Enderman(level);
					break;
				case EntityType.Silverfish:
					mob = new Silverfish(level);
					break;
				case EntityType.CaveSpider:
					mob = new CaveSpider(level);
					break;
				case EntityType.Ghast:
					mob = new Ghast(level);
					break;
				case EntityType.MagmaCube:
					mob = new MagmaCube(level);
					break;
				case EntityType.Blaze:
					mob = new Blaze(level);
					break;
				case EntityType.ZombieVillager:
					mob = new ZombieVillager(level);
					break;
				case EntityType.Witch:
					mob = new Witch(level);
					break;
				case EntityType.Stray:
					mob = new Stray(level);
					break;
				case EntityType.Husk:
					mob = new Husk(level);
					break;
				case EntityType.WitherSkeleton:
					mob = new WitherSkeleton(level);
					break;
				case EntityType.Guardian:
					mob = new Guardian(level);
					break;
				case EntityType.ElderGuardian:
					mob = new ElderGuardian(level);
					break;
				case EntityType.Horse:
					mob = new Horse(level);
					break;
				case EntityType.PolarBear:
					mob = new PolarBear(level);
					break;
				case EntityType.Shulker:
					mob = new Shulker(level);
					break;
				case EntityType.Dragon:
					mob = new Dragon(level);
					break;
				case EntityType.SkeletonHorse:
					mob = new SkeletonHorse(level);
					break;
				case EntityType.Wither:
					mob = new Wither(level);
					break;
				case EntityType.Evoker:
					mob = new Evoker(level);
					break;
				case EntityType.Vindicator:
					mob = new Vindicator(level);
					break;
				case EntityType.Vex:
					mob = new Vex(level);
					break;
				case EntityType.Npc:
					mob = new PlayerMob("test", level);
					break;
			}

			if (!String.IsNullOrEmpty(mobName))
			{
				mob.NameTag = mobName.Replace("&", "§");
				mob.HideNameTag = false;
				mob.IsAlwaysShowName = true;
			}

			if (!String.IsNullOrEmpty(mobScale))
			{
				try
				{
					mob.Scale = Double.Parse(mobScale);
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
					throw;
				}
			}

			mob.KnownPosition = (PlayerLocation) player.KnownPosition.Clone();
			mob.SpawnEntity();

			player.SendMessage($"§e§l(!) §r§eSpawned new {entityType}");
		}

		[Command(Name = "getpos")]
		[Authorize(Permission = CommandPermission.Normal)]
		public void CommandGetPos(MiNET.Player player)
		{
			if (player.CommandPermission < CommandPermission.Admin)
			{
				player.SendMessage("§c§l(!)§r §cYou do not have permission for this command.");
				return;
			}

			PlayerLocation knownLocation = player.KnownPosition;

			player.SendMessage(
				$"Pos: {player.Level.LevelId}:{knownLocation.X},{knownLocation.Y},{knownLocation.Z}:{knownLocation.HeadYaw}:{knownLocation.Pitch}");
		}

		/*[Command(Name = "transfer")]
        [Authorize(Permission = CommandPermission.Admin)]
        public void CommandTransfer(MiNET.Player player, string address, ushort serverPort = 19132)
		{
			ushort port = serverPort;

            McpeTransfer transferPacket = new McpeTransfer
            {
                serverAddress = address,
                port = port
            };

            player.SendMessage($"§e§l(!) §r§eSending you to {address}:{port}...");
			SkyUtil.log($"Sending {player.Username} to {address}:{port}");
            player.SendPackage(transferPacket);
        }*/

		/**
		 * Credit to @gurun, as what is below is based on his work.
		 */
		[Command]
		public void Video2X(MiNET.Player player)
		{
			Task.Run(delegate
			{
				try
				{
					//W: 896 -> 7
					//H: 512-> 4

					const int width = 7;
					const int height = 4;

					string file = @"C:\Users\Administrator\Desktop\dl\TestImage.bmp";
					if (!File.Exists(file))
					{
						return;
					}

					Bitmap image = new Bitmap((Bitmap)Image.FromFile(file), width * 128, height * 128);

					BlockCoordinates center = player.KnownPosition.GetCoordinates3D();
					var level = player.Level;

					for (int z = 0; z < width; z++)
					{
						int zSpawnLoc = center.Z + (width - (z + (width / 2) + 1));
						for (int y = 0; y < height; y++)
						{
							var croppedImage = CropImage(image, new Rectangle(new Point(z * 128, y * 128), new Size(128, 128)));
							byte[] bitmapToBytes = BitmapToBytes(croppedImage, true);

							if (bitmapToBytes.Length != 128 * 128 * 4)
							{
								return;
							}

							MapEntity frame = new MapEntity(player.Level);
							frame.ImageProvider = new MapImageProvider { Batch = CreateCachedPacket(frame.EntityId, bitmapToBytes) };
							frame.SpawnEntity();

							BlockCoordinates frambc = new BlockCoordinates(center.X - 8, center.Y + height - y - 2, zSpawnLoc);
							ItemFrameBlockEntity itemFrameBlockEntity = new ItemFrameBlockEntity
							{
								Coordinates = frambc
							};

							var itemFrame = new CustomItemFrame(frame, itemFrameBlockEntity, level) { Coordinates = frambc, Metadata = 4 };
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
