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
using SkyCore.BugSnag;
using SkyCore.Database;
using SkyCore.Entities;
using SkyCore.Game;
using SkyCore.Game.Level;
using SkyCore.Game.State;
using SkyCore.Punishments;
using SkyCore.Statistics;
using SkyCore.Util;
using Bugsnag;
using MiNET.Blocks;
using SkyCore.Games.Hub;
using SkyCore.Server;

namespace SkyCore.Player
{
    public class SkyPlayer : MiNET.Player, IBugSnagMetadatable
    {

        public readonly SkyCoreAPI SkyCoreApi;

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
	        PermissionLevel = playerGroup.PermissionLevel;
            CommandPermission = playerGroup.CommandPermission;
	        ActionPermissions = playerGroup.ActionPermission;

	        SetHideNameTag(false);
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

	    public string GetNameTag(MiNET.Player player)
	    {
		    string username = player.Username;

		    string rank;
		    if (player is SkyPlayer skyPlayer)
		    {
			    rank = skyPlayer.PlayerGroup.Prefix;
		    }
		    else
		    {
			    rank = SkyCoreApi.Permissions.GetPlayerGroup(player.Username).Prefix;
		    }

		    if (rank.Length > 2)
		    {
			    rank += " ";
		    }

		    return $"{rank}{username}";
	    }

