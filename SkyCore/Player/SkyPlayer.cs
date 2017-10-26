using System.Net;
using MiNET;
using MiNET.Net;
using SkyCore.Permissions;
using System;
using System.Collections.Generic;
using MiNET.Utils;
using MiNET.Entities;
using MiNET.Items;
using MiNET.Worlds;
using SkyCore.Database;
using SkyCore.Entities;
using SkyCore.Game;
using SkyCore.Game.Level;
using SkyCore.Game.State;
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
            SkyCoreApi = skyCoreApi;
        }

        private bool _hasJoined;
	    private bool _isRankLoaded;

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

	            switch (Username.ToLower())
	            {
					case "donnas wraps":
					case "ohblihv":
					case "ohblihv2":
					case "erictigerawr":
						break;
					default:
						Disconnect("§7What could this be...?");
						return;
	            }

				StatisticsCore.AddPlayer(CertificateData.ExtraData.Xuid, Username);

				//Sync retrieve any active punishments
	            PlayerPunishments playerPunishments = PunishCore.GetPunishmentsFor(CertificateData.ExtraData.Xuid);
	            Punishment activePunishment = playerPunishments.GetActive(PunishmentType.Ban);
				if (activePunishment != null)
	            {
		            Disconnect("§cYou are currently banned from the §dSkytonia §eNetwork\n" +
		                       $"§c({PunishmentMessages.GetNeatExpiryForPunishment(activePunishment)})\n" +
		                       $"§cReason: {activePunishment.PunishReason}");
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
							PlayerGroup.ValueOf(reader.GetString(0), out var playerGroup);

							if (playerGroup == null)
							{
								playerGroup = PlayerGroup.Player;
							}

							SetPlayerGroup(playerGroup);
						},
						new Action(delegate
						{
							_isRankLoaded = true;
							//SkyUtil.log($"Initialized as {PlayerGroup.GroupName}({CommandPermission})");

							if (PlayerGroup == PlayerGroup.Admin)
							{
								SetGameMode(GameMode.Adventure);
							}
							else
							{
								SetGameMode(GameMode.Adventure);

								if (SkyCoreApi.GameType.Equals("hub") && PlayerGroup.IsAtLeast(PlayerGroup.Mvp))
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

                //SkyUtil.log($"Pre-Initialized as {PlayerGroup.GroupName}({CommandPermission})");

				//Initialize once we've loaded the group etc.
	            base.InitializePlayer();

				_hasJoined = true;

                IsSpawned = true;

	            //SkyUtil.log("Game Count: " + SkyCoreAPI.Instance.GameModes.Count);
				//Add this player to any games if available and if this is the only game available
	            if (SkyCoreAPI.Instance.GameModes.Count == 1)
	            {
		            GameInfo targetedGame = ExternalGameHandler.GetGameForIncomingPlayer(Username);
		            
		            //Foreach, but only one value.
		            foreach (CoreGameController coreGameController in SkyCoreAPI.Instance.GameModes.Values)
		            {
			            //SkyUtil.log("Queueing for " + coreGameController.GameName + " In " + (targetedGame == null ? "nothing specific" : $"GameId:{targetedGame.GameId}"));
						if (targetedGame != null)
						{
							coreGameController.InstantQueuePlayer(this, targetedGame);
						}
						else
						{
							coreGameController.InstantQueuePlayer(this);
						}
			            break;
		            }
	            }
			}
            catch (Exception e)
            {
                Console.WriteLine(e);
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

			if (Level is GameLevel level)
			{
				if (level.DoInteract(message.actionId, this, null))
				{
					//return; //Avoid default handling
				}
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

		protected override void HandleTransactions(Transaction transaction)
	    {
		    foreach (var record in transaction.Transactions)
		    {
			    if (record is WorldInteractionTransactionRecord)
			    {
				    //Drop
				    if (record.Slot == 0)
				    {
					    if (Level is GameLevel level && level.DropItem(this, record.NewItem))
					    {
						    return; //Avoid default handling
					    }
				    }
				    //Pickup
				    else if (record.Slot == 1)
				    {
					    if (Level is GameLevel level && level.PickupItem(this, record.NewItem))
					    {
						    return; //Avoid default handling
					    }
				    }
			    }
		    }

		    base.HandleTransactions(transaction);
	    }

	    protected override void HandleTransactionItemUse(Transaction transaction)
	    {
		    HandleInteract(2, null); //'Right Click'

		    base.HandleTransactionItemUse(transaction);
	    }

		public virtual void HandleInteract(byte actionId, Entity target)
        {
            if (actionId == 4)
            {
                return;
            }

            SkyUtil.log($"Interact Id:{actionId} ({Username})");

            if (target is PlayerNPC npc)
            {
                if (actionId == 1 || actionId == 2)
                {
                    //SkyUtil.log($"Processing NPC Interact as {Username}");
                    npc.OnInteract(this);
                }
            }
            else
            {
                if (Level is GameLevel level)
                {
	                if (level.DoInteract(actionId, this, (SkyPlayer) target))
	                {
						//return; //Avoid default handling
					}
				}
            }
        }

	    public virtual void UpdateGameMode(GameMode gameMode, bool allowBreakingIfCreative = false)
	    {
		    SetGameMode(gameMode);

		    if (gameMode == GameMode.Creative && allowBreakingIfCreative)
		    {
				IsWorldImmutable = false; //Allow breaking
			    IsWorldBuilder = true;
			    SendAdventureSettings();
			}
		    else if(allowBreakingIfCreative)
		    {
				IsWorldImmutable = true; //Prevent breaking
			    IsWorldBuilder = false;
			    SendAdventureSettings();
			}
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

	        base.SpawnLevel(toLevel, spawnPoint, false, levelFunc);
        }

	    public override void HandleMcpeServerSettingsRequest(McpeServerSettingsRequest message)
		{
			/*SkyUtil.log("Replying with Skytonia settings");
			CustomForm customForm1 = new CustomForm {Title = "Skytonia Settings"};
			McpeServerSettingsResponse settingsResponse = Package<McpeServerSettingsResponse>.CreateObject(1L);
			settingsResponse.formId = 12345L;
			settingsResponse.data = customForm1.ToJson();
			this.SendPackage((Package)settingsResponse);*/
		}

		public override string ToString()
	    {
		    return $"SkyPlayer: {Username}";
	    }

    }

}
