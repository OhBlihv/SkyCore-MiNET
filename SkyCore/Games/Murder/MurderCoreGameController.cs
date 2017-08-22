using System;
using System.Collections.Generic;
using System.Linq;
using MiNET;
using MiNET.Utils;
using MiNET.Worlds;
using SkyCore.Entities;
using SkyCore.Game;
using SkyCore.Game.State;

namespace SkyCore.Games.Murder
{
    public class MurderCoreGameController : CoreGameController
    {
        
        public MurderCoreGameController(SkyCoreAPI plugin) : base(plugin, "murder", "Murder Mystery", 
            new List<string>{"murder-library"})
        {
            Level level = plugin.Context.LevelManager.Levels.FirstOrDefault(l => l.LevelId.Equals("Overworld", StringComparison.InvariantCultureIgnoreCase));

            if (level == null)
            {
                Console.WriteLine($"§c§l(!) §r§cUnable to find level Overworld/world");

                string worldNames = "";
                foreach (Level levelLoop in plugin.Context.LevelManager.Levels)
                {
                    worldNames += levelLoop.LevelName + "(" + levelLoop.LevelId + "), ";
                }

                Console.WriteLine($"§7§l* §r§7Valid Names: {worldNames}");
            }
            else
            {
                PlayerNPC.SpawnNPC(level, "§e§lMurder Mystery", new PlayerLocation(0.5D, 30D, 16.5D, 180F, 180F), "GID:murder");
            }
        }

        protected override GameLevel _getGameController()
        {
            return new MurderLevel(Plugin, GetNextGameId(), GetRandomLevelName());
        }

    }
}