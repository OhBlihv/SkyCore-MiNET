using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiNET;
using MiNET.Blocks;
using MiNET.Effects;
using MiNET.Entities;
using MiNET.Entities.Projectiles;
using MiNET.Entities.World;
using MiNET.Items;
using MiNET.Net;
using MiNET.Particles;
using MiNET.Sounds;
using MiNET.Utils;
using MiNET.Worlds;
using SkyCore.Game;
using SkyCore.Game.State;
using SkyCore.Game.State.Impl;
using SkyCore.Games.Murder.Entities;
using SkyCore.Games.Murder.Items;
using SkyCore.Games.Murder.Level;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Games.Murder.State
{
    class MurderRunningState : RunningState
    {

	    private const int MaxGameTime = 120;
	    private const int PreStartTime = 10;

        private const int MaxGunParts = 5;

        private int _endTick = -1; //Default value
	    private bool _isStarted = false;

        private static readonly List<PlayerLocation> GunPartLocations = new List<PlayerLocation>();
        private static readonly List<PlayerLocation> PlayerSpawnLocations = new List<PlayerLocation>();

        public readonly Dictionary<PlayerLocation, MurderGunPartEntity> GunParts = new Dictionary<PlayerLocation, MurderGunPartEntity>();

        public readonly Dictionary<string, int> PlayerGunPartCounts = new Dictionary<string, int>();
        public readonly Dictionary<string, int> PlayerAmmoCounts = new Dictionary<string, int>();

        public override void EnterState(GameLevel gameLevel)
        {
            base.EnterState(gameLevel);

            GunPartLocations.Clear();
            PlayerSpawnLocations.Clear();

            GunPartLocations.AddRange(((MurderLevelInfo) ((MurderLevel) gameLevel).GameLevelInfo).GunPartLocations);
            PlayerSpawnLocations.AddRange(((MurderLevelInfo)((MurderLevel)gameLevel).GameLevelInfo).PlayerSpawnLocations);

            while (PlayerSpawnLocations.Count < gameLevel.GetMaxPlayers())
            {
                PlayerSpawnLocations.Add(((MurderLevelInfo)((MurderLevel)gameLevel).GameLevelInfo).PlayerSpawnLocations[0]);
            }

	        _endTick = gameLevel.Tick + MaxGameTime + PreStartTime;

            try
            {
				RunnableTask.RunTask(() =>
	            {
					//Create new collection due to iterating over a live list
		            ICollection<SkyPlayer> players = gameLevel.GetAllPlayers();
		            foreach (SkyPlayer player in players)
		            {
			            player.SetEffect(new Blindness{Duration = 80,Particles = false}); //Should be 3 seconds?
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

						//SkyUtil.log($"Moving speed from {player.MovementSpeed} to 0 during freeze phase");

			            player.SetNoAi(true);

						player.HungerManager.Hunger = 6; //Set food to 'unable to run' level.
						player.MovementSpeed = 0f;
						player.SendUpdateAttributes();
					});

					List<MurderTeam> teamRotation = new List<MurderTeam> { MurderTeam.Murderer, MurderTeam.Detective, MurderTeam.Innocent };
					int offset = new Random().Next(teamRotation.Count);
		            for (int i = 0; i < 12; i++)
		            {
			            MurderTeam team = teamRotation[(offset + i) % 3];
						foreach (SkyPlayer player in players)
						{
							TitleUtil.SendCenteredSubtitle(player, team.TeamPrefix + "§l" + team.TeamName + "\n");

							//Poorly enforce speed
							if (i == 0 || i == 11)
							{
								player.SetNoAi(true);
							}
						}

						SkyUtil.log($"Printed scroll {i}/12, with {team.TeamPrefix + "§l" + team.TeamName}");
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

					gameLevel.DoForPlayersIn(player =>
					{
						TitleUtil.SendCenteredSubtitle(player, "§a§lInnocent §r\n§7Track down the murderer!");
					}, MurderTeam.Innocent);

					gameLevel.DoForPlayersIn(player =>
					{
						TitleUtil.SendCenteredSubtitle(player, "§9§lDetective §r\n§7Track down the murderer!");

						player.Inventory.SetInventorySlot(0, new ItemInnocentGun());
						//SkyUtil.log($"In Slot 0 = {player.Inventory.GetSlots()[0].GetType().FullName}");

						PlayerAmmoCounts[player.Username] = int.MaxValue;
					}, MurderTeam.Detective);

		            gameLevel.DoForPlayersIn(InitializeMurderer, MurderTeam.Murderer);

					gameLevel.DoForAllPlayers(player =>
					{
						player.SendAdventureSettings();

						player.MovementSpeed = 0.1f;
						player.SendUpdateAttributes();

						player.SetNoAi(false);

						//Ensure this player is at the correct spawn location
						if (gameLevel.GetBlock(player.KnownPosition).Id != 0)
						{
							PlayerLocation newLocation = (PlayerLocation) player.KnownPosition.Clone();
							newLocation.Y++;
						
							player.Teleport(newLocation);
						}
					});

		            _isStarted = true;
	            });
			}
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public override void LeaveState(GameLevel gameLevel)
        {
            base.LeaveState(gameLevel);

            foreach (MurderGunPartEntity item in GunParts.Values)
            {
                item.DespawnEntity();
            }

            GunParts.Clear();
        }

	    public void InitializeMurderer(SkyPlayer player)
	    {
		    TitleUtil.SendCenteredSubtitle(player, "§c§l  Murderer§r\n§7Kill all innocent players!");

		    player.Inventory.SetInventorySlot(0, new ItemMurderKnife());
			player.Inventory.SetHeldItemSlot(1); //Avoid holding the knife on spawn/select

		    player.HungerManager.Hunger = 20; //Set food to 'able to run' level.

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

	    public override void OnTick(GameLevel gameLevel, int currentTick, out int outTick)
        {
	        base.OnTick(gameLevel, currentTick, out outTick);

            int secondsLeft = (_endTick - currentTick) / 2;

	        if (secondsLeft > (MaxGameTime / 2))
	        {
		        return; //Ignore until the ticker has finished
	        }
	        if (secondsLeft == 0)
	        {
		        gameLevel.UpdateGameState(GetNextGameState(gameLevel));
		        return;
	        }

	        /*{
				PlayerLocation soundLocation = PlayerSpawnLocations[_random.Next(PlayerSpawnLocations.Count)];
		        Vector3 soundVector = new Vector3(soundLocation.X, soundLocation.Y, soundLocation.Z);

		        Sound sound;

		        switch (_random.Next(2))
		        {
			        case 0:
				        sound = new DoorCloseSound(soundVector);
				        break;
					case 1:
						sound = new FizzSound(soundVector);
						break;
					default:
						sound = new BlazeFireballSound(soundVector);
						break;
		        }

		        sound.Spawn(gameLevel);
			}*/

	        string neatRemaining;
	        {
		        int minutes = 0;
				while (secondsLeft >= 60)
				{
					secondsLeft -= 60;
					minutes++;
				}

		        neatRemaining = minutes + ":";

		        if (secondsLeft < 10)
		        {
			        neatRemaining += "0" + secondsLeft;
		        }
		        else
		        {
			        neatRemaining += secondsLeft;
		        }
	        }

            gameLevel.DoForPlayersIn(player =>
            {
				player.BarHandler.AddMajorLine($"§a§lINNOCENT§r §7| {neatRemaining} Remaining §7| §d{GetPlayerAmmo(gameLevel as MurderLevel, player)}/10 §fBullets...", 2);
            }, MurderTeam.Innocent);

            gameLevel.DoForPlayersIn(player =>
            {
	            player.BarHandler.AddMajorLine($"§9§lDETECTIVE§r §7| {neatRemaining} Remaining §7| §dUnlimited §fBullets...", 2);
            }, MurderTeam.Detective);

            gameLevel.DoForPlayersIn(player =>
			{
				player.BarHandler.AddMajorLine($"§c§lMURDERER§r §7| {neatRemaining} §fRemaining...", 2);
				//$"              §7{PlayerAmmoCounts[player.Username]}/3 Throwing Knives", TitleType.ActionBar);
			}, MurderTeam.Murderer);

			/*
			 * Dodgy Anti-Cheat
			 */

			gameLevel.DoForAllPlayers(player =>
			{
				if (player.IsSprinting && gameLevel.GetPlayerTeam(player) != MurderTeam.Murderer)
				{
					player.SetSprinting(false);
					//TODO: Enforce some freeze for hacking players?
				}
			});

            /*
             * Gun Parts
             */

            //Every 5 Seconds -- Can't spawn any gun parts if the spawned amount == the total locations
            if (currentTick % 10 == 0 && GunParts.Count != GunPartLocations.Count)
            {
                PlayerLocation spawnLocation = null;

	            int maxSpawnAmount = gameLevel.GetMaxPlayers();
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

        public override bool DoInteract(GameLevel gameLevel, int interactId, SkyPlayer player, SkyPlayer target)
        {
            MurderLevel murderLevel = (MurderLevel) gameLevel;
            Item itemInHand = player.Inventory.GetItemInHand();

            if (player == murderLevel.Murderer && itemInHand is ItemMurderKnife && target != null)
            {
                KillPlayer((MurderLevel) gameLevel, target);
            }
            else if (itemInHand is ItemInnocentGun && PlayerAmmoCounts[player.Username] > 0)
            {
				SkyUtil.log($"Experience: {player.Experience}");
	            if (player.Experience > 0.05f)
	            {
		            return true;
	            }

				var arrow = new Arrow(player, gameLevel)
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

				PlayerAmmoCounts[player.Username]--;

				const float levelOneFullBarXp = 6.65f;

	            player.Experience = 0;
	            player.AddExperience(levelOneFullBarXp, true);

				const int updateTicks = 60;
	            const int timerMillis = 50;
	            RunnableTask.RunTaskTimer(() =>
	            {

		            player.AddExperience(-(levelOneFullBarXp / updateTicks), true);

	            }, timerMillis, updateTicks + 2);
            }

            return true;
        }

        public override void HandleDamage(GameLevel gameLevel, Entity source, Entity target, Item item, int damage, DamageCause damageCause)
        {
            if (!(target is SkyPlayer))
            {
                return;
            }

            if (source is Arrow && ((Arrow)source).Shooter is SkyPlayer)
            {
                SkyPlayer shooter = (SkyPlayer) ((Arrow) source).Shooter;
                
                //Ensure this player is alive
                if (!gameLevel.GetPlayerTeam(shooter).IsSpectator)
                {
                    KillPlayer((MurderLevel) gameLevel, (SkyPlayer) target);
                }
            }
            else if (item is ItemMurderKnife && source == ((MurderLevel) gameLevel).Murderer)
            {
                //Ensure this player is alive
                if (!gameLevel.GetPlayerTeam((SkyPlayer) source).IsSpectator)
                {
                    KillPlayer((MurderLevel)gameLevel, (SkyPlayer)target);
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
		        /*DestroyBlockParticle particle = new DestroyBlockParticle(murderLevel, new RedstoneBlock())
		        {
			        Position = new Vector3(player.KnownPosition.X,
					(float) (player.KnownPosition.Y + 0.1 + i),
					player.KnownPosition.Z)
		        };*/

				ItemBreakParticle particle = new ItemBreakParticle(murderLevel, new ItemRedstone())
				{
					Position = new Vector3(player.KnownPosition.X - 1 + ((float)random.NextDouble() * 1),
						(float)(player.KnownPosition.Y + 0.5f + ((float) random.NextDouble() * 1)),
						player.KnownPosition.Z - 1 + ((float)random.NextDouble() * 1))
				};

		        particle.Spawn();
			}

	        if (player == murderLevel.Murderer ||
				murderLevel.GetPlayersInTeam(MurderTeam.Innocent, MurderTeam.Detective).Count == 0)
			{
				//Message appears once game ends
			}
			else
			{
				player.SendTitle("§c§lYOU DIED§r");
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
                return -1; //Murderer cannot pick up gun parts
            }

	        McpeLevelEvent levelEvent = McpeLevelEvent.CreateObject();
	        levelEvent.eventId = 1051;
	        levelEvent.data = 1;
	        levelEvent.position = player.KnownPosition.ToVector3();
	        levelEvent.AddReferences(1);

	        player.SendPackage(levelEvent);

			int count = GetPlayerGunParts(gameLevel, player) + 1;

            if (count == MaxGunParts)
            {
                count = 0;

                PlayerInventory inventory = player.Inventory;

                int currentSlot = inventory.InHandSlot;
                inventory.SetInventorySlot(8, new ItemAir());
                inventory.SetInventorySlot(0, new ItemInnocentGun());

				PlayerAmmoCounts[player.Username] = 10; //TODO: Avoid hardcode?

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

    }
}
