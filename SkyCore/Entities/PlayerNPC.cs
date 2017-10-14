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
using MiNET.Utils.Skins;
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

        public PlayerNPC(string name, Level level, PlayerLocation playerLocation, onInteract action = null, string gameName = "") : base(name, level)
        {
	        NameTag = name;
            KnownPosition = playerLocation;

	        if (string.IsNullOrEmpty(gameName))
	        {
				Skin = new Skin { SkinData = Skin.GetTextureFromFile("Skin.png") };
			}
	        else
	        {
				Skin = new Skin { SkinData = Skin.GetTextureFromFile($"..\\npc-skins\\{gameName}-npc.png") };
			}

			Scale = 1.8D; //Ensure this NPC is visible

            Action = action;
        }

        public void OnInteract(MiNET.Player player)
        {
            if (Action != null)
            {
                SkyUtil.log($"(2) Processing NPC Interact as {player.Username}");
                Action((SkyPlayer)player);
            }
        }

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
	                        //Freeze the players movement
	                        player.SetNoAi(true);
							switch (gameName)
                            {
                                case "murder":
									RunnableTask.RunTaskLater(() => ExternalGameHandler.AddPlayer(player, gameName), 200);
                                    break;
								case "build-battle":
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
				PlayerNPC npc = new PlayerNPC("§a(Punch to play)", level, spawnLocation, action, gameName) {Scale = 1.5};

				SkyCoreAPI.Instance.AddPendingTask(() =>
				{
					npc.KnownPosition = spawnLocation;
					npc.SpawnEntity();
				});

	            {
		            PlayerLocation playerCountLocation = (PlayerLocation) spawnLocation.Clone();
		            
					//Spawn a hologram with player counts
		            PlayerCountHologram hologram = new PlayerCountHologram(npcName, level, playerCountLocation, gameName);

					SkyCoreAPI.Instance.AddPendingTask(() => hologram.SpawnEntity());
				}

				{
					PlayerLocation gameNameLocation = (PlayerLocation) spawnLocation.Clone();
					gameNameLocation.Y += 3.3f;

					Hologram gameNameHologram = new Hologram(npcName, level, gameNameLocation);

					SkyCoreAPI.Instance.AddPendingTask(() => gameNameHologram.SpawnEntity());
				}

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