		public SkyPlayer(MiNetServer server, IPEndPoint endpoint, SkyCoreAPI skyCoreApi) : base(server, endpoint)
        {
            SkyCoreApi = skyCoreApi;

			Inventory = new SkyPlayerInventory(this);
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

	            if (Whitelist.IsEnabled() && !Whitelist.OnWhitelist(Username))
	            {
					Disconnect(Whitelist.GetWhitelistMessage());
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
							_postLoginActions.Clear();

							if (Username.Equals("OhBlihv") || Username.Equals("Donnas Wraps"))
							{
								if (PlayerGroup != PlayerGroup.Admin)
								{
									SetPlayerGroup(PlayerGroup.Admin);
									SkyUtil.log($"Overriding {Username}'s group to Admin");
								}
							}

							RunnableTask.RunTaskLater(() =>
							{
								SendTitle("§f", TitleType.Clear);
								SendTitle("§f", TitleType.AnimationTimes, 6, 6, 20 * 10);
								SendTitle("§f", TitleType.ActionBar, 6, 6, 20 * 10);
								SendTitle("§f", TitleType.Title, 6, 6, 20 * 10);
								SendTitle("§f", TitleType.SubTitle, 6, 6, 20 * 10);
							}, 500);
						})
					);
				});
	            
				//Initialize once we've loaded the group etc.
	            base.InitializePlayer();

				_hasJoined = true;

                IsSpawned = true;

	            //Should already be in a 'GameLevel'.
				//Check and force-spawn them in if they're missing.
	            if (Level is GameLevel level)
	            {
		            if (!level.PlayerTeamDict.ContainsKey(Username))
		            {
			            level.AddPlayer(this);
		            }
	            }

	            GameInfo targetedGame = ExternalGameHandler.GetGameForIncomingPlayer(Username);
	            if (targetedGame != null && (!(Level is GameLevel) || !((GameLevel) Level).GameId.Equals(targetedGame.GameId)))
	            {
		            SkyCoreApi.GameModes[SkyCoreApi.GameType].InstantQueuePlayer(this, targetedGame);
				}
			}
            catch (Exception e)
            {
				BugSnagUtil.ReportBug(this, e);
			}
        }

	    public void PopulateMetadata(Metadata metadata)
	    {
		    metadata.AddToTab("SkyPlayer", "Username", Username);
		    metadata.AddToTab("SkyPlayer", "XUID", CertificateData.ExtraData.Xuid);
		    metadata.AddToTab("SkyPlayer", "UUID", CertificateData.ExtraData.Identity);
		    metadata.AddToTab("SkyPlayer", "GameTeam", GameTeam);
		    metadata.AddToTab("SkyPlayer", "Game Name", (Level as GameLevel)?.GameType);
		    metadata.AddToTab("SkyPlayer", "Game Id", (Level as GameLevel)?.GameId);
			metadata.AddToTab("SkyPlayer", "Level Name", Level?.LevelName);
	    }

		//

		private bool _freeze;

	    public void Freeze(bool freeze)
	    {
		    _freeze = freeze;
		    SetNoAi(freeze);
		}

		public override void BroadcastSetEntityData()
		{
			if (IsGameSpectator)
			{
				McpeSetEntityData mcpeSetEntityData = McpeSetEntityData.CreateObject();
				mcpeSetEntityData.runtimeEntityId = EntityId;
				mcpeSetEntityData.metadata = GetMetadata();
				mcpeSetEntityData.metadata[(int)Entity.MetadataFlags.Scale] = new MetadataFloat(0.01f); // Scale

				foreach (var gamePlayer in Level.GetAllPlayers())
				{
					if (gamePlayer == this)
					{
						continue;
					}

					gamePlayer.SendPackage(mcpeSetEntityData);
				}

				McpeSetEntityData selfSetEntityData = McpeSetEntityData.CreateObject();
				selfSetEntityData.runtimeEntityId = EntityId;
				selfSetEntityData.metadata = GetMetadata();

				SendPackage(selfSetEntityData);
			}
			else
			{
				base.BroadcastSetEntityData();
			}
		}

	    public override void SendEquipmentForPlayer(MiNET.Player[] receivers)
	    {
		    McpeMobEquipment mcpePlayerEquipment = McpeMobEquipment.CreateObject();
		    mcpePlayerEquipment.runtimeEntityId = EntityId;
		    mcpePlayerEquipment.item = IsGameSpectator ? new ItemAir() : Inventory.GetItemInHand();
		    mcpePlayerEquipment.slot = 0;
		    Level.RelayBroadcast(this, receivers, mcpePlayerEquipment);
	    }

	    public override void HandleMcpeMovePlayer(McpeMovePlayer message)
	    {
		    if (_freeze)
		    {
			    if (Math.Abs(message.x - KnownPosition.X) > 0.1 ||
			        Math.Abs(message.z - KnownPosition.Z) > 0.1)
			    {
				    Teleport(KnownPosition);
				    return;
			    }
		    }

		    base.HandleMcpeMovePlayer(message);
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
			    if (level.DoInteractAtEntity(message.actionId, this, null))
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
			    {
					Entity target = Level.GetEntity(transaction.EntityId);

					if (HandleInteract((byte)transaction.ActionType, target))
				    {
					    return;
				    }
				    break;
				}
		    }

		    base.HandleTransactionItemUseOnEntity(transaction);
	    }

		protected override void HandleTransactions(Transaction transaction)
	    {
		    foreach (var record in transaction.Transactions)
		    {
			    if (record is ContainerTransactionRecord)
			    {
				    if (Level is GameLevel level)
				    {
					    if (level.CurrentState.HandleInventoryModification(this, level, record))
					    {
							SendPlayerInventory(); //Reset their inventory to what we have tracked
						    return;
					    }
				    }
			    }
			    else if (record is WorldInteractionTransactionRecord)
			    {
				    //Drop
				    if (record.Slot == 0)
				    {
					    if (Level is GameLevel level && level.DropItem(this, record.NewItem))
					    {
							SendPlayerInventory(); //Reset inventory to what is tracked
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
			Block clickedBlock;
			if (Level is GameLevel level && level.DoInteractAtBlock(2, this, clickedBlock = Level.GetBlock(new BlockCoordinates(transaction.Position)))) //'Right Click'
		    {
				switch (clickedBlock.Id)
			    {
					//Single-Block Interactables (Visible)
				    case 92: //Cake
				    {
						//Instantly reset player hunger
					    HungerManager.SendHungerAttributes();

						goto case 107; //Dirty case fall-through
				    }

					case 107:   //Fence Gate
					case 183:   //Fence Gate (Spruce)
					case 184:   //Fence Gate (Birch)
					case 185:   //Fence Gate (Jungle)
					case 186:   //Fence Gate (Dark Oak)
					case 187:	//Fence Gate (Acacia)
				    case 69:	//Lever
				    case 149:	//Comparator (Unpowered)
				    case 150:	//Comparator (Powered)
				    case 93:	//Repeater (Unpowered)
				    case 94:    //Repeater (Powered)
					case 96:    //Trap Door (Wood)
					//case 167:	//Trap Door (Iron)
					{
						var blockUpdate = McpeUpdateBlock.CreateObject();
					    blockUpdate.blockId = clickedBlock.Id;
					    blockUpdate.coordinates = clickedBlock.Coordinates;
					    blockUpdate.blockMetaAndPriority = (byte)(0xb << 4 | (clickedBlock.Metadata & 0xf));

						SendPackage(blockUpdate);
					    return;
				    }

					//Single-Block Interactables (Block)
					/*case 70:	//Stone Pressure Plate
					case 72:    //Wood Pressure Plate
					case 147:   //Light (Gold) Pressure Plate
					case 148:   //Heavy (Iron) Pressure Plate*/
				    case 77:	//Stone Button
				    case 143:   //Wood Button
					case 199:	//Item Frame
				    {
					    return; //Ignore Handling
				    }

					//Containers (Block)
				    case 54:  //Chest
					case 146: //Trapped Chest
				    case 61:  //Furnace (Unlit)
				    case 62:  //Furnace (Lit)
				    case 117: //Brewing Stand
				    case 130: //EnderChest
				    case 138: //Beacon
				    case 125: //Dropper
				    case 23:  //Dispenser
				    case 25:  //Noteblock
				    case 84:  //Jukebox
				    case 58:  //Crafting Bench
					case 145: // Anvil
				    {
					    return; //Ignore handling
				    }

					//Doors
					case 64:	//Door (Wood)
					//case 71:   //Door (Iron)
				    case 193:
				    case 194:
				    case 195:
				    case 196:
				    case 197:
				    {
					    Block upperHalf, lowerHalf;

					    //Lower Half
					    if ((clickedBlock.Metadata | 0x8) == 0)
					    {
						    lowerHalf = clickedBlock;

						    BlockCoordinates otherHalfCoordinates = new BlockCoordinates(transaction.Position);
						    otherHalfCoordinates.Y += 1;

						    upperHalf = Level.GetBlock(otherHalfCoordinates);
					    }
					    //Upper Half
					    else
					    {
						    upperHalf = clickedBlock;

						    BlockCoordinates otherHalfCoordinates = new BlockCoordinates(transaction.Position);
						    otherHalfCoordinates.Y -= 1;

						    lowerHalf = Level.GetBlock(otherHalfCoordinates);
					    }

					    var lowerHalfUpdate = McpeUpdateBlock.CreateObject();
					    lowerHalfUpdate.blockId = lowerHalf.Id;
					    lowerHalfUpdate.coordinates = lowerHalf.Coordinates;
					    lowerHalfUpdate.blockMetaAndPriority = (byte)(0xb << 4 | (lowerHalf.Metadata & 0xf));

					    var upperHalfUpdate = McpeUpdateBlock.CreateObject();
					    upperHalfUpdate.blockId = upperHalf.Id;
					    upperHalfUpdate.coordinates = upperHalf.Coordinates;
					    upperHalfUpdate.blockMetaAndPriority = (byte)(0xb << 4 | (upperHalf.Metadata & 0xf));

					    SendPackage(lowerHalfUpdate);

					    SendPackage(upperHalfUpdate);
					    return; //Avoid base functionality
				    }
			    }
			}

			base.HandleTransactionItemUse(transaction);
		}

		public bool HandleInteract(byte actionId, Entity target)
        {
            if (actionId == 4)
            {
                return false;
            }

            //SkyUtil.log($"Interact Id:{actionId} ({Username})");

            if (target is PlayerNPC npc)
            {
                if (actionId == 1 || actionId == 2)
                {
                    //SkyUtil.log($"Processing NPC Interact as {Username}");
                    npc.OnInteract(this);
	                return true;
                }
            }
            else
            {
                if (Level is GameLevel level)
                {
					//return level.DoInteractAtEntity(actionId, this, (SkyPlayer) target);
					level.DoInteractAtEntity(actionId, this, (SkyPlayer)target);

	                return true;
                }
            }

	        return false;
        }

	    public virtual void UpdateGameMode(GameMode gameMode, bool allowBreakingIfCreative = false)
	    {
		    SetGameMode(gameMode);

		    if (gameMode == GameMode.Creative && allowBreakingIfCreative)
		    {
				IsWorldImmutable = false; //Allow breaking
			    //IsWorldBuilder = true;
			    SendAdventureSettings();
			}
		    else if(allowBreakingIfCreative)
		    {
				IsWorldImmutable = true; //Prevent breaking
			    //IsWorldBuilder = false;
			    SendAdventureSettings();
			}
		}

	    protected override void OnPlayerLeave(PlayerEventArgs e)
	    {
		    BarHandler?.Clear();

		    base.OnPlayerLeave(e);
	    }

	    public void HandleHeldItemSlotChange(int newHeldSlot)
	    {
		    if (Level is GameLevel level)
		    {
			    level.CurrentState.HandleHeldItemSlotChange(level, this, newHeldSlot);
		    }
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

	        if (toLevel is HubLevel)
	        {
		        useLoadingScreen = false; //Always disable when entering the server
	        }

	        base.SpawnLevel(toLevel, spawnPoint, useLoadingScreen, levelFunc, postSpawnAction);
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
