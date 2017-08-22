using MiNET;
using MiNET.Entities;
using MiNET.Items;
using MiNET.Utils;
using SkyCore.Player;

namespace SkyCore.Game.State.Impl
{

    public abstract class LobbyState : GameState
    {
        public override void EnterState(GameLevel gameController)
        {
            
        }

        public override void LeaveState(GameLevel gameController)
        {
            
        }

        public override bool CanAddPlayer(GameLevel gameLevel)
        {
            return true;
        }

        public override void InitializePlayer(GameLevel gameController, SkyPlayer player)
        {
            player.SendMessage($"{ChatColors.Yellow}{player.PlayerGroup.Prefix}{player.Username} joined ({gameController.GetPlayerCount()}/{gameController.GetMaxPlayers()})");
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
                requiredPlayers = getRequiredPlayers(gameLevel);

			string actionBarMessage = null;

            if (currentPlayers < requiredPlayers)
            {
                startCountdownTick = -1; //Reset the timer

                //Only update action bar every second
                if (currentTick % 2 == 0)
                {
                    actionBarMessage = $"{ChatColors.Yellow}{gameLevel.GetPlayerCount()}/{getRequiredPlayers(gameLevel)} players required for start";
                }
            }
            else
            {
                if (startCountdownTick == -1)
                {
                    startCountdownTick = currentTick;
                }

                //Only update action bar every second
                if (currentTick % 2 == 0)
                {
                    int secondsRemaining = (getCountdownTicks() - (currentTick - startCountdownTick)) / 2;
                    if (secondsRemaining <= 0)
                    {
                        if (secondsRemaining == 0)
                        {
                            actionBarMessage = $"{ChatColors.Yellow}Starting Game...";

                            gameLevel.UpdateGameState(GetNextGameState(gameLevel));
                        }
                    }
                    else
                    {
                        actionBarMessage = $"{ChatColors.Yellow}Starting Game in {secondsRemaining} seconds...";
                    }
                }
            }

            if (actionBarMessage != null)
            {
                foreach (SkyPlayer player in gameLevel.GetPlayers())
                {
					SkyUtil.log($"Sending to {player.Username}");
                    player.SendTitle(actionBarMessage, TitleType.ActionBar);
                }
				SkyUtil.log(actionBarMessage);
            }

            outTick = currentTick;
        }

        /*
         *  Lobby Countdown/Management methods
         */ 

        private int startCountdownTick = -1;

        protected int getCountdownTicks()
        {
            return 20; //10 seconds by default
        }

        protected int getRequiredPlayers(GameLevel gameLevel)
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
