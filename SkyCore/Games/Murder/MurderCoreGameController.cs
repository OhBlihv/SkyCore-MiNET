using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MiNET.Net;
using MiNET.Plugins.Attributes;
using MiNET.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkyCore.Game;
using SkyCore.Game.Level;
using SkyCore.Games.Murder.Level;
using SkyCore.Player;

namespace SkyCore.Games.Murder
{
    public class MurderCoreGameController : CoreGameController
    {
        
        public MurderCoreGameController(SkyCoreAPI plugin) : base(plugin, "murder", "Murder Mystery", 
            new List<string>{"murder-library", "murder-library-alternative"})
		{
			SkyCoreAPI.Instance.Context.PluginManager.LoadCommands(this);  //Initialize Location/Murder Commands
		}

        protected override GameLevel _initializeNewGame()
        {
			string selelectedLevel = GetRandomLevelName();

	        return new MurderLevel(Plugin, GetNextGameId(), selelectedLevel, GetGameLevelInfo(selelectedLevel));
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

	    [Command(Name = "location")]
	    [Authorize(Permission = CommandPermission.Normal)]
	    public void CommandLocation(MiNET.Player player, string action = "", string type = "")
	    {
		    if (player.CommandPermission < CommandPermission.Admin)
		    {
			    player.SendMessage("§c§l(!)§r §cYou do not have permission for this command.");
			    return;
		    }

			if (action.Length == 0 || type.Length == 0)
		    {
			    player.SendMessage("§c/location <add> <spawn/gunpart>");
			    return;
		    }

		    if (!(player.Level is MurderLevel))
		    {
			    player.SendMessage("§cYou must be in a murder game to use this command!");
			    return;
		    }

		    MurderLevel murderLevel = (MurderLevel) player.Level;
		    if (!(SkyCoreAPI.Instance.GameModes["murder"].LoadGameLevelInfo(murderLevel.LevelName) is MurderLevelInfo murderLevelInfo))
		    {
			    player.SendMessage("§cThe current level's information could not be loaded.");
			    return;
		    }

			List<PlayerLocation> locationList = null;
		    if (type.Equals("spawn"))
		    {
			    locationList = murderLevelInfo.PlayerSpawnLocations;
		    }
		    else if(type.Equals("gunpart"))
		    {
			    locationList = murderLevelInfo.GunPartLocations;
		    }

		    if (locationList == null)
		    {
			    player.SendMessage($"§cAction invalid. Must be 'spawn' or 'gunpart', but was '{action}'");
			    return;
		    }

		    PlayerLocation addedLocation = (PlayerLocation) player.KnownPosition.Clone();
		    addedLocation.X = (float) (Math.Floor(addedLocation.X) + 0.5f);
		    addedLocation.X = (float) (Math.Floor(addedLocation.X) + 0.5f);
		    addedLocation.Z = (float) (Math.Floor(addedLocation.Z) + 0.5f);

			locationList.Add(addedLocation);

		    string fileName =
			    $"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\\config\\{murderLevel.GameType}-{murderLevel.LevelName}.json";

			SkyUtil.log($"Saving as '{fileName}' -> {murderLevel.GameType} AND {murderLevel.LevelName}");

			File.WriteAllText(fileName, JsonConvert.SerializeObject(murderLevelInfo, Formatting.Indented));

			player.SendMessage($"§cUpdated {action} location list ({locationList.Count}) with current location.");
	    }

	}
}