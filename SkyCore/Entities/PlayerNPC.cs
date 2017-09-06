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
using SkyCore.Game;
using SkyCore.Player;

namespace SkyCore.Entities
{
    public class PlayerNPC : PlayerMob
    {

        public static List<PlayerNPC> PendingNpcs = new List<PlayerNPC>();

        public delegate void onInteract(SkyPlayer player);

        private onInteract Action;

        public PlayerNPC(string name, Level level, PlayerLocation playerLocation, onInteract action = null, MiNetServer server = null) : base(name, level)
        {
            KnownPosition = playerLocation;
            Skin = new Skin {Texture = Skin.GetTextureFromFile("Skin.png")};

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
            else
            {
                player.SendMessage("It Works!");
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

                onInteract action = null;
                if (!String.IsNullOrEmpty(command))
                {
                    if (command.StartsWith("GID:"))
                    {
                        action = player =>
                        {
                            switch (command)
                            {
								//TODO: Split around the colon
                                case "GID:murder":
                                    player.SendMessage("Queueing for Murder");
									//SkyUtil.log($"Sending {player.Username} to {ExternalGameHandler.GameRegistrations["murder"].ConnectingAddress}:{ExternalGameHandler.GameRegistrations["murder"].ConnectingPort}");
                                    //SkyCoreAPI.Instance.GameModes["murder"].QueuePlayer(player);
									ExternalGameHandler.AddPlayer(player, "murder");
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

				//Ensure this NPC can be seen
				PlayerNPC npc = new PlayerNPC(npcName, level, spawnLocation, action) {Scale = 1.2};

				npc.BroadcastEntityEvent();
                npc.BroadcastSetEntityData();

				SkyCoreAPI.Instance.AddPendingTask(() => npc.SpawnEntity());
                /*try
                {
                    npc.SpawnEntity();
                }
                catch (Exception e)
                {
                    //PendingNpcs.Add(npc);
                    Console.WriteLine(e);
                }*/

                PendingNpcs.Add(npc);

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
