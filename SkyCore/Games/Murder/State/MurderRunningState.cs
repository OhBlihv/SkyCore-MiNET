using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using MiNET;
using MiNET.Effects;
using MiNET.Entities;
using MiNET.Items;
using MiNET.Net;
using MiNET.Particles;
using MiNET.Utils;
using MiNET.Worlds;
using SkyCore.Game.Items;
using SkyCore.Game.Level;
using SkyCore.Game.State;
using SkyCore.Game.State.Impl;
using SkyCore.Games.BuildBattle.State;
using SkyCore.Games.Hub;
using SkyCore.Games.Murder.Entities;
using SkyCore.Games.Murder.Items;
using SkyCore.Games.Murder.Level;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Games.Murder.State
{

	class MurderTickableInformation : ITickableInformation
	{
		
		public string NeatTimeRemaining { get; set; }

	}

    class MurderRunningState : RunningState, IMessageTickableState
    {

	    public const int PreStartTime = 10;

	    public const int MaxGunParts = 5;
		public const int MaxGunAmmo = 5;

        private static readonly List<PlayerLocation> GunPartLocations = new List<PlayerLocation>();
        private static readonly List<PlayerLocation> PlayerSpawnLocations = new List<PlayerLocation>();

        public readonly Dictionary<PlayerLocation, MurderGunPartEntity> GunParts = new Dictionary<PlayerLocation, MurderGunPartEntity>();

        public readonly Dictionary<string, int> PlayerGunPartCounts = new Dictionary<string, int>();
        public readonly Dictionary<string, int> PlayerAmmoCounts = new Dictionary<string, int>();

	    public MurderRunningState()
	    {
		    MaxGameTime = 300 * 2;
	    }

		public override void EnterState(GameLevel gameLevel)
        {
            GunPartLocations.Clear();
            PlayerSpawnLocations.Clear();

            GunPartLocations.AddRange(((MurderLevelInfo) ((MurderLevel) gameLevel).GameLevelInfo).GunPartLocations);
            PlayerSpawnLocations.AddRange(((MurderLevelInfo)((MurderLevel)gameLevel).GameLevelInfo).PlayerSpawnLocations);

            while (PlayerSpawnLocations.Count < gameLevel.GetMaxPlayers())
            {
                PlayerSpawnLocations.Add(((MurderLevelInfo)((MurderLevel)gameLevel).GameLevelInfo).PlayerSpawnLocations[0]);
            }

	        EndTick = gameLevel.Tick + MaxGameTime + PreStartTime;

            try
            {
				RunnableTask.RunTask(() =>
	            {
					//Create new collection due to iterating over a live list
		            ICollection<SkyPlayer> players = gameLevel.GetAllPlayers();
		            foreach (SkyPlayer player in players)
		            {
			            player.SetEffect(new Blindness{Duration = 80,Particles = false}); //Should be 3 seconds?

						player.SetHideNameTag(true);
						player.IsAlwaysShowName = false;
			            player.SetNameTagVisibility(false);
					}

		            List<PlayerLocation> usedSpawnLocations = new List<PlayerLocation>();
		            gameLevel.DoForAllPlayers(player =>
		            {
			            //Pre-add all players to the map to avoid any unnecessary contains
			            PlayerAmmoCounts.Add(player.Username, 0);
			            PlayerGunPartCounts.Add(player.Username, 0);

			            player.SetGameMode(GameMode.Adventure);

			            //Avoid spawning two players in the same location
			            PlayerLocation spawnLocation;
			            while (usedSpawnLocations.Contains((spawnLocation = PlayerSpawnLocations[Random.Next(PlayerSpawnLocations.Count)])))
			            {
				            //
			            }

			            usedSpawnLocations.Add(spawnLocation);

			            player.Teleport(spawnLocation);

						player.SetHideNameTag(true);
			            player.IsAlwaysShowName = false;
			            player.SetNameTagVisibility(false);

						player.Freeze(true);

						player.HungerManager.Hunger = 6; //Set food to 'unable to run' level.
						player.SendUpdateAttributes(); //TODO: Not required? Or is this required for Hunger
					});

					List<MurderTeam> teamRotation = new List<MurderTeam> { MurderTeam.Murderer, MurderTeam.Detective, MurderTeam.Innocent };
					int offset = Random.Next(teamRotation.Count);
		            for (int i = 0; i < 12; i++)
		            {
			            MurderTeam team = teamRotation[(offset + i) % 3];
						foreach (SkyPlayer player in players)
						{
							TitleUtil.SendCenteredSubtitle(player, team.TeamPrefix + "§l" + team.TeamName);

							//Poorly enforce speed
							if (i == 0 || i == 11)
							{
								player.Freeze(true);
								player.SetHideNameTag(true);
								player.IsAlwaysShowName = false;
								player.SetNameTagVisibility(false);
							}
						}

						//SkyUtil.log($"Printed scroll {i}/12, with {team.TeamPrefix + "§l" + team.TeamName}");
			            Thread.Sleep(250);
		            }

					int murdererIdx = Random.Next(players.Count),
						detectiveIdx = 0;

					int idx = 0;
					while (++idx < 50 && (detectiveIdx = Random.Next(players.Count)) == murdererIdx)
					{
						//
					}

					Console.WriteLine($"Rolled Murderer as {murdererIdx} Detective as {detectiveIdx} with 0-{players.Count - 1} possible indexes");

					idx = 0;
					foreach (SkyPlayer player in players)
					{
						if (idx == murdererIdx)
						{
							gameLevel.SetPlayerTeam(player, MurderTeam.Murderer);
						}
						else if (idx == detectiveIdx)
						{
							gameLevel.SetPlayerTeam(player, MurderTeam.Detective);
						}
						else
						{
							gameLevel.SetPlayerTeam(player, MurderTeam.Innocent);
						}

						idx++;
					}

					//Workaround for one player (single murderer)
		            if (((MurderLevel) gameLevel).Detective == null)
		            {
			            ((MurderLevel) gameLevel).Detective = ((MurderLevel) gameLevel).Murderer;
		            }

					gameLevel.DoForPlayersIn(player =>
					{
						TitleUtil.SendCenteredSubtitle(player, "§a§lInnocent §r\n§7Track down the murderer!");
					}, MurderTeam.Innocent);

					gameLevel.DoForPlayersIn(player =>
					{
						TitleUtil.SendCenteredSubtitle(player, "§9§lDetective §r\n§7Track down the murderer!");

						player.Inventory.SetInventorySlot(0, new ItemInnocentGun());
						//SkyUtil.log($"In Slot 0 = {player.Inventory.GetSlots()[0].GetType().FullName}");
						player.Inventory.SetInventorySlot(9, new ItemArrow());

						PlayerAmmoCounts[player.Username] = int.MaxValue;
					}, MurderTeam.Detective);

		            gameLevel.DoForPlayersIn(InitializeMurderer, MurderTeam.Murderer);

					gameLevel.DoForAllPlayers(player =>
					{
						player.SendAdventureSettings();

						player.Freeze(false);

						//Ensure this player is at the correct spawn location
						if (gameLevel.GetBlock(player.KnownPosition).Id != 0)
						{
							PlayerLocation newLocation = (PlayerLocation) player.KnownPosition.Clone();
							newLocation.Y++;
						
							player.Teleport(newLocation);
						}
					});
	            });
			}
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public override void LeaveState(GameLevel gameLevel)
        {
            foreach (MurderGunPartEntity item in GunParts.Values)
            {
                item.DespawnEntity();
            }

			gameLevel.DoForAllPlayers(player =>
			{
				player.SetHideNameTag(false);
				player.IsAlwaysShowName = true;
				player.SetNameTagVisibility(true);
			});

			GunParts.Clear();
        }

	    public void InitializeMurderer(SkyPlayer player)
	    {
		    TitleUtil.SendCenteredSubtitle(player, "§c§l  Murderer§r\n§7Kill all innocent players!");

		    player.Inventory.SetInventorySlot(0, new ItemMurderKnife());
			player.Inventory.SetInventorySlot(8, new ItemGunParts());
			player.Inventory.SetHeldItemSlot(1); //Avoid holding the knife on spawn/select

		    player.HungerManager.Hunger = 20; //Set food to 'able to run' level.
		    player.SendUpdateAttributes();

			//PlayerAmmoCounts[player.Username] = 3; //Throwing Knives
		}

	    public override void HandleLeave(GameLevel gameLevel, SkyPlayer player)
	    {
		    base.HandleLeave(gameLevel, player);

		    if (((MurderLevel) gameLevel).Murderer == player)
		    {
			    //Re-select murderer
			    List<SkyPlayer> players = gameLevel.GetPlayersInTeam(MurderTeam.Innocent);
				int murdererIdx = Random.Next(players.Count);

			    int i = 0;
				foreach (SkyPlayer gamePlayer in players)
			    {
				    if (++i == murdererIdx)
				    {
					    gameLevel.SetPlayerTeam(gamePlayer, MurderTeam.Murderer);

					    InitializeMurderer(gamePlayer);
						gameLevel.DoForAllPlayers(playerLoop =>
						{
							if (playerLoop == gamePlayer)
							{
								return;
							}

							TitleUtil.SendCenteredSubtitle(playerLoop, "§fA new §c§lMurderer\n§fhas been selected");
						});
					    break;
				    }
			    }
		    }
	    }

	    private readonly IDictionary<string, int> _modalCountdownDict = new Dictionary<string, int>();

		public override void OnTick(GameLevel gameLevel, int currentTick, out int outTick)
        {
	        base.OnTick(gameLevel, currentTick, out outTick);

	        int secondsLeft = GetSecondsLeft();

	        if (secondsLeft > (MaxGameTime / 2))
	        {
		        return; //Ignore until the ticker has finished
	        }
	        if (secondsLeft == 0)
	        {
		        gameLevel.UpdateGameState(GetNextGameState(gameLevel));
		        return;
	        }

	        ITickableInformation tickableInformation = GetTickableInformation(null);

			gameLevel.DoForAllPlayers(player =>
			{
				SendTickableMessage(gameLevel, player, tickableInformation);

				if (player.IsSprinting && player.GameTeam != MurderTeam.Murderer)
				{
					player.SetSprinting(false);
				}

				if (player.IsGameSpectator)
				{
					if (player.Inventory.InHandSlot == 4)
					{
						if (_modalCountdownDict.TryGetValue(player.Username, out var countdownValue))
						{
							if (countdownValue == 1)
							{
								if (player.Level is GameLevel level)
								{
									level.ShowEndGameMenu(player);
									_modalCountdownDict[player.Username] = 6; //Reset to default
									player.Inventory.SetHeldItemSlot(3); //Shift off slot.

									player.BarHandler.AddMinorLine("§r", 1);
									return;
								}
							}
							else
							{
								_modalCountdownDict[player.Username] = (countdownValue = (countdownValue - 1));
							}
						}
						else
						{
							_modalCountdownDict.Add(player.Username, 6); //Default to 3 seconds
							countdownValue = 6;
						}

						int visibleCountdown = (int)Math.Ceiling(countdownValue / 2D);

						player.BarHandler.AddMinorLine($"§dContinue Holding for {visibleCountdown} Second{(visibleCountdown == 1 ? "" : "s")} to Open Menu", 1);
					}
					else
					{
						_modalCountdownDict.Remove(player.Username);
					}
				}
			});

            /*
             * Gun Parts
             */

            //Every 15 Seconds -- Can't spawn any gun parts if the spawned amount == the total locations
			// V Avoid spawning gun parts on the first possible spawn tick
            if (secondsLeft < (MaxGameTime / 2) - 10 && currentTick % 30 == 0 && GunParts.Count != GunPartLocations.Count)
            {
				SkyUtil.log("Attempting to spawn gun parts at tick " + currentTick);
                PlayerLocation spawnLocation = null;

	            int maxSpawnAmount = gameLevel.GetPlayerCount();
	            int spawnCount = 0;
	            while (++spawnCount < maxSpawnAmount)
	            {
		            int rollCount = 0;
		            while (++rollCount < 10 && GunParts.ContainsKey(spawnLocation = GunPartLocations[Random.Next(GunPartLocations.Count)]))
		            {
			            //
		            }

		            if (rollCount == 10)
		            {
			            break; //No more spawn points available.
		            }

		            if (spawnLocation != null)
		            {
			            MurderGunPartEntity item = new MurderGunPartEntity(this, (MurderLevel)gameLevel, spawnLocation);

			            GunParts.Add(spawnLocation, item);

			            gameLevel.AddEntity(item);
		            }
				}
            }
        }

        public override GameState GetNextGameState(GameLevel gameLevel)
        {
            return new MurderEndState();
        }

        public override bool DoInteractAtEntity(GameLevel gameLevel, int interactId, SkyPlayer player, SkyPlayer target)
        {
            MurderLevel murderLevel = (MurderLevel) gameLevel;
            Item itemInHand = player.Inventory.GetItemInHand();

            if (player == murderLevel.Murderer && itemInHand is ItemMurderKnife && target != null)
            {
                KillPlayer((MurderLevel) gameLevel, target);
				player.Inventory.SetInventorySlot(0, new ItemMurderKnife()); //Update Knife
			}
			//Left click only (Right click charges up)
            else if (interactId != 2 && itemInHand is ItemInnocentGun && PlayerAmmoCounts[player.Username] > 0)
            {
	            if (player.Experience > 0.05f)
	            {
		            return true;
	            }

				var arrow = new GunProjectile(player, gameLevel)
	            {
		            Damage = 0,
		            KnownPosition = (PlayerLocation) player.KnownPosition.Clone()
	            };
	            arrow.KnownPosition.Y += 1.62f;

                arrow.Velocity = arrow.KnownPosition.GetHeadDirection() * (2 * 2.0f * 1.5f);
                arrow.KnownPosition.Yaw = (float)arrow.Velocity.GetYaw();
                arrow.KnownPosition.Pitch = (float)arrow.Velocity.GetPitch();
                arrow.BroadcastMovement = true;
                arrow.DespawnOnImpact = true;
                arrow.SpawnEntity();

				int currentAmmo = --PlayerAmmoCounts[player.Username];
	            if (currentAmmo <= 0)
	            {
					player.Inventory.SetInventorySlot(0, new ItemAir()); //Remove Gun
	            }
	            else
	            {
					//Ensure the gun is updated to a 'ItemInnocentGun' rather than an ItemBow
		            RunnableTask.RunTaskLater(() => player.Inventory.SetInventorySlot(0, new ItemInnocentGun()), 50);
				}

				const float levelOneFullBarXp = 6.65f;

	            player.Experience = 0;
	            player.AddExperience(levelOneFullBarXp);

	            player.Inventory.SetInventorySlot(9, new ItemAir());

				const int updateTicks = 60;
	            const int timerMillis = 50;
	            RunnableTask.RunTaskTimer(() =>
	            {

		            player.AddExperience(-(levelOneFullBarXp / updateTicks));

		            if (currentAmmo > 0 && player.Experience <= 0.1)
		            {
			            player.Inventory.SetInventorySlot(9, new ItemArrow());
		            }

	            }, timerMillis, updateTicks + 2);
            }

            return true;
        }

        public override void HandleDamage(GameLevel gameLevel, Entity source, Entity target, Item item, int damage, DamageCause damageCause)
        {
            if (!(target is SkyPlayer) || ((SkyPlayer) target).IsGameSpectator)
            {
                return;
            }

            if (source is GunProjectile arrow && arrow.Shooter is SkyPlayer)
            {
                SkyPlayer shooter = (SkyPlayer) arrow.Shooter;
                
                //Ensure this player is alive
                if (!shooter.IsGameSpectator)
                {
                    KillPlayer((MurderLevel) gameLevel, (SkyPlayer) target);
                }
            }
            else if (item is ItemMurderKnife && source == ((MurderLevel) gameLevel).Murderer)
            {
                //Ensure this player is alive
                if (!((SkyPlayer) source).IsGameSpectator)
                {
                    KillPlayer((MurderLevel) gameLevel, (SkyPlayer)target);
                }
            }
        }

        //

        public void KillPlayer(MurderLevel murderLevel, SkyPlayer player)
        {
            murderLevel.SetPlayerTeam(player, MurderTeam.Spectator);

			player.Inventory.Clear();

			player.SetEffect(new Invisibility { Duration = int.MaxValue, Particles = false });
			player.SetEffect(new Blindness { Duration = 20, Particles = false });
			
			Random random = new Random();
	        for (int i = 0; i < 30; i++)
	        {
				ItemBreakParticle particle = new ItemBreakParticle(murderLevel, new ItemRedstone())
				{
					Position = new Vector3(player.KnownPosition.X - 1 + ((float)random.NextDouble() * 1),
						player.KnownPosition.Y + 0.5f + ((float) random.NextDouble() * 1),
						player.KnownPosition.Z - 1 + ((float)random.NextDouble() * 1))
				};

		        particle.Spawn();
			}

	        if (player == murderLevel.Murderer) //||
				/*murderLevel.GetPlayersInTeam(MurderTeam.Innocent, MurderTeam.Detective).Count == 0*///) //TODO: Uncomment for better presentation
			{
				//Message appears once game ends
			}
			else
			{
				TitleUtil.SendCenteredSubtitle(player, "§c§lYou Died§r\n§fYou are now in §7spectator §fmode!");

				//Only indicate if they aren't the last one alive
				if (murderLevel.GetPlayersInTeam(MurderTeam.Innocent, MurderTeam.Detective).Count > 0)
				{
					player.SetAllowFly(true);
					player.IsFlying = true;

					player.SendAdventureSettings();

					player.Knockback(new Vector3(0, 1.5f, 0));
				}

				RunnableTask.RunTaskLater(() =>
				{
					murderLevel.ShowEndGameMenu(player);
					player.Inventory.SetInventorySlot(4, new ItemEndNav()); //Modal Navigator
				}, 5000);
			}
        }

	    public int GetPlayerAmmo(MurderLevel gameLevel, SkyPlayer player)
	    {
		    if (PlayerAmmoCounts.ContainsKey(player.Username))
		    {
			    return PlayerAmmoCounts[player.Username];
			}

		    return 0;
	    }

        public int GetPlayerGunParts(MurderLevel gameLevel, SkyPlayer player)
        {
            //Allow gameLevel to be null if we're 100% sure this player isn't a Murderer
            if (gameLevel != null && gameLevel.Murderer == player)
            {
                return -1; //Invalid amount
            }

            return PlayerGunPartCounts[player.Username];
        }

        public int AddPlayerGunParts(MurderLevel gameLevel, SkyPlayer player)
        {
            //Allow gameLevel to be null if we're 100% sure this player isn't a Murderer
	        if (gameLevel != null && gameLevel.Murderer == player)
	        {
				player.BarHandler.AddMinorLine("§6(Stolen Gun Parts)");
		        return -1; //Murderer cannot pick up gun parts
			}

	        McpeLevelEvent levelEvent = McpeLevelEvent.CreateObject();
	        levelEvent.eventId = 1051;
	        levelEvent.data = 1;
	        levelEvent.position = player.KnownPosition.ToVector3();
	        levelEvent.AddReferences(1);

	        player.SendPackage(levelEvent);

	        if (GetPlayerAmmo(gameLevel, player) > 0)
	        {
		        return 0; //Ignore gun parts while a player still has ammo
	        }

			player.BarHandler.AddMinorLine("§6(+1 Gun Parts)");

			int count = GetPlayerGunParts(gameLevel, player) + 1;

            if (count == MaxGunParts)
            {
                count = 0;

                PlayerInventory inventory = player.Inventory;

                int currentSlot = inventory.InHandSlot;
                inventory.SetInventorySlot(8, new ItemAir());
                inventory.SetInventorySlot(0, new ItemInnocentGun());
	            inventory.SetInventorySlot(9, new ItemArrow());

				PlayerAmmoCounts[player.Username] = MaxGunAmmo;

                inventory.SetHeldItemSlot(currentSlot);
            }
            //Update gun parts count
            else
            {
				PlayerInventory inventory = player.Inventory;

				int currentSlot = inventory.InHandSlot;
				
				player.Inventory.SetInventorySlot(8, new ItemGunParts {Count = (byte) count});

				inventory.SetHeldItemSlot(currentSlot);
			}

			PlayerGunPartCounts[player.Username] = count;

			return count;
        }

	    public void SendTickableMessage(GameLevel gameLevel, SkyPlayer player, ITickableInformation tickableInformation)
	    {
		    if (tickableInformation == null)
		    {
			    tickableInformation = GetTickableInformation(player);
		    }

		    MurderTickableInformation murderInformation = (MurderTickableInformation) tickableInformation;

		    if (player.GameTeam == MurderTeam.Murderer)
		    {
				player.BarHandler.AddMajorLine($"§c§lMURDERER§r §7| {murderInformation.NeatTimeRemaining} §fRemaining...", 2);
			    //$"              §7{PlayerAmmoCounts[player.Username]}/3 Throwing Knives", TitleType.ActionBar);
			}
			else if (player.GameTeam == MurderTeam.Innocent)
		    {
				int gunAmmo = GetPlayerAmmo((MurderLevel) gameLevel, player);
			    player.BarHandler.AddMajorLine(
				    gunAmmo > 0
					    ? $"§a§lINNOCENT§r §7| {murderInformation.NeatTimeRemaining} §fRemaining §7| §d{GetPlayerAmmo((MurderLevel)gameLevel, player)}/{MaxGunAmmo} §fBullets..."
					    : $"§a§lINNOCENT§r §7| {murderInformation.NeatTimeRemaining} §fRemaining §7| §d{GetPlayerGunParts((MurderLevel)gameLevel, player)}/{MaxGunParts} §fGun Parts...",
				    2);
			}
			else if (player.GameTeam == MurderTeam.Detective)
		    {
				player.BarHandler.AddMajorLine($"§9§lDETECTIVE§r §7| {murderInformation.NeatTimeRemaining} §fRemaining §7| §dUnlimited §fBullets...", 2);
			}
			else if (player.IsGameSpectator)
		    {
			    player.BarHandler.AddMajorLine($"§7§lSPECTATOR§r §7| {murderInformation.NeatTimeRemaining} §fRemaining...", 2);
			}
	    }

	    public ITickableInformation GetTickableInformation(SkyPlayer player)
	    {
		    return new MurderTickableInformation() { NeatTimeRemaining = GetNeatTimeRemaining(GetSecondsLeft()) };
	    }

	}
}
