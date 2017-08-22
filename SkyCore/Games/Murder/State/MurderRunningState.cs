using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using MiNET;
using MiNET.Effects;
using MiNET.Entities;
using MiNET.Entities.Projectiles;
using MiNET.Entities.World;
using MiNET.Items;
using MiNET.Utils;
using MiNET.Worlds;
using SkyCore.Game;
using SkyCore.Game.State;
using SkyCore.Game.State.Impl;
using SkyCore.Games.Murder.Entities;
using SkyCore.Games.Murder.Items;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Games.Murder.State
{
    class MurderRunningState : RunningState
    {

        private const int MaxGunParts = 5;

        private readonly Random _random = new Random();

        private int endTick = 5000; //Default value

        private static readonly List<PlayerLocation> GunPartLocations = new List<PlayerLocation>();
        private static readonly List<PlayerLocation> PlayerSpawnLocations = new List<PlayerLocation>();

        public readonly Dictionary<PlayerLocation, MurderGunPartEntity> GunParts = new Dictionary<PlayerLocation, MurderGunPartEntity>();

        public readonly Dictionary<string, int> PlayerGunPartCounts = new Dictionary<string, int>();

        public override void EnterState(GameLevel gameLevel)
        {
            base.EnterState(gameLevel);

            GunPartLocations.Clear();
            PlayerSpawnLocations.Clear();

            GunPartLocations.AddRange(((MurderLevel) gameLevel).GunPartLocations);
            PlayerSpawnLocations.AddRange(((MurderLevel) gameLevel).PlayerSpawnLocations);

            while (PlayerSpawnLocations.Count < gameLevel.GetMaxPlayers())
            {
                PlayerSpawnLocations.Add(((MurderLevel)gameLevel).PlayerSpawnLocations[0]);
            }

            try
            {
                //endTick = gameLevel.Tick + 240; //2 Minutes
                endTick = gameLevel.Tick + 120; //60 Seconds

                //Create new collection due to iterating over a live list
                ICollection<MiNET.Player> players = new List<MiNET.Player>(gameLevel.Players.Values);

                int murdererIdx = _random.Next(players.Count),
                    detectiveIdx = 0;

                int idx = 0;
                while (++idx < 50 && (detectiveIdx = _random.Next(players.Count)) == murdererIdx)
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
                    player.SendTitle("§7Track down the murderer!", TitleType.SubTitle);
                    player.SendTitle("§a§lInnocent§r"); //Title
                }, MurderTeam.Innocent);

                gameLevel.DoForPlayersIn(player =>
                {
                    player.SendTitle("§7Track down the murderer!", TitleType.SubTitle);
                    player.SendTitle("§9§lDetective§r"); //Title

                    player.Inventory.SetInventorySlot(0, new ItemInnocentGun());
                    player.Inventory.SetInventorySlot(1, new ItemArrow{Count = 10});
                }, MurderTeam.Detective);

                gameLevel.DoForPlayersIn(player =>
                {
                    player.SendTitle("§7Kill all innocent players!", TitleType.SubTitle);
                    player.SendTitle("§c§lMurderer§r"); //Title

                    player.Inventory.SetInventorySlot(0, new ItemMurderKnife());
                }, MurderTeam.Murderer);

                List<PlayerLocation> usedSpawnLocations = new List<PlayerLocation>();
                gameLevel.DoForAllPlayers(player =>
                {
                    player.SetGameMode(GameMode.Adventure);

                    //Avoid spawning two players in the same location
                    PlayerLocation spawnLocation;
                    while (usedSpawnLocations.Contains((spawnLocation = PlayerSpawnLocations[_random.Next(PlayerSpawnLocations.Count)])))
                    {
                        //
                    }

                    usedSpawnLocations.Add(spawnLocation);

                    player.Teleport(spawnLocation);

                    player.SetHideNameTag(true);
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

        public override void OnTick(GameLevel gameLevel, int currentTick, out int outTick)
        {
            base.OnTick(gameLevel, currentTick, out outTick);

            int secondsLeft = (endTick - currentTick) / 2;

            if (secondsLeft == 0)
            {
                gameLevel.UpdateGameState(GetNextGameState(gameLevel));
                return;
            }

            gameLevel.DoForPlayersIn(player =>
            {
                player.SendTitle($"§a§lINNOCENT §r§7{secondsLeft} Seconds Remaining\n" +
                                 $"\t              §7{GetPlayerGunParts(null, player)}/{MaxGunParts} Gun Parts\n" +
                                 $"\t              §7{0}/10 Bullets", TitleType.ActionBar);
            }, MurderTeam.Innocent);

            gameLevel.DoForPlayersIn(player =>
            {
                player.SendTitle($"§9§lDETECTIVE §r§7{secondsLeft} Seconds Remaining\n" +
                                 $"              §7{GetPlayerGunParts(null, player)}/{MaxGunParts} Gun Parts\n" +
                                 $"              §7{0}/10 Bullets", TitleType.ActionBar);
            }, MurderTeam.Detective);

            gameLevel.DoForPlayersIn(player =>
            {
                player.SendTitle($"§c§lMURDERER §r§7{secondsLeft} Seconds Remaining\n" +
                                 $"              §7{0}/3 Throwing Knives", TitleType.ActionBar);
            }, MurderTeam.Murderer);

            /*
             * Gun Parts
             */

            //Every 5 Seconds -- Can't spawn any gun parts if the spawned amount == the total locations
            if (currentTick % 1 == 0 && GunParts.Count != GunPartLocations.Count)
            {
                PlayerLocation spawnLocation = null;

                int rollCount = 0;
                while (++rollCount < 10 && GunParts.ContainsKey((spawnLocation = GunPartLocations[_random.Next(GunPartLocations.Count)])))
                {
                    //
                }

                if (rollCount == 10)
                {
                    spawnLocation = null; //Invalidate. All spots allocated.
                }

                if (spawnLocation != null)
                {
                    MurderGunPartEntity item = new MurderGunPartEntity(this, (MurderLevel) gameLevel, spawnLocation);

                    GunParts.Add(spawnLocation, item);

                    //item.Gravity = 2D;
                    //item.Velocity = Vector3.Zero;

                    gameLevel.AddEntity(item);
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

            if (player == murderLevel.Murderer && itemInHand is ItemMurderKnife)
            {
                gameLevel.SetPlayerTeam(target, MurderTeam.Spectator);
            }
            //TODO: Check ammo counts
            else if (itemInHand is ItemInnocentGun)
            {
                var arrow = new Arrow(player, gameLevel) { Damage = 0 };
                arrow.KnownPosition = (PlayerLocation)player.KnownPosition.Clone();
                arrow.KnownPosition.Y += 1.62f;

                arrow.Velocity = arrow.KnownPosition.GetHeadDirection() * (2 * 2.0f * 1.5f);
                arrow.KnownPosition.Yaw = (float)arrow.Velocity.GetYaw();
                arrow.KnownPosition.Pitch = (float)arrow.Velocity.GetPitch();
                arrow.BroadcastMovement = true;
                arrow.DespawnOnImpact = true;
                arrow.SpawnEntity();
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

            player.SetEffect(new Invisibility{Duration = int.MaxValue, Particles = false});
            player.SetEffect(new Blindness {Duration = 5, Particles = false});
            player.SendTitle("§c§lYOU DIED§r");

            //TODO: Hide player from all living players
            player.SetGameMode(GameMode.Spectator);
        }

        public int GetPlayerGunParts(MurderLevel gameLevel, SkyPlayer player)
        {
            //Allow gameLevel to be null if we're 100% sure this player isn't a Murderer
            if (gameLevel != null && gameLevel.Murderer == player)
            {
                return -1; //Invalid amount
            }

            int count;
            PlayerGunPartCounts.TryGetValue(player.Username, out count);

            return count;
        }

        public int AddPlayerGunParts(MurderLevel gameLevel, SkyPlayer player)
        {
            //Allow gameLevel to be null if we're 100% sure this player isn't a Murderer
            if (gameLevel != null && gameLevel.Murderer == player)
            {
                return -1; //Murderer cannot pick up gun parts
            }

            int count = GetPlayerGunParts(gameLevel, player) + 1;

            if (count == MaxGunParts)
            {
                count = 0;

                PlayerInventory inventory = player.Inventory;

                int currentSlot = inventory.InHandSlot;
                inventory.SetInventorySlot(8, new ItemAir());
                inventory.SetInventorySlot(0, new ItemInnocentGun());

                inventory.SetHeldItemSlot(currentSlot);
            }
            //Update gun parts count
            else
            {
                player.Inventory.SetInventorySlot(8, new ItemGunParts {Count = (byte) count});
            }

            if (PlayerGunPartCounts.ContainsKey(player.Username))
            {
                PlayerGunPartCounts[player.Username] = count;
            }
            else
            {
                PlayerGunPartCounts.Add(player.Username, count);
            }

            return count;
        }

    }
}
