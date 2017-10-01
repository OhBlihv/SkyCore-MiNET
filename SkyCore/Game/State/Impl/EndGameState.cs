using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiNET;
using MiNET.Entities;
using MiNET.Items;
using MiNET.Worlds;
using SkyCore.Games.Murder;
using SkyCore.Games.Murder.State;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Game.State.Impl
{
    public abstract class EndGameState : GameState
    {

	    protected int TimeRemaining { get; set; }

        public override void EnterState(GameLevel gameLevel)
        {
	        TimeRemaining = 30 * 2;  //30 Seconds

			RunnableTask.RunTaskLater(() =>
			{
				gameLevel.DoForAllPlayers(gameLevel.ShowEndGameMenu);
			}, 5000);
		}

        public override void LeaveState(GameLevel gameLevel)
        {
			gameLevel.DoForAllPlayers(player =>
			{
				player.RemoveAllEffects();

				ExternalGameHandler.RequeuePlayer(player, gameLevel.GameType);
			});
		}

        public override bool CanAddPlayer(GameLevel gameLevel)
        {
            return false;
        }

        public override void InitializePlayer(GameLevel gameLevel, SkyPlayer player)
        {
            
        }

        public override void HandleLeave(GameLevel gameLevel, SkyPlayer player)
        {
            
        }

        public override void HandleDamage(GameLevel gameLevel, Entity source, Entity target, Item item, int damage, DamageCause damageCause)
        {
            //No damage during End Game Phase
        }

        public override void OnTick(GameLevel gameLevel, int currentTick, out int outTick)
        {
            outTick = currentTick;

	        if (TimeRemaining-- >= 0)
	        {
		        int timeRemaining = TimeRemaining / 2;
		        string message;
		        if (timeRemaining != 1)
		        {
			        message = $"§d§lGame Ended:§r §fNext Game starts in §7{timeRemaining} §fSeconds...";
		        }
		        else
		        {
			        message = $"§d§lGame Ended:§r §fNext Game starts in §7{timeRemaining} §fSecond...";
		        }

				gameLevel.DoForAllPlayers(player =>
				{
					player.BarHandler.AddMajorLine(message, 4, 3);
				});
			}
	        else
	        {
				MiNET.Player[] remainingPlayers = gameLevel.GetAllPlayers();
		        if (remainingPlayers.Length > 0)
		        {
			        Level hubLevel = SkyCoreAPI.Instance.GetHubLevel();

			        foreach (MiNET.Player player in remainingPlayers)
			        {
				        if (hubLevel == null)
				        {
					        //TODO: Avoid kicking them?
					        player.Disconnect("Unable to enter hub.");
				        }
				        else
				        {
					        ((SkyPlayer) player).BarHandler.AddMajorLine(($"§d§lGame Ending: §r§fMoving to New Game..."), 20, 7);

							gameLevel.RemovePlayer(player);
							
							ExternalGameHandler.RequeuePlayer((SkyPlayer) player, gameLevel.GameType);
				        }
			        }
		        }

				Thread.Sleep(5000);

				gameLevel.UpdateGameState(new VoidGameState());
			}
		}

        public override GameState GetNextGameState(GameLevel gameLevel)
        {
            return new VoidGameState(); //End
        }

        public override StateType GetEnumState(GameLevel gameLevel)
        {
            return StateType.EndGame;
        }
    }
}
