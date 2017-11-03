using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using MiNET.Net;
using MiNET.Particles;
using MiNET.Plugins.Attributes;
using MiNET.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkyCore.Game;
using SkyCore.Game.Level;
using SkyCore.Game.State.Impl;
using SkyCore.Games.Murder.Level;
using SkyCore.Games.Murder.State;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Games.Murder
{
    public class MurderCoreGameController : CoreGameController
    {
        
        public MurderCoreGameController(SkyCoreAPI plugin) : base(plugin, "murder", "Murder Mystery", 
            new List<string>{"murder-grandlibrary", "murder-funzone", "murder-sunsetresort"})
		{
			
		}

	    public override void PostLaunchTask()
	    {
		    base.PostLaunchTask();

			SkyCoreAPI.Instance.Server.PluginManager.LoadCommands(this);  //Initialize Location/Murder Commands
		}

	    protected override GameLevel _initializeNewGame()
        {
			string selelectedLevel = GetRandomLevelName();

	        return new MurderLevel(Plugin, GetNextGameId(), selelectedLevel, GetGameLevelInfo(selelectedLevel));
        }

	    protected override GameLevel _initializeNewGame(string levelName)
	    {
			return new MurderLevel(Plugin, GetNextGameId(), levelName, GetGameLevelInfo(levelName));
		}

	    public override Type GetGameLevelInfoType()
	    {
		    return typeof(MurderLevelInfo);
		}

	    /*[Command(Name = "nospec")]
	    [Authorize(Permission = CommandPermission.Host)]
	    public void CommandLocation(MiNET.Player player)
	    {
		    if (!(player.Level is MurderLevel))
		    {
				player.SendMessage("Not murder level");
			    return;
		    }

		    SkyPlayer altPlayer = SkyCoreAPI.Instance.GetPlayer("OhBlihv2");

			//Set back as detective 
			player.SendMessage("Alt should be detective");
		    ((MurderLevel) altPlayer.Level).SetPlayerTeam(altPlayer, MurderTeam.Detective);
		}*/

	    private readonly IDictionary<string, RunnableTask> _currentVisualizationTasks = new Dictionary<string, RunnableTask>();

	    [Command(Name = "gameedit")]
	    [Authorize(Permission = CommandPermission.Normal)]
	    public void CommandGameEdit(MiNET.Player player, params string[] args)
	    {
		    if (player.CommandPermission < CommandPermission.Admin)
		    {
			    player.SendMessage("§c§l(!)§r §cYou do not have permission for this command.");
			    return;
		    }

		    if (!(player.Level is MurderLevel))
		    {
			    player.SendMessage("§cYou must be in a murder game to use this command!");
			    return;
		    }

		    MurderLevel murderLevel = (MurderLevel) player.Level;
		    if (!(murderLevel.GameLevelInfo is MurderLevelInfo murderLevelInfo))
		    {
				player.SendMessage("§cThe current level's information could not be loaded.");
			    return;
		    }

		    if (args[0].Equals("add"))
		    {
			    if (args.Length < 2)
			    {
					player.SendMessage("§c/location add <spawn/gunpart>");
				    player.SendMessage("§cNot Enough Arguments.");
				    return;
			    }
			    
			    List<PlayerLocation> locationList = null;
			    if (args[1].Equals("spawn"))
			    {
				    locationList = murderLevelInfo.PlayerSpawnLocations;
			    }
			    else if (args[1].Equals("gunpart"))
			    {
				    locationList = murderLevelInfo.GunPartLocations;
			    }

			    if (locationList == null)
			    {
				    player.SendMessage($"§cAction invalid. Must be 'spawn' or 'gunpart', but was '{args[1]}'");
				    return;
			    }

			    PlayerLocation addedLocation = (PlayerLocation)player.KnownPosition.Clone();
			    addedLocation.X = (float)(Math.Floor(addedLocation.X) + 0.5f);
			    addedLocation.Y = (float) Math.Floor(addedLocation.Y);
			    addedLocation.Z = (float)(Math.Floor(addedLocation.Z) + 0.5f);

			    addedLocation.HeadYaw = (float) Math.Floor(addedLocation.HeadYaw);
			    addedLocation.HeadYaw = addedLocation.HeadYaw;
				addedLocation.Pitch = (float)Math.Floor(addedLocation.Pitch);

				locationList.Add(addedLocation);

			    string fileName =
					 $"C:\\Users\\Administrator\\Desktop\\worlds\\{RawName}\\{RawName}-{murderLevel.LevelName}.json";

				SkyUtil.log($"Saving as '{fileName}' -> {murderLevel.GameType} AND {murderLevel.LevelName}");

			    File.WriteAllText(fileName, JsonConvert.SerializeObject(murderLevelInfo, Formatting.Indented));

			    player.SendMessage($"§cUpdated {args[0]} location list ({locationList.Count}) with current location.");
			    
			    //Update current level info
			    ((MurderLevel) player.Level).GameLevelInfo = murderLevelInfo;
		    }
		    else if (args[0].Equals("visualize"))
		    {
			    if (_currentVisualizationTasks.ContainsKey(player.Username))
			    {
				    _currentVisualizationTasks[player.Username].Cancelled = true;

				    _currentVisualizationTasks.Remove(player.Username);

					player.SendMessage("§eCancelling Visualization Task");
				}
			    else
				{
					player.SendMessage("§eVisualizing Gun Part and Player Spawn Locations...");

					_currentVisualizationTasks.Add(player.Username, RunnableTask.RunTaskIndefinitely(() =>
					{
						foreach (PlayerLocation location in murderLevelInfo.GunPartLocations)
						{
							PlayerLocation displayLocation = (PlayerLocation)location.Clone();
							displayLocation.Y += 0.5f;

							Vector3 particleLocation = displayLocation.ToVector3();
							
							new FlameParticle(player.Level){Position = particleLocation}.Spawn(new[]{player});
						}

						foreach (PlayerLocation location in murderLevelInfo.PlayerSpawnLocations)
						{
							PlayerLocation displayLocation = (PlayerLocation)location.Clone();
							displayLocation.Y += 0.5f;

							Vector3 particleLocation = displayLocation.ToVector3();

							new HeartParticle(player.Level) { Position = particleLocation }.Spawn(new[] { player });
						}
					}, 500));
				}
		    }
		    else if (args[0].Equals("tp"))
		    {
				player.SendMessage("§eTeleporting to a random spawn location");
				player.Teleport(murderLevelInfo.PlayerSpawnLocations[Random.Next(murderLevelInfo.PlayerSpawnLocations.Count)]);
		    }
		    else if (args[0].Equals("timeleft"))
		    {
			    if (args.Length < 2)
			    {
					player.SendMessage("§c/gameedit timeleft <time>");
				    return;
			    }

			    if (!int.TryParse(args[1], out var timeRemaining))
			    {
					player.SendMessage($"§cInvalid time remaining ({args[1]})");
				    return;
			    }

			    murderLevel.Tick = 0;
				((MurderRunningState) murderLevel.CurrentState).EndTick = timeRemaining * 2;
		    }
		    else if (args[0].Equals("level"))
		    {
			    if (args.Length < 2)
			    {
					player.SendMessage("§c/gameedit level <levelname>");
				    return;
				}

			    string fullyQualifiedName = $"C:\\Users\\Administrator\\Desktop\\worlds\\{RawName}\\{args[1]}";
				GameLevel gameLevel;
			    if (!LevelNames.Contains(fullyQualifiedName) || (gameLevel = InitializeNewGame(fullyQualifiedName)) == null)
			    {
				    player.SendMessage($"§cInvalid level name ({args[1]})");
					player.SendMessage($"§cBad Args: \n§c- {string.Join("\n§c- ", LevelNames.Select(x => _removeQualification(x.ToString())).ToArray())}");
				    return;
			    }

			    foreach (SkyPlayer gamePlayer in murderLevel.GetAllPlayers())
			    {
				    gameLevel.AddPlayer(gamePlayer);
			    }
			    
			    murderLevel.UpdateGameState(new VoidGameState()); //'Close' the game eventually
			    
			    player.SendMessage($"§cUpdating game level to {args[1]}");
		    }
			else
		    {
				player.SendMessage("§c/gameedit add <spawn/gunpart>");
			    player.SendMessage("§c/gameedit visualize");
			    player.SendMessage("§c/gameedit timeleft");
			    player.SendMessage("§c/gameedit tp");
				player.SendMessage($"§cBad Args: {string.Join(",", args.Select(x => x.ToString()).ToArray())}");
		    }
		}

	    private string _removeQualification(string fullyQualifiedName)
	    {
		    string levelName;
		    {
			    string[] split = fullyQualifiedName.Split('\\');
			    levelName = split[split.Length - 1];
		    }
		    return levelName;
		}

	}
}