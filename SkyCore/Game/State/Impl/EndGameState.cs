using System.Collections.Generic;
using System.Threading;
using SkyCore.Game.Level;
using SkyCore.Player;

namespace SkyCore.Game.State.Impl
{
    public abstract class EndGameState : GameState
    {

	    protected int TimeRemaining { get; set; }
	    
        public override void EnterState(GameLevel gameLevel)
        {
	        TimeRemaining = 5 * 2; //5 Seconds
		}

        public override void LeaveState(GameLevel gameLevel)
        {
			gameLevel.DoForAllPlayers(player =>
			{
				player.RemoveAllEffects();

				player.SetHideNameTag(false);
				player.SetNameTagVisibility(true);

				if (SkyCoreAPI.IsRebootQueued)
				{
					ExternalGameHandler.AddPlayer(player, "hub");
				}
				else
				{
					ExternalGameHandler.AddPlayer(player, gameLevel.GameType);
				}
			});
		}

        public override bool CanAddPlayer(GameLevel gameLevel)
        {
            return false;
        }

        public override void OnTick(GameLevel gameLevel, int currentTick, out int outTick)
        {
            outTick = currentTick;

	        if (TimeRemaining-- >= 0)
	        {
		        string message;
		        if (TimeRemaining == 0)
		        {
			        message = "§r§f §r";
		        }
		        else
		        {
					int timeRemaining = TimeRemaining / 2;
			        message = $"§d§lGame Ended:§r §fNext Game in §7{timeRemaining} §fSecond{(timeRemaining != 1 ? "s" : "")}...";
				}

				gameLevel.DoForAllPlayers(player =>
				{
					player.BarHandler.AddMajorLine(message, 4, 3);
				});
			}
	        else
	        {
				List<SkyPlayer> remainingPlayers = gameLevel.GetAllPlayers();
		        if (remainingPlayers.Count > 0)
		        {
			        foreach (SkyPlayer player in remainingPlayers)
			        {
				        gameLevel.RemovePlayer(player);

						if (SkyCoreAPI.IsRebootQueued)
						{
							player.BarHandler.AddMajorLine(("§d§lGame Ending: §r§fMoving to Network Lobby..."), 20, 7);
							ExternalGameHandler.AddPlayer(player, "hub");
						}
						else
						{
							ExternalGameHandler.AddPlayer(player, gameLevel.GameType);
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
