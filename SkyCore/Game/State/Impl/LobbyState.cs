﻿using MiNET;
using MiNET.Entities;
using MiNET.Items;
using MiNET.Utils;
using SkyCore.Player;

namespace SkyCore.Game.State.Impl
{

    public abstract class LobbyState : GameState
    {
        public override void EnterState(GameLevel gameLevel)
        {
            gameLevel.DoForAllPlayers(player =>
			{
				player.RemoveAllEffects();
			});
        }

        public override void LeaveState(GameLevel gameController)
        {
            
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
			
        }

        public override void HandleLeave(GameLevel gameController, SkyPlayer player)
        {
            
        }

        public override void HandleDamage(GameLevel gameLevel, Entity source, Entity target, Item item, int damage, DamageCause damageCause)
        {
            //No damage during Lobby Phase
        }

        public override void OnTick(GameLevel gameLevel, int currentTick, out int outTick)
        {
            int currentPlayers  = gameLevel.GetPlayerCount(),
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
	                    if (secondsRemaining == 1)
	                    {
							actionBarMessage = $"§d§lGame Starting:§r §7{secondsRemaining} §fSecond Remaining...";
						}
	                    else
	                    {
							actionBarMessage = $"§d§lGame Starting:§r §7{secondsRemaining} §fSeconds Remaining...";
						}
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
            else if (gameLevel.GetPlayerCount() == gameLevel.GetMaxPlayers())
            {
                return StateType.PreGameStarting;
            }

            return StateType.PreGame;
        }
    }

}
