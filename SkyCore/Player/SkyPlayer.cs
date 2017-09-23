using System.Net;
using MiNET;
using MiNET.Net;
using SkyCore.Commands;
using SkyCore.Permissions;
using System;
using System.Runtime.Serialization;
using System.Threading;
using MiNET.Utils;
using log4net;
using MiNET.Items;
using MiNET.Worlds;
using SkyCore.Database;
using SkyCore.Entities;
using SkyCore.Game;
using SkyCore.Util;

namespace SkyCore.Player
{
    public class SkyPlayer : MiNET.Player
    {

        public SkyCoreAPI SkyCoreApi;

        public PlayerGroup PlayerGroup { get; set; }

		public bool IsGameSpectator { get; set; }

		public BarHandler BarHandler { get; private set; }

        public void SetPlayerGroup(PlayerGroup playerGroup)
        {
            PlayerGroup = playerGroup;

            //Initialize Player UserPermission level for commands
            CommadPermission = playerGroup.PermissionLevel;

	        string prefix = PlayerGroup.Prefix;
	        if (prefix.Length > 2)
	        {
		        prefix += " ";
	        }

	        SetDisplayName(prefix + Username);
	        SetNameTag(prefix + Username);
	        this.SetHideNameTag(false);

	        SkyUtil.log($"Set {Username}'s name to {DisplayName}");
		}

        public SkyPlayer(MiNetServer server, IPEndPoint endpoint, SkyCoreAPI skyCoreApi) : base(server, endpoint)
        {
            this.SkyCoreApi = skyCoreApi;
        }

        private bool _hasJoined = false;

        public override void InitializePlayer()
        {
			SkyUtil.log("a");
            if (_hasJoined)
            {
                return;
            }

            try
            {
                SkyUtil.log("Initialising Player");

                if (CertificateData.ExtraData.Xuid == null)
                {
                    Disconnect("§aXBOX §caccount required to login.");
	                SkyUtil.log("no xbox account");
					return;
                }

	            SkyUtil.log("adding bar handler");

				BarHandler = new BarHandler(this);

	            SkyUtil.log("set bar handler");

				SetPlayerGroup(PlayerGroup.Player);

	            SkyUtil.log("set player group and bar handler");

				RunnableTask.RunTask(() =>
				{
					SkyUtil.log("starting permission sql");
					new DatabaseAction().Query(
						"SELECT `group_name` FROM player_groups WHERE `player_xuid`=@id",
						(command) =>
						{
							command.Parameters.AddWithValue("@id", CertificateData.ExtraData.Xuid);
						},
						(reader) =>
						{
							PlayerGroup playerGroup;
							PlayerGroup.ValueOf(reader.GetString(0), out playerGroup);

							if (playerGroup == null)
							{
								playerGroup = PlayerGroup.Player;
							}

							SetPlayerGroup(playerGroup);
						},
						new Action(delegate
						{
							SkyUtil.log($"Initialized as {PlayerGroup.GroupName}({CommadPermission})");

							if (PlayerGroup == PlayerGroup.Admin)
							{
								//SetGameMode(GameMode.Creative);
								//UseCreativeInventory = true;
								SetGameMode(GameMode.Adventure);
							}
							else
							{
								SetGameMode(GameMode.Adventure);
								UseCreativeInventory = false;
							}

							/*if (Username.Equals("OhBlihv"))
							{
								setPlayerGroup(PlayerGroup.Admin);
								SkyUtil.log("Overriding OhBlihv's group to Admin");
							}*/
						})
					);
				});

                SkyUtil.log($"Pre-Initialized as {PlayerGroup.GroupName}({CommadPermission})");

				//Initialize once we've loaded the group etc.
	            base.InitializePlayer();

				//Scale = 2.0D;

				_hasJoined = true;

                IsSpawned = true;

	            try
	            {
		            //Add this player to any games if available and if this is the only game available
		            if (!SkyCoreApi.GameType.Equals("hub") && SkyCoreAPI.Instance.GameModes.Count == 1)
		            {
			            //Foreach, but only one value.
			            foreach (CoreGameController coreGameController in SkyCoreAPI.Instance.GameModes.Values)
			            {
				            coreGameController.InstantQueuePlayer(this);
				            break;
			            }
		            }
	            }
	            catch (Exception e)
	            {
		            Console.WriteLine(e);
	            }
			}
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public override void HandleMcpeAnimate(McpeAnimate message)
        {
            if (message.actionId != 1)
            {
                base.HandleMcpeAnimate(message);
                return;
            }

            SkyUtil.log($"Animate Id:{message.actionId} ({Username})");

            if (Level is GameLevel && ((GameLevel) Level).DoInteract(message.actionId, this, null))
            {
                //return; //Avoid default handling
            }

            base.HandleMcpeAnimate(message);
        }

        public override void HandleMcpeInteract(McpeInteract message)
        {
            if (message.actionId == 4)
            {
                base.HandleMcpeInteract(message);
                return;
            }

            SkyUtil.log($"Interact Id:{message.actionId} ({Username})");
            MiNET.Entities.Entity target = Level.GetEntity(message.targetRuntimeEntityId);

            if (target is PlayerNPC)
            {
                if (message.actionId == 1 || message.actionId == 2)
                {
                    SkyUtil.log($"Processing NPC Interact as {Username}");
                    (target as PlayerNPC).OnInteract(this);
                }
            }
            else
            {
                if (Level is GameLevel && ((GameLevel) Level).DoInteract(message.actionId, this, (SkyPlayer) target))
                {
                    //return; //Avoid default handling
                }
            }

            base.HandleMcpeInteract(message);
        }

	    protected override void OnPlayerLeave(PlayerEventArgs e)
	    {
			BarHandler.Clear();

		    base.OnPlayerLeave(e);
	    }

		public override void SpawnLevel(Level toLevel, PlayerLocation spawnPoint, bool useLoadingScreen = false, Func<Level> levelFunc = null, Action postSpawnAction = null)
        {
            for (int i = 0; i < Inventory.Slots.Count; ++i)
            {
                if (Inventory.Slots[i] == null || Inventory.Slots[i].Id != 0) Inventory.Slots[i] = new ItemAir();
            }

            if (Inventory.Helmet.Id != 0) Inventory.Helmet = new ItemAir();
            if (Inventory.Chest.Id != 0) Inventory.Chest = new ItemAir();
            if (Inventory.Leggings.Id != 0) Inventory.Leggings = new ItemAir();
            if (Inventory.Boots.Id != 0) Inventory.Boots = new ItemAir();
            AllowFly = false;
            IsFlying = false;
            IsSpectator = false;

            base.SpawnLevel(toLevel, spawnPoint, useLoadingScreen, levelFunc);
        }

    }

}
