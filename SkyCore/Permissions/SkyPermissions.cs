﻿using System;
using System.Collections.Generic;
using MiNET.Net;
using MiNET.Plugins.Attributes;
using MiNET.Utils;
using SkyCore.Database;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Permissions
{

	public enum PlayerGroupCommandPermissions
	{
		
		Player = 1,
		Vip = 2,
		Pro = 3,
		Mvp = 4,
		Helper = 5,
		Mod = 6,
		Developer = 7,
		Youtuber = 8,
		Admin = 9

	}

    public class PlayerGroup : Enumeration
    {

	    public static readonly List<PlayerGroup> Values = new List<PlayerGroup>();

	    public static readonly PlayerGroup Player = new PlayerGroup(0, "Player", "§7", "§7",
		    PermissionLevel.Member, PlayerGroupCommandPermissions.Player, ActionPermissions.Default);

	    public static readonly PlayerGroup Vip = new PlayerGroup(1, "VIP", "§a", "§a[VIP]",
		    PermissionLevel.Member, PlayerGroupCommandPermissions.Vip, ActionPermissions.Default);

	    public static readonly PlayerGroup Pro = new PlayerGroup(2, "PRO", "§b", "§b[PRO]",
		    PermissionLevel.Member, PlayerGroupCommandPermissions.Pro, ActionPermissions.Default);

	    public static readonly PlayerGroup Mvp = new PlayerGroup(3, "MVP", "§d", "§d[MVP]",
		    PermissionLevel.Member, PlayerGroupCommandPermissions.Mvp, ActionPermissions.Default);

	    public static readonly PlayerGroup Helper = new PlayerGroup(4, "Helper", "§3", "§3[HELPER]",
		    PermissionLevel.Member, PlayerGroupCommandPermissions.Helper, ActionPermissions.Default);

	    public static readonly PlayerGroup Mod = new PlayerGroup(5, "Mod", "§6", "§6[MOD]",
		    PermissionLevel.Member, PlayerGroupCommandPermissions.Mod, ActionPermissions.Default);

	    public static readonly PlayerGroup Developer = new PlayerGroup(6, "Developer", "§e", "§e[DEVELOPER]",
		    PermissionLevel.Member, PlayerGroupCommandPermissions.Developer, ActionPermissions.Default);

	    public static readonly PlayerGroup Youtuber = new PlayerGroup(7, "Youtuber", "§c", "§c[YOUTUBE]",
		    PermissionLevel.Member, PlayerGroupCommandPermissions.Youtuber, ActionPermissions.Default);

	    public static readonly PlayerGroup Admin = new PlayerGroup(8, "Admin", "§c", "§c[ADMIN]",
		    PermissionLevel.Operator, PlayerGroupCommandPermissions.Admin, ActionPermissions.All);

		//

		static PlayerGroup()
		{
			RunnableTask.RunTask(() =>
			{
				new DatabaseAction().Query(
					"CREATE TABLE IF NOT EXISTS `player_groups` (\n" +
						"`player_xuid`       varchar(50),\n" +
						"`group_name`        varchar(50),\n" +
						" PRIMARY KEY(`player_xuid`)\n" +
					");",
					null, null, null);
			});
		}

		private int EnumVal { get; }

        public string GroupName { get; }

        public string GroupColour { get; }

        public string Prefix { get; }

	    public PermissionLevel PermissionLevel { get; }

		public PlayerGroupCommandPermissions CommandPermission { get; }

		public ActionPermissions ActionPermission { get; }

        private PlayerGroup(int enumVal, string groupName, string groupColour, string prefix,
			PermissionLevel permissionLevel, PlayerGroupCommandPermissions commandPermission, ActionPermissions actionPermission)
        {
            EnumVal = enumVal;

            GroupName = groupName;
            GroupColour = groupColour;
            Prefix = prefix;

	        CommandPermission = commandPermission;
            PermissionLevel = permissionLevel;
	        ActionPermission = actionPermission;

			Values.Add(this);
        }

        public int CompareTo(PlayerGroup playerGroup)
        {
            return EnumVal.CompareTo(playerGroup.EnumVal);
        }

        public bool IsAtLeast(PlayerGroup playerGroup)
        {
            return CompareTo(playerGroup) >= 0;
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

        private readonly SkyCoreAPI _skyCoreApi;

        public SkyPermissions(SkyCoreAPI skyCoreApi)
        {
            _skyCoreApi = skyCoreApi;
        }

        public PlayerGroup GetPlayerGroup(string playerName)
        {
            PlayerGroup playerGroup = PlayerGroup.Player;
	        if (_skyCoreApi.GetPlayer(playerName) is SkyPlayer player)
	        {
		        return player.PlayerGroup;
	        }

            return playerGroup;
        }

        [Command(Name = "perm set")]
        [Authorize(Permission = (int) PlayerGroupCommandPermissions.Admin)]
        public void CommandPermSet(MiNET.Player player, string targetName, string targetGroupName)
        {
			if (!(player is SkyPlayer skyPlayer) || !skyPlayer.PlayerGroup.IsAtLeast(PlayerGroup.Admin))
			{
				player.SendMessage("§c§l(!)§r §cYou do not have permission for this command.");
				return;
			}

			if (String.IsNullOrEmpty(targetName))
            {
                player.SendMessage($"{ChatColors.Red}Enter a valid player name.");
                return;
            }

            SkyPlayer target = _skyCoreApi.GetPlayer(targetName);

            if (target == null || !target.IsConnected)
            {
                player.SendMessage($"{ChatColors.Red}Target player is not online.");
                return;
            }

            //Format as our Enums
            targetGroupName = targetGroupName.ToLower();
            targetGroupName = Char.ToUpper(targetGroupName.ToCharArray()[0]) + targetGroupName.Substring(1);

	        if (!PlayerGroup.ValueOf(targetGroupName, out var targetGroup))
            {
                player.SendMessage($"{ChatColors.Red}Unrecognized group name '{targetGroupName}'.");
                string possibleGroups = "";
                foreach (PlayerGroup groupLoop in PlayerGroup.Values)
                {
                    possibleGroups += groupLoop.GroupName + ",";
                }

                player.SendMessage($"Possible Groups: {possibleGroups}");
                return;
            }

            target.SetPlayerGroup(targetGroup);

	        RunnableTask.RunTask(() =>
	        {
		        new DatabaseAction().Execute(
					"INSERT INTO `player_groups`\n" +
					"  (`player_xuid`, `group_name`)\n" +
			        "VALUES\n" +
					"  (@xuid, @group)\n" +
			        "ON DUPLICATE KEY UPDATE\n" +
					"  `player_xuid`    = VALUES(`player_xuid`),\n" +
					"  `group_name`     = VALUES(`group_name`);",
					(command) =>
			        {
				        command.Parameters.AddWithValue("@xuid", target.CertificateData.ExtraData.Xuid);
				        command.Parameters.AddWithValue("@group", targetGroup.GroupName);
			        },
			        new Action(delegate
			        {
						player.SendMessage($"{ChatColors.Yellow}Updated {target.Username}'s group to {targetGroup.GroupName}");
					})
		        );
	        });
        }

        [Command(Name = "perm get")]
		[Authorize(Permission = (int)PlayerGroupCommandPermissions.Admin)]
		public void CommandPermGet(MiNET.Player player, string targetName)
        {
			if (!(player is SkyPlayer skyPlayer) || !skyPlayer.PlayerGroup.IsAtLeast(PlayerGroup.Admin))
			{
				player.SendMessage("§c§l(!)§r §cYou do not have permission for this command.");
				return;
			}

			if (string.IsNullOrEmpty(targetName))
            {
                player.SendMessage($"{ChatColors.Red}Enter a valid player name.");
                return;
            }

            SkyPlayer target = _skyCoreApi.GetPlayer(targetName);

            if (target == null || !target.IsConnected)
            {
                player.SendMessage($"{ChatColors.Red}Target player is not online.");
                return;
            }

            player.SendMessage($"{ChatColors.Yellow}{target.Username} is currently rank '{((SkyPlayer)player).PlayerGroup.GroupName}'");
        }

    }
}
