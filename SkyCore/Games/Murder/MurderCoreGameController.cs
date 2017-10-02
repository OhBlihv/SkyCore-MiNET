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

namespace SkyCore.Games.Murder
{
    public class MurderCoreGameController : CoreGameController
    {
        
        public MurderCoreGameController(SkyCoreAPI plugin) : base(plugin, "murder", "Murder Mystery", 
            new List<string>{"murder-library", "murder-library-alternative"})
		{
			SkyCoreAPI.Instance.Context.PluginManager.LoadCommands(this);  //Initialize Location/Murder Commands
		}

        protected override GameLevel _getGameController()
        {
            return new MurderLevel(Plugin, GetNextGameId(), GetRandomLevelName());
        }

	    public override Type GetGameLevelInfoType()
	    {
		    return typeof(MurderLevelInfo);
	    }

	    [Command(Name = "location")]
	    [Authorize(Permission = CommandPermission.Normal)]
	    public void CommandHub(MiNET.Player player, string action = "", string type = "")
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

		    if (!(player.Level is MurderLevel murderLevel))
		    {
			    player.SendMessage("§cYou must be in a murder game to use this command!");
			    return;
		    }

		    List<PlayerLocation> locationList = null;
		    //string typePath = null;
		    if (type.Equals("spawn"))
		    {
			    //typePath = $"level-names.{murderLevel.LevelName}.spawn-locations";
			    locationList = murderLevel.PlayerSpawnLocations;
		    }
		    else if(type.Equals("gunpart"))
		    {
			    //typePath = $"level-names.{murderLevel.LevelName}.gun-part-locations";
			    locationList = murderLevel.GunPartLocations;
		    }

		    if (locationList == null)
		    {
			    player.SendMessage($"§cAction invalid. Must be 'spawn' or 'gunpart', but was '{action}'");
			    return;
		    }

		    {
			    List<PlayerLocation> intermediateList = new List<PlayerLocation>();
			    intermediateList.AddRange(locationList);

			    locationList = intermediateList;
		    }

			locationList.Add(player.KnownPosition);

			//FlatFile flatFile = FlatFile.ForFile(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\config\\murder.yml");

			//flatFile.Set(typePath, locationList);

			File.WriteAllText($"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\\config\\murder-{type.ToLower()}-{murderLevel.LevelName}.json",
								JsonConvert.SerializeObject(locationList));

			player.SendMessage($"§cUpdated {action} location list with current location.");
	    }

	}
}