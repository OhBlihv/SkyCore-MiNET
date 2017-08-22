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
using SkyCore.Entities;
using SkyCore.Game;

namespace SkyCore.Player
{
    public class SkyPlayer : MiNET.Player
    {
        private static ILog Log = LogManager.GetLogger(typeof(SkyPlayer));

        public SkyCoreAPI skyCoreApi;

        public PlayerGroup PlayerGroup { get; set; }

        public void setPlayerGroup(PlayerGroup playerGroup)
        {
            PlayerGroup = playerGroup;

            //Initialize Player UserPermission level for commands
            PermissionLevel = playerGroup.PermissionLevel;
        }

        public SkyPlayer(MiNetServer server, IPEndPoint endpoint, SkyCoreAPI skyCoreApi) : base(server, endpoint)
        {
            this.skyCoreApi = skyCoreApi;
        }

        private bool _hasJoined = false;

        public override void InitializePlayer()
        {
            if (_hasJoined)
            {
                return;
            }

            base.InitializePlayer();

            try
            {
                SkyUtil.log("Initialising Player");
                base.InitializePlayer();

                if (CertificateData.ExtraData.Xuid == null)
                {
                    //Disconnect("§aXBOX §caccount required to login.");
                    //return;
                }

                SkyUtil.log("Reading Group for " + Username);
                setPlayerGroup(skyCoreApi.Permissions.getPlayerGroup(Username));
                SkyUtil.log($"Initialized as {PlayerGroup}({PermissionLevel})");

                string prefix = PlayerGroup.Prefix;
                if (prefix.Length > 2)
                {
                    prefix += " ";
                }

                SetDisplayName(prefix + Username);
                SetNameTag(prefix + Username);
                this.SetHideNameTag(false);
                
                SkyUtil.log($"Set {Username}'s name to {DisplayName}");

                //Scale = 2.0D;

                _hasJoined = true;

                IsSpawned = true;
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
                if (message.actionId == 2)
                {
                    SkyUtil.log($"Processing NPC Interact as {Username}");
                    (target as PlayerNPC)?.OnInteract(this);
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
