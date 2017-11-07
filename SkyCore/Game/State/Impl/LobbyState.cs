﻿using System;
using System.Collections.Generic;
using System.IO;
using MiNET.Entities;
using MiNET.Items;
using MiNET.Utils;
using Newtonsoft.Json;
using SkyCore.Entities;
using SkyCore.Game.Level;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Game.State.Impl
{

    public abstract class LobbyState : GameState
    {

	    private List<Entity> spawnedEntities = null;

        public override void EnterState(GameLevel gameLevel)
        {
            gameLevel.DoForAllPlayers(player =>
			{
				player.RemoveAllEffects();
			});

	        GameLevelInfo gameLevelInfo = gameLevel.GameLevelInfo;

	        //Spawn Lobby NPC
	        if (gameLevelInfo.LobbyNPCLocation == null)
	        {
		        gameLevelInfo.LobbyNPCLocation = new PlayerLocation(260.5, 15, 251.5);

				File.WriteAllText(GameController.GetGameLevelInfoLocation(gameLevel.GameType, gameLevel.LevelName), JsonConvert.SerializeObject(gameLevel.GameLevelInfo, Formatting.Indented));
		        SkyUtil.log($"LobbyNPCLocation Updated with default value for {gameLevel.LevelName}");
	        }

	        spawnedEntities = PlayerNPC.SpawnLobbyNPC(gameLevel, gameLevelInfo.GameType, gameLevel.GameLevelInfo.LobbyNPCLocation);

	        //Spawn Lobby Map/Image
	        if (gameLevelInfo.LobbyMapLocation.Y < 0) //Default == -1
	        {
		        gameLevelInfo.LobbyMapLocation = new BlockCoordinates(252, 15, 249);

		        File.WriteAllText(GameController.GetGameLevelInfoLocation(gameLevel.GameType, gameLevel.LevelName), JsonConvert.SerializeObject(gameLevel.GameLevelInfo, Formatting.Indented));
	        }

			spawnedEntities.AddRange(MapUtil.SpawnMapImage(@"C:\Users\Administrator\Desktop\dl\map-images\TestImage.png", 7, 4, gameLevel, gameLevelInfo.LobbyMapLocation));
		}

	    public override void LeaveState(GameLevel gameLevel)
	    {
		    foreach (Entity entity in spawnedEntities)
		    {
			    entity.DespawnEntity();
		    }

			spawnedEntities.Clear();
	    }

		public override bool CanAddPlayer(GameLevel gameLevel)
        {
	        return gameLevel.GetPlayerCount() < gameLevel.GetMaxPlayers();
        }

        public override void InitializePlayer(GameLevel gameLevel, SkyPlayer player)
        {
			//Runs the join message once the player's permissions have loaded
			player.AddPostLoginTask(() =>
			{
				gameLevel.DoForAllPlayers(gamePlayer =>
				{
					//§f(§e{gameLevel.GetPlayerCount()}/{gameLevel.GetMaxPlayers()}§f)
					gamePlayer.BarHandler.AddMinorLine($"§e{player.PlayerGroup.Prefix} {player.Username}§r §7entered the game!");
				});
			});

	        player.SendPlayerInventory();
		}

        public override void OnTick(GameLevel gameLevel, int currentTick, out int outTick)
        {
            int currentPlayers  = gameLevel.GetGamePlayerCount(), //Doesn't count incoming players
                requiredPlayers = GetRequiredPlayers(gameLevel);

			string actionBarMessage = null;

            if (currentPlayers < requiredPlayers)
            {
                _startCountdownTick = -1; //Reset the timer

                //Only update action bar every second
                if (currentTick % 2 == 0)
                {
	                actionBarMessage = $"§d§lStarting Soon:§r §7({gameLevel.GetPlayerCount()}/{GetRequiredPlayers(gameLevel)}) §fPlayers Required...";
                }
            }
            else
            {
                if (_startCountdownTick == -1)
                {
                    _startCountdownTick = currentTick;
                }

                //Only update action bar every second
                if (currentTick % 2 == 0)
                {
                    int secondsRemaining = (GetCountdownTicks() - (currentTick - _startCountdownTick)) / 2;
                    if (secondsRemaining <= 0)
                    {
                        if (secondsRemaining == 0)
                        {
                            actionBarMessage = "§d§lGame Starting:§r §fBeginning Now...";

                            gameLevel.UpdateGameState(GetNextGameState(gameLevel));
                        }
                    }
                    else
                    {
						actionBarMessage = $"§d§lGame Starting:§r §7{secondsRemaining} §fSecond{(secondsRemaining == 1 ? "" : "s")} Remaining...";
                    }
                }
            }

            if (actionBarMessage != null)
            {
                foreach (SkyPlayer player in gameLevel.GetPlayers())
                {
	                player.BarHandler.AddMajorLine(actionBarMessage, 2);
                }
            }

            outTick = currentTick;
        }

        /*
         *  Lobby Countdown/Management methods
         */ 

        private int _startCountdownTick = -1;

        protected int GetCountdownTicks()
        {
            return 20; //10 seconds by default
        }

        protected int GetRequiredPlayers(GameLevel gameLevel)
        {
            return 2;
            //return (int) (gameLevel.GetMaxPlayers() * 0.8D);
        }

        /*
         * End
         */ 

        public override StateType GetEnumState(GameLevel gameLevel)
        {
            if (gameLevel.GetPlayerCount() == 0)
            {
                return StateType.Empty;
            }
            if (gameLevel.GetPlayerCount() == gameLevel.GetMaxPlayers())
            {
                return StateType.PreGameStarting;
            }

            return StateType.PreGame;
        }
    }

}
