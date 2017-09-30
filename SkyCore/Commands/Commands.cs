using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
using SkyCore.Entities;
using SkyCore.Game;
using SkyCore.Games.Murder;
using SkyCore.Permissions;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Commands
{
    public class SkyCommands
    {

        private SkyCoreAPI skyCoreApi;

        public SkyCommands(SkyCoreAPI skyCoreApi)
        {
            this.skyCoreApi = skyCoreApi;
        }

		[Command(Name = "hub")]
	    [Authorize(Permission = CommandPermission.Normal)]
	    public void CommandHub(MiNET.Player player)
	    {
			MoveToLobby(player);
		}

	    [Command(Name = "lobby")]
	    [Authorize(Permission = CommandPermission.Normal)]
	    public void CommandLobby(MiNET.Player player)
	    {
		    MoveToLobby(player);
	    }

	    public void MoveToLobby(MiNET.Player player)
	    {
			player.SendMessage("§e§l(!)§r §eMoving to Hub...");

		    McpeTransfer transferPacket = new McpeTransfer
		    {
			    serverAddress = "184.171.171.26",
			    port = 19132
		    };

		    player.SendPackage(transferPacket);
		}

	    [Command(Name = "popuptest")]
	    [Authorize(Permission = CommandPermission.Normal)]
	    public void CommandPopupTest(MiNET.Player player, string popup, string actionbar)
	    {
		    SkyPlayer skyPlayer = (SkyPlayer) player;

			skyPlayer.BarHandler.AddMajorLine(popup);
			skyPlayer.BarHandler.AddMinorLine(actionbar);
	    }

		[Command(Name = "subtitletest")]
	    [Authorize(Permission = CommandPermission.Admin)]
	    public void CommandSubtitle(MiNET.Player player, int lineCount)
		{
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
	    [Authorize(Permission = CommandPermission.Admin)]
	    public void CommandActionBar(MiNET.Player player, int lineCount)
	    {
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
	    [Authorize(Permission = CommandPermission.Admin)]
	    public void CommandTitle(MiNET.Player player, int lineCount)
	    {
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
		[Authorize(Permission = CommandPermission.Admin)]
		public void CommandGamemode(MiNET.Player player, float speed = 0.1f)
		{
			player.MovementSpeed = speed;
			player.SendAdventureSettings();
			
			player.SendMessage($"§eUpdated movement speed to {speed}");
		}

		[Command(Name = "gamemode", Aliases = new[] {"gm"})]
        [Authorize(Permission = CommandPermission.Admin)]
        public void CommandGamemode(MiNET.Player player, int gamemodeId = 0)
        {
            CommandGamemode(player, player.Username, gamemodeId);
        }

        [Command(Name = "gamemode", Aliases = new[] {"gm"})]
        [Authorize(Permission = CommandPermission.Admin)]
        public void CommandGamemode(MiNET.Player player, string targetName = "", int gamemodeId = 0)
        {
            MiNET.Player target;
            if (String.IsNullOrEmpty(targetName))
            {
                player.SendMessage($"{ChatColors.Red}Enter a valid player name.");
                return;
            }

            target = skyCoreApi.GetPlayer(targetName);

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
                    if (((SkyPlayer) target).PlayerGroup.isAtLeast(PlayerGroup.Youtuber))
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
            MiNET.Player targetPlayer;
            if (String.IsNullOrEmpty(targetName))
            {
                targetPlayer = player;
            }
            else
            {
                targetPlayer = skyCoreApi.GetPlayer(targetName);
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

        [Command(Name = "hologram")]
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
        }

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
				        ExternalGameHandler.AddPlayer((SkyPlayer)player, "murder");
				        break;
			        }
			        case "build-battle":
			        {
						ExternalGameHandler.AddPlayer((SkyPlayer)player, "build-battle");
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

        [Command(Name = "npc")]
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
        }

        [Command(Name = "scale")]
        [Authorize(Permission = CommandPermission.Admin)]
        public void CommandScale(MiNET.Player player, string scaleString, string targetName = "")
        {
            MiNET.Player targetPlayer;
            if (String.IsNullOrEmpty(targetName))
            {
                targetPlayer = player;
            }
            else
            {
                targetPlayer = skyCoreApi.GetPlayer(targetName);
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
        [Authorize(Permission = CommandPermission.Admin)]
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
                    mob = new Wolf(level) { Owner = player };
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

            mob.KnownPosition = (PlayerLocation)player.KnownPosition.Clone();
            mob.SpawnEntity();

            player.SendMessage($"§e§l(!) §r§eSpawned new {entityType}");
        }

        [Command(Name = "world")]
        [Authorize(Permission = CommandPermission.Admin)]
        public void CommandWorld(MiNET.Player player, string worldName)
        {
            if (worldName.Equals("world-treasurewars"))
            {
                new MurderCoreGameController(skyCoreApi).GetGameController(); //initialize new level
            }
            
            Level level = skyCoreApi.Context.LevelManager.Levels.FirstOrDefault(l => l.LevelId.Equals(worldName, StringComparison.InvariantCultureIgnoreCase));
            if (level == null)
            {
                player.SendMessage($"§c§l(!) §r§cUnable to find level {worldName}");

                string worldNames = "";
                foreach(Level levelLoop in skyCoreApi.Context.LevelManager.Levels)
                {
                    worldNames += levelLoop.LevelName + "(" + levelLoop.LevelId + "), ";
                }
                
                player.SendMessage($"§7§l* §r§7Valid Names: {worldNames}");
                return;
            }
            
            player.SendMessage($"§e§l(!) §r§eTeleporting to {worldName}");
            player.SpawnLevel(level, player.KnownPosition);
        }

        [Command(Name = "getpos")]
        [Authorize(Permission = CommandPermission.Admin)]
        public void CommandGetPos(MiNET.Player player)
        {
            PlayerLocation knownLocation = player.KnownPosition;

            player.SendMessage($"Pos: {player.Level.LevelId}:{knownLocation.X},{knownLocation.Y},{knownLocation.Z}:{knownLocation.HeadYaw}:{knownLocation.Pitch}");
        }

		[Command(Name = "transfer")]
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
        }

		[Command(Name = "maptest")]
        [Authorize(Permission = CommandPermission.Admin)]
        public void CommandTestMap(MiNET.Player player)
        {
            player.SendMessage("_o_");
            PlayerLocation playerLocation = player.KnownPosition;
            
            //Stone
            /*player.Level.SetBlock((int) playerLocation.X, (int) playerLocation.Y, (int)playerLocation.Z, 1);
            player.Level.SetBlock((int) playerLocation.X, (int) playerLocation.Y, (int)playerLocation.Z + 1, 1);
            player.Level.SetBlock((int) playerLocation.X, (int) playerLocation.Y + 1, (int)playerLocation.Z, 1);
            player.Level.SetBlock((int) playerLocation.X, (int) playerLocation.Y + 1, (int)playerLocation.Z + 1, 1);*/

            /*VideoImageProvider videoProvider = new VideoImageProvider(new FrameTicker(100));
            videoProvider.Frames = new List<McpeWrapper>();
            videoProvider.Frames.Add(new McpeWrapper());*/
            
            player.Level.SetBlock((int) playerLocation.X, (int)playerLocation.Y, (int)playerLocation.Z, 199);
            
            /*ItemFrame itemFrameBlock = new ItemFrame();
            itemFrameBlock.PlaceBlock(player.Level, player, new BlockCoordinates(player.KnownPosition), BlockFace.West, Vector3.One);*/
            
            MapEntity mapEntity = new MapEntity(player.Level);
            //mapEntity.ImageProvider = new RandomColorMapImageProvider();
            mapEntity.KnownPosition = (PlayerLocation) playerLocation.Clone();
            mapEntity.ImageProvider = new TextMapImageProvider("Test");

            //((ItemFrameBlockEntity) player.Level.GetBlockEntity(new BlockCoordinates(playerLocation))).SetItem(mapEntity);
            //player.Level.SetBlockEntity(mapEntity, true);

            //mapEntity.AddToMapListeners(player, mapEntity.MapInfo.MapId);

            mapEntity.SpawnEntity();
            //player.Level.AddEntity(mapEntity);

            player.SendMessage("\\o/");
        }

        [Command]
        //[Authorize(Permission = UserPermission.Op)]
        public void VideoX(MiNET.Player player, int numberOfFrames, bool color)
        {
            Task.Run(delegate
            {
                try
                {
                    Dictionary<Tuple<int, int>, MapEntity> entities = new Dictionary<Tuple<int, int>, MapEntity>();

                    int width = 2;
                    int height = 2;
                    int frameCount = numberOfFrames;
                    //int frameOffset = 0;
                    int frameOffset = 120;

                    var frameTicker = new FrameTicker(frameCount);


                    // 768x384
                    for (int frame = frameOffset; frame < frameCount + frameOffset; frame++)
                    {
                        Console.WriteLine($"Generating frame {frame}");

                        string file = Path.Combine(@"D:\Development\Other\Smash Heroes 3x6 (128)\Smash Heroes 3x6 (128)", $"Smash Heroes Trailer{frame:D4}.bmp");
                        //string file = Path.Combine(@"D:\Development\Other\2 by 1 PE test app ad for Gurun-2\exported frames 2", $"pe app ad{frame:D2}.bmp");
                        if (!File.Exists(file)) continue;

                        Bitmap image = new Bitmap((Bitmap)Image.FromFile(file), width * 128, height * 128);

                        for (int x = 0; x < width; x++)
                        {
                            for (int y = 0; y < height; y++)
                            {
                                var key = new Tuple<int, int>(x, y);
                                if (!entities.ContainsKey(key))
                                {
                                    entities.Add(key, new MapEntity(player.Level) { ImageProvider = new VideoImageProvider(frameTicker) });
                                }

                                var croppedImage = CropImage(image, new Rectangle(new Point(x * 128, y * 128), new Size(128, 128)));
                                byte[] bitmapToBytes = BitmapToBytes(croppedImage, color);

                                if (bitmapToBytes.Length != 128 * 128 * 4) return;

                                ((VideoImageProvider)entities[key].ImageProvider).Frames.Add(CreateCachedPacket(entities[key].EntityId, bitmapToBytes));
                            }
                        }
                    }

                    int i = 0;

                    player.Inventory.Slots[i++] = new ItemBlock(new Planks(), 0) { Count = 64 };
                    player.Inventory.Slots[i++] = new ItemItemFrame { Count = 64 };

                    foreach (MapEntity entity in entities.Values)
                    {
                        entity.SpawnEntity();
                        player.Inventory.Slots[i++] = new ItemMap(entity.EntityId);
                    }

                    player.SendPlayerInventory();
                    player.SendMessage("Done generating video.", MessageType.Raw);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Aborted video generation", e);
                }
            });

            player.SendMessage("Generating video...", MessageType.Raw);
        }

        private McpeWrapper CreateCachedPacket(long mapId, byte[] bitmapToBytes)
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
            McpeWrapper batch = BatchUtils.CreateBatchPacket(bytes, 0, (int)bytes.Length, CompressionLevel.Optimal, true);
            batch.MarkPermanent();
            batch.Encode();
            return batch;
        }


        private static Bitmap CropImage(Bitmap img, Rectangle cropArea)
        {
            return img.Clone(cropArea, img.PixelFormat);
        }

        private static byte[] ReadFrame(string filename)
        {
            Bitmap bitmap;
            try
            {
                bitmap = new Bitmap(filename);
            }
            catch (Exception)
            {
                Console.WriteLine("Failed reading file " + filename);
                bitmap = new Bitmap(128, 128);
            }

            byte[] bytes = BitmapToBytes(bitmap);

            return bytes;
        }

        public Bitmap GrayScale(Bitmap bmp)
        {
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    var c = bmp.GetPixel(x, y);
                    var rgb = (int)((c.R + c.G + c.B) / 3);
                    bmp.SetPixel(x, y, Color.FromArgb(rgb, rgb, rgb));
                }
            }
            return bmp;
        }

        private static byte[] BitmapToBytes(Bitmap bitmap, bool useColor = false)
        {
            byte[] bytes;
            bytes = new byte[bitmap.Height * bitmap.Width * 4];

            int i = 0;
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Color color = bitmap.GetPixel(x, y);
                    if (!useColor)
                    {
                        byte rgb = (byte)((color.R + color.G + color.B) / 3);
                        bytes[i++] = rgb;
                        bytes[i++] = rgb;
                        bytes[i++] = rgb;
                        bytes[i++] = 0xff;
                    }
                    else
                    {
                        bytes[i++] = color.R;
                        bytes[i++] = color.G;
                        bytes[i++] = color.B;
                        bytes[i++] = 0xff;
                    }
                }
            }
            return bytes;
        }

    }

}
