using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiNET;
using MiNET.Plugins;
using MiNET.Plugins.Attributes;
using MiNET.Utils;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Permissions
{

    public class PlayerGroup : Enumeration
    {
        
        public static readonly PlayerGroup Player    = new PlayerGroup(0, "Player", "§7", "§7", UserPermission.Any);
        public static readonly PlayerGroup Vip       = new PlayerGroup(1, "VIP", "§a", "§a[VIP]", UserPermission.Any);
        public static readonly PlayerGroup Pro       = new PlayerGroup(2, "PRO", "§b", "§b[PRO]", UserPermission.Any);
        public static readonly PlayerGroup Mvp       = new PlayerGroup(3, "MVP", "§d", "§d[MVP]", UserPermission.Any);
        public static readonly PlayerGroup Helper    = new PlayerGroup(4, "Helper", "§d", "§d[HELPER]", UserPermission.Gamemasters);
        public static readonly PlayerGroup Mod       = new PlayerGroup(5, "Mod", "§b", "§b[MOD]", UserPermission.Gamemasters);
        public static readonly PlayerGroup Developer = new PlayerGroup(6, "Developer", "§e", "§e[DEVELOPER]", UserPermission.Host);
        public static readonly PlayerGroup Youtuber  = new PlayerGroup(7, "Youtuber", "§c", "§c[YOUTUBE]", UserPermission.Host);
        public static readonly PlayerGroup Admin     = new PlayerGroup(8, "Admin", "§c", "§c[ADMIN]", UserPermission.Admin);

        //

        private int _enumVal { get; }

        public string GroupName { get; }

        public string GroupColour { get; }

        public string Prefix { get; }

        public UserPermission PermissionLevel { get; }

        private PlayerGroup(int enumVal, string groupName, string groupColour, string prefix, UserPermission permissionLevel)
        {
            _enumVal = enumVal;

            GroupName = groupName;
            GroupColour = groupColour;
            Prefix = prefix;
            PermissionLevel = permissionLevel;
        }

        public int compareTo(PlayerGroup playerGroup)
        {
            return _enumVal.CompareTo(playerGroup._enumVal);
        }

        public bool isAtLeast(PlayerGroup playerGroup)
        {
            return compareTo(playerGroup) >= 0;
        }

        public static bool ValueOf(string groupName, out PlayerGroup playerGroup)
        {
            playerGroup = null;

            switch (groupName.ToLower())
            {
                case "player": playerGroup = Player;
                    break;
                case "vip": playerGroup = Vip;
                    break;
                case "pro": playerGroup = Pro;
                    break;
                case "mvp": playerGroup = Mvp;
                    break;
                case "helper": playerGroup = Helper;
                    break;
                case "mod": playerGroup = Mod;
                    break;
                case "developer": playerGroup = Developer;
                    break;
                case "youtuber": playerGroup = Youtuber;
                    break;
                case "admin": playerGroup = Admin;
                    break;
            }

            return playerGroup != null;
        }

    }

    public class SkyPermissions
    {

        private readonly SkyCoreAPI skyCoreApi;

        public SkyPermissions(SkyCoreAPI skyCoreApi)
        {
            this.skyCoreApi = skyCoreApi;
        }

        public PlayerGroup getPlayerGroup(string playerName)
        {
            PlayerGroup playerGroup = PlayerGroup.Player;
            if (playerName.Equals("OhBlihv") || playerName.Equals("OhBlihv2") || playerName.Equals("Donnas_Wraps"))
            {
                playerGroup = PlayerGroup.Admin;
            }

            return playerGroup;
        }

        [Command(Name = "perm set")]
        [Authorize(Permission = UserPermission.Admin)]
        public void CommandPermSet(MiNET.Player player, string targetName, string targetGroupName)
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

            //Format as our Enums
            targetGroupName = targetGroupName.ToLower();
            targetGroupName = Char.ToUpper(targetGroupName.ToCharArray()[0]) + targetGroupName.Substring(1);

            PlayerGroup targetGroup;
            if (!PlayerGroup.ValueOf(targetGroupName, out targetGroup))
            {
                player.SendMessage($"{ChatColors.Red}Unrecognized group name '{targetGroupName}'.");
                string possibleGroups = "";
                foreach (PlayerGroup groupLoop in Enum.GetValues(typeof(PlayerGroup)))
                {
                    possibleGroups += groupLoop + ",";
                }

                player.SendMessage($"Possible Groups: {possibleGroups}");
                return;
            }

            ((SkyPlayer) player).setPlayerGroup(targetGroup);
            player.SendMessage($"{ChatColors.Yellow}Updated {player.Username}'s group to {targetGroup}");
        }

        [Command(Name = "perm get")]
        [Authorize(Permission = UserPermission.Admin)]
        public void CommandPermGet(MiNET.Player player, string targetName)
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

            player.SendMessage($"{ChatColors.Yellow}{player.Username} is currently rank '{((SkyPlayer)player).PlayerGroup}'");
        }

    }
}
