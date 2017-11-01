using System;
using System.Collections.Generic;
using MiNET.Entities;
using MiNET.Utils;
using MiNET.Utils.Skins;
using MiNET.Worlds;
using SkyCore.Entities.Holograms;
using SkyCore.Game;
using SkyCore.Game.Level;
using SkyCore.Games.Hub;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Entities
{

	public delegate void NPCSpawnTask(GameLevel gameLevel);

	public class PlayerNPC : PlayerMob
    {
	    
	    public static readonly IDictionary<string, NPCSpawnTask> GameNPCs = new Dictionary<string, NPCSpawnTask>();

	    public const string ComingSoonName = "§d§lMystery Game";

		public static List<PlayerNPC> PendingNpcs = new List<PlayerNPC>();

        public delegate void onInteract(SkyPlayer player);

        private readonly onInteract _action;

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

            _action = action;
        }

        public void OnInteract(MiNET.Player player)
        {
            if (_action != null)
            {
                SkyUtil.log($"(2) Processing NPC Interact as {player.Username}");
                _action((SkyPlayer)player);
            }
        }

        /*
         * Helper Methods
         */

	    public static void SpawnAllHubNPCs(HubLevel gameLevel)
	    {
		    if (gameLevel == null)
		    {
			    SkyUtil.log($"Attempted to spawn NPCs on gameLevel == null");
			    return;
		    }
		    if (gameLevel.CurrentlySpawnedNPCs == null)
		    {
				SkyUtil.log($"Attempted to spawn NPCs on gameLevel.CurrentlySpawnedNPCs == null");
			}
		    
		    foreach (KeyValuePair<string, NPCSpawnTask> entry in GameNPCs)
		    {
			    //Only spawn NPCs which have not been spawned yet
			    if (gameLevel
				    .
				    CurrentlySpawnedNPCs
				    .
				    Contains
				    (entry
					    .
					    Key))
			    {
				    continue;
			    }
			    
			    entry.Value.Invoke(gameLevel);
			    gameLevel.CurrentlySpawnedNPCs.Add(entry.Key);
		    }
	    }

        public static void SpawnHubNPC(GameLevel level, string npcName, PlayerLocation spawnLocation, string command)
        {
	        NPCSpawnTask spawnTask = (gameLevel) =>
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

							switch (gameName)
							{
								case "murder":
								case "build-battle":
								{
									action = player =>
									{
										//Freeze the players movement
										player.SetNoAi(true);
										RunnableTask.RunTaskLater(() => ExternalGameHandler.AddPlayer(player, gameName), 200);
									};
									break;
								}
							}
						}
					}

					if (gameName.Equals(command))
					{
						SkyUtil.log($"Unknown game command '{command}'");
						return;
					}

					//Ensure this NPC can be seen
					PlayerNPC npc;
					/*if (action != null)
					{
						npc = new PlayerNPC("§a(Punch to play)", gameLevel, spawnLocation, action, gameName) { Scale = 1.5 };
					}
					else
					{
						npc = new PlayerNPC("§e(Coming Soon)", gameLevel, spawnLocation, null, gameName) { Scale = 1.5 };
					}*/
					npc = new PlayerNPC("", gameLevel, spawnLocation, action, gameName) { Scale = 1.5 };

					SkyCoreAPI.Instance.AddPendingTask(() =>
					{
						npc.KnownPosition = spawnLocation;
						//npc.Width = 0D;
						//npc.Height = 1.0D;
						npc.SpawnEntity();
					});

					{
						PlayerLocation playerCountLocation = (PlayerLocation)spawnLocation.Clone();

						//Spawn a hologram with player counts
						PlayerCountHologram hologram = new PlayerCountHologram(npcName, gameLevel, playerCountLocation, gameName);

						SkyCoreAPI.Instance.AddPendingTask(() => hologram.SpawnEntity());
					}

					{
						PlayerLocation gameNameLocation = (PlayerLocation)spawnLocation.Clone();
						gameNameLocation.Y += 3.1f;

						Hologram gameNameHologram = new Hologram(npcName, gameLevel, gameNameLocation);

						SkyCoreAPI.Instance.AddPendingTask(() => gameNameHologram.SpawnEntity());
					}

					Console.WriteLine($"§e§l(!) §r§eSpawned NPC with text '{npcName}§r'");
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
					throw;
				}
			};

	        if (level != null)
	        {
				spawnTask.Invoke(level);
			}
	        
	        GameNPCs.Add(command, spawnTask);
        }

		public static void SpawnLobbyNPC(GameLevel level, string npcName, PlayerLocation spawnLocation)
		{
			try
			{
				npcName = npcName.Replace("_", " ").Replace("&", "§");

				if (npcName.Equals("\"\""))
				{
					npcName = "";
				}

				//Ensure this NPC can be seen
				PlayerNPC npc = new PlayerNPC("", level, spawnLocation) { Scale = 1.5 };

				SkyCoreAPI.Instance.AddPendingTask(() =>
				{
					npc.KnownPosition = spawnLocation;

					npc.SpawnEntity();
				});

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
