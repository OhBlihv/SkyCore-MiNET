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
    public class MurderGameController : GameController
    {
        
        public MurderGameController(SkyCoreAPI plugin) : base(plugin, "murder", "Murder Mystery", 
            new List<string>{"murder-grandlibrary", "murder-funzone", "murder-sunsetresort"})
		{
			
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

		//

	    private readonly IDictionary<string, RunnableTask> _currentVisualizationTasks = new Dictionary<string, RunnableTask>();

	    public override bool HandleGameEditCommand(SkyPlayer player, GameLevel level, GameLevelInfo gameLevelInfo, params string[] args)
		{
			if (!(gameLevelInfo is MurderLevelInfo murderLevelInfo))
			{
				player.SendMessage("§cThe current levels game info is not in the correct format to be a Murder Level Info.");
				player.SendMessage("§cUpdating as MurderLevelInfo and saving with default options.");

				murderLevelInfo = new MurderLevelInfo(gameLevelInfo.LevelName, gameLevelInfo.WorldTime, gameLevelInfo.LobbyLocation,
					new List<PlayerLocation>(), new List<PlayerLocation>());
			}

			if (args[0].Equals("add"))
			{
				if (args.Length < 2)
				{
					player.SendMessage("§c/location add <spawn/gunpart>");
					player.SendMessage("§cNot Enough Arguments.");
					return true;
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
					return true;
				}

				PlayerLocation addedLocation = (PlayerLocation)player.KnownPosition.Clone();
				addedLocation.X = (float)(Math.Floor(addedLocation.X) + 0.5f);
				addedLocation.Y = (float)Math.Floor(addedLocation.Y);
				addedLocation.Z = (float)(Math.Floor(addedLocation.Z) + 0.5f);

				addedLocation.HeadYaw = (float)Math.Floor(addedLocation.HeadYaw);
				addedLocation.HeadYaw = addedLocation.HeadYaw;
				addedLocation.Pitch = (float)Math.Floor(addedLocation.Pitch);

				locationList.Add(addedLocation);

				string fileName =
					 $"C:\\Users\\Administrator\\Desktop\\worlds\\{RawName}\\{RawName}-{level.LevelName}.json";

				SkyUtil.log($"Saving as '{fileName}' -> {level.GameType} AND {level.LevelName}");

				File.WriteAllText(fileName, JsonConvert.SerializeObject(gameLevelInfo, Formatting.Indented));

				player.SendMessage($"§cUpdated {args[0]} location list ({locationList.Count}) with current location.");

				//Update current level info
				((MurderLevel)player.Level).GameLevelInfo = gameLevelInfo;
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

							new FlameParticle(player.Level) { Position = particleLocation }.Spawn(new MiNET.Player[] { player });
						}

						foreach (PlayerLocation location in murderLevelInfo.PlayerSpawnLocations)
						{
							PlayerLocation displayLocation = (PlayerLocation)location.Clone();
							displayLocation.Y += 0.5f;

							Vector3 particleLocation = displayLocation.ToVector3();

							new HeartParticle(player.Level) { Position = particleLocation }.Spawn(new MiNET.Player[] { player });
						}
					}, 500));
				}
			}
			else if (args[0].Equals("tp"))
			{
				player.SendMessage("§eTeleporting to a random spawn location");
				player.Teleport(murderLevelInfo.PlayerSpawnLocations[Random.Next(murderLevelInfo.PlayerSpawnLocations.Count)]);
			}
			else
			{
				//Falls through, no specific handling
				return false;
			}

			//One if-branch was used, this counts enough as usage
			return true;
		}

	    public override string GetGameEditCommandHelp(SkyPlayer player)
	    {
		    return "§c/gameedit add <spawn/gunpart>\n" +
		           "§c/gameedit visualize";
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

	}
}