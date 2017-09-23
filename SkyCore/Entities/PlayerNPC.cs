using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using MiNET;
using MiNET.Entities;
using MiNET.Entities.Hostile;
using MiNET.Net;
using MiNET.Plugins;
using MiNET.Utils;
using MiNET.Worlds;
using SkyCore.Entities.Holograms;
using SkyCore.Game;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Entities
{
    public class PlayerNPC : PlayerMob
    {

        public static List<PlayerNPC> PendingNpcs = new List<PlayerNPC>();

        public delegate void onInteract(SkyPlayer player);

        private onInteract Action;

        public PlayerNPC(string name, Level level, PlayerLocation playerLocation, onInteract action = null) : base(name, level)
        {
	        /*NameTag = name;
            KnownPosition = playerLocation;
			Skin = new Skin { SkinData = Skin.GetTextureFromFile("Skin.png")};
	        EntityId = 52;

			Scale = 1.8D; //Ensure this NPC is visible

            Action = action;*/
        }

        public void OnInteract(MiNET.Player player)
        {
            if (Action != null)
            {
                SkyUtil.log($"(2) Processing NPC Interact as {player.Username}");
                Action((SkyPlayer)player);
            }
        }

        /*public int Time = 0;

        public int Minute = 0;

        public override void OnTick()
        {
            Time++;
            if (Time == 20)
            {
                Minute++;
                if (Game != null)
                {
                    NameTag = NameString + Game.PlayerCount;
                    BroadcastSetEntityData();

                }
                else
                {
                    if (LevelLobby.isGlobalLobby && NameString != null)
                    {
                        NameTag = string.Format(NameString, server.ServerInfo.NumberOfPlayers);
                        BroadcastSetEntityData();
                        if (Minute == 60)
                        {
                            Minute = 0;
                            Level.BroadcastMessage(Plugin.Rm.get());
                        }
                    }
                }
                Time = 0;
            }
        }*/

        /*
         * Helper Methods
         */

        public static void SpawnNPC(Level level, string npcName, PlayerLocation spawnLocation, string command)
        {
            try
            {
                if (String.IsNullOrEmpty(npcName))
                {
                    Console.WriteLine("§c§l(!) §r§cInvalid NPC text. /hologram <text>");
                    return;
                }

                npcName = npcName.Replace("_", " ").Replace("&", "§");

                if (npcName.Equals("\"\""))
                {
                    npcName = "";
                }

				string gameName = command;
                onInteract action = null;
                if (!String.IsNullOrEmpty(command))
                {
                    if (command.StartsWith("GID:"))
					{
						gameName = command.Split(':')[1];
						action = player =>
                        {
	                        //player.FreezePlayer = true; //Avoid movement //TODO: Set speed to 0
							switch (gameName)
                            {
                                case "murder":
                                    player.SendMessage($"Queueing for {gameName}");
									RunnableTask.RunTaskLater(() => ExternalGameHandler.AddPlayer(player, gameName), 200);
                                    break;
								case "build-battle":
									player.SendMessage($"Queueing for {gameName}");
									RunnableTask.RunTaskLater(() => ExternalGameHandler.AddPlayer(player, gameName), 200);
									break;
								default:
                                    Console.WriteLine($"Unable to process game command {command}");
                                    break;
                            }
                        };
                    }
                    else
                    {
                        //TODO:
                    }
                }

				if (gameName.Equals(command))
				{
					SkyUtil.log($"Unknown game command '{command}'");
					return;
				}

				//Ensure this NPC can be seen
				PlayerNPC npc = new PlayerNPC(npcName, level, spawnLocation, action) {Scale = 1.2};
				//PlayerMob npc = new PlayerMob("Name", level);

				//npc.BroadcastEntityEvent();
                //npc.BroadcastSetEntityData();

				SkyCoreAPI.Instance.AddPendingTask(() => npc.SpawnEntity());

				//Spawn a hologram with player counts //TODO: Split around the colon
	            PlayerCountHologram hologram = new PlayerCountHologram(npcName, level, spawnLocation, gameName);

	            hologram.BroadcastEntityEvent();
	            hologram.BroadcastSetEntityData();

	            SkyCoreAPI.Instance.AddPendingTask(() => hologram.SpawnEntity());

				{
					PlayerLocation betaLocation = (PlayerLocation) spawnLocation.Clone();
					betaLocation.Y += 2.80f;

					//Spawn a hologram with player counts //TODO: Split around the colon
					Hologram betaHologram = new Hologram(npcName, level, betaLocation);
					betaHologram.SetNameTag("§e§lBETA");

					betaHologram.BroadcastEntityEvent();
					betaHologram.BroadcastSetEntityData();

					SkyCoreAPI.Instance.AddPendingTask(() => betaHologram.SpawnEntity());
				}

				//PendingNpcs.Add(npc);

                Console.WriteLine($"§e§l(!) §r§eSpawned NPC with text '{npcName}§r'");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

    }
}
