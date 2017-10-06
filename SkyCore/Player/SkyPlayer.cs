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
using MiNET.Entities;
using MiNET.Items;
using MiNET.Worlds;
using SkyCore.Database;
using SkyCore.Entities;
using SkyCore.Game;
using SkyCore.Game.State;
using SkyCore.Games.Hub;
using SkyCore.Punishments;
using SkyCore.Statistics;
using SkyCore.Util;

namespace SkyCore.Player
{
    public class SkyPlayer : MiNET.Player
    {

        public SkyCoreAPI SkyCoreApi;

        public PlayerGroup PlayerGroup { get; set; }

		public bool IsGameSpectator { get; set; }

		public BarHandler BarHandler { get; private set; }

		private readonly List<Action> _postLoginActions = new List<Action>();

		//Game Settings
			
		public GameTeam GameTeam { get; set; }

		//

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
            CommandPermission = playerGroup.PermissionLevel;

	        this.SetHideNameTag(false);
			UpdatePlayerName();

	        SkyUtil.log($"Set {Username}'s name to {DisplayName}");
		}

	    public void UpdatePlayerName()
	    {
			string prefix = PlayerGroup.Prefix;
		    if (prefix.Length > 2)
		    {
			    prefix += " ";
		    }

		    SetDisplayName(prefix + Username);
		    SetNameTag(prefix + Username);
		}

	    public void SetNameTagVisibility(bool visible)
	    {
		    if (visible)
		    {
			    if (string.IsNullOrEmpty(NameTag))
			    {
				    UpdatePlayerName();
				}
		    }
		    else
		    {
			    if (!string.IsNullOrEmpty(NameTag))
			    {
					SetDisplayName("");
				    SetNameTag("");
				}
			}
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

				StatisticsCore.AddPlayer(CertificateData.ExtraData.Xuid, Username);

				//Sync retrieve any active punishments
	            PlayerPunishments playerPunishments = PunishCore.GetPunishmentsFor(CertificateData.ExtraData.Xuid);
	            Punishment activePunishment = playerPunishments.GetActive(PunishmentType.Ban);
				if (activePunishment != null)
	            {
					string expiryString = "";
		            if (activePunishment.DurationUnit != DurationUnit.Permanent)
		            {
						TimeSpan expiryTime = activePunishment.Expiry.Subtract(DateTime.Now);

			            if (expiryTime.Days > 0)
			            {
				            expiryString += expiryTime.Days + " Days";
			            }
			            if (expiryTime.Hours > 0)
			            {
				            if (expiryString.Length > 0)
				            {
					            expiryString += " ";
				            }

				            expiryString += expiryTime.Hours + " Hours";
			            }
			            if (expiryTime.Minutes > 0)
			            {
				            if (expiryString.Length > 0)
				            {
					            expiryString += " ";
				            }

				            expiryString += expiryTime.Minutes + " Minutes";
			            }
			            if (expiryTime.Seconds > 0)
			            {
				            if (expiryString.Length > 0)
				            {
					            expiryString += " ";
				            }

				            expiryString += expiryTime.Seconds + " Seconds";
			            }

			            expiryString += $" Remaining";
		            }
		            else
		            {
			            expiryString = "Permanent";
		            }

					Disconnect("§cYou are currently banned from the §dSkytonia §eNetwork\n" +
							   $"§c({expiryString})\n" + 
							   $"§cReason: {activePunishment.PunishReason}");
		            return;
				}
	            else
	            {
		            SkyUtil.log($"{Username} has no current bans, and is allowed to connect.");
		            foreach (PunishmentType punishmentType in playerPunishments.Punishments.Keys)
		            {
			            foreach (Punishment punishment in playerPunishments.Punishments[punishmentType])
			            {
				            SkyUtil.log(punishment.ToString());
			            }
		            }
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
							SkyUtil.log($"Initialized as {PlayerGroup.GroupName}({CommandPermission})");

							if (PlayerGroup == PlayerGroup.Admin)
							{
								SetGameMode(GameMode.Adventure);
							}
							else
							{
								SetGameMode(GameMode.Adventure);

								if (SkyCoreApi.GameType.Equals("hub") && PlayerGroup.isAtLeast(PlayerGroup.Mvp))
								{
									SetAllowFly(true);
								}
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

                SkyUtil.log($"Pre-Initialized as {PlayerGroup.GroupName}({CommandPermission})");

				//Initialize once we've loaded the group etc.
	            base.InitializePlayer();

				//Scale = 2.0D;

				_hasJoined = true;

                IsSpawned = true;

	            try
	            {
		            //Add this player to any games if available and if this is the only game available
		            if (!SkyCoreApi.GameType.Equals("hub") && SkyCoreAPI.Instance.GameModes.Count <= 2)
		            {
			            //Foreach, but only one value.
			            foreach (CoreGameController coreGameController in SkyCoreAPI.Instance.GameModes.Values)
			            {
				            if (coreGameController is HubCoreController)
				            {
					            continue;
				            }

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

            //SkyUtil.log($"Animate Id:{message.actionId} ({Username})");

            if (Level is GameLevel level && level.DoInteract(message.actionId, this, null))
            {
                //return; //Avoid default handling
            }

            base.HandleMcpeAnimate(message);
        }

	    protected override void HandleTransactionItemUseOnEntity(Transaction transaction)
	    {
		    switch ((McpeInventoryTransaction.ItemUseOnEntityAction)transaction.ActionType)
		    {
			    case McpeInventoryTransaction.ItemUseOnEntityAction.Interact: // Right click
				case McpeInventoryTransaction.ItemUseOnEntityAction.Attack:
				    Entity target = Level.GetEntity(transaction.EntityId);
				    
					HandleInteract((byte) transaction.ActionType, target);
					break;
		    }

		    base.HandleTransactionItemUseOnEntity(transaction);
	    }

		public virtual void HandleInteract(byte actionId, Entity target)
        {
            if (actionId == 4)
            {
                return;
            }

            SkyUtil.log($"Interact Id:{actionId} ({Username})");

            if (target is PlayerNPC)
            {
                if (actionId == 1 || actionId == 2)
                {
                    //SkyUtil.log($"Processing NPC Interact as {Username}");
                    ((PlayerNPC) target).OnInteract(this);
                }
            }
            else
            {
                if (Level is GameLevel level && level.DoInteract(actionId, this, (SkyPlayer) target))
                {
                    //return; //Avoid default handling
                }
            }

            //base.HandleMcpeInteract(message);
        }

	    protected override void OnPlayerLeave(PlayerEventArgs e)
	    {
		    BarHandler?.Clear();

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

	    public override string ToString()
	    {
		    return $"SkyPlayer: {Username}";
	    }

    }

}
