using System.Net;
using MiNET;
using MiNET.Net;
using SkyCore.Commands;
using SkyCore.Permissions;
using System;
using System.Collections.Generic;
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

		private List<Action> _postLoginActions = new List<Action>();

	    public void AddPostLoginTask(Action action)
	    {
		    if (_isRankLoaded)
		    {
			    action.Invoke();
		    }
		    else
		    {
			    _postLoginActions.Add(action);
		    }
	    }


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
	    private bool _isRankLoaded = false;

        public override void InitializePlayer()
        {
            if (_hasJoined)
            {
                return;
            }

            try
            {
                if (CertificateData.ExtraData.Xuid == null)
                {
                    Disconnect("§cAn §2§lXBOX§r §caccount required to login to §dSkytonia §eNetwork");
					return;
                }

				BarHandler = new BarHandler(this);

				SetPlayerGroup(PlayerGroup.Player);

				RunnableTask.RunTask(() =>
				{
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
							_isRankLoaded = true;
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

							foreach (Action action in _postLoginActions)
							{
								action.Invoke();
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

		public bool Freeze { get; set; }

	    public override void HandleMcpeMovePlayer(McpeMovePlayer message)
	    {
		    if (Freeze)
		    {
				//Allow players to fall
			    if (Math.Abs(message.x - KnownPosition.X) > 0.1 || Math.Abs(message.x - KnownPosition.Z) > 0.1)
			    {
					SendMovePlayer(true);
				    return;
				}
		    }

		    base.HandleMcpeMovePlayer(message);
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
