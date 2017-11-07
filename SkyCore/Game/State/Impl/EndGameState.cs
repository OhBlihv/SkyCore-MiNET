using System;
using System.Collections.Generic;
using System.Threading;
using SkyCore.Game.Items;
using SkyCore.Game.Level;
using SkyCore.Player;
using SkyCore.Util;

namespace SkyCore.Game.State.Impl
{
    public abstract class EndGameState : GameState
    {

	    protected int TimeRemaining { get; set; }
	    
	    private readonly IDictionary<string, int> _modalCountdownDict = new Dictionary<string, int>(); 

        public override void EnterState(GameLevel gameLevel)
        {
	        TimeRemaining = 30 * 2; //30 Seconds

			RunnableTask.RunTaskLater(() =>
			{
				if (SkyCoreAPI.IsRebootQueued)
				{
					gameLevel.DoForAllPlayers((player) => ExternalGameHandler.AddPlayer(player, "hub"));
				}
				else
				{
					gameLevel.DoForAllPlayers(gameLevel.ShowEndGameMenu);

					//Re-enable the player nametags
					gameLevel.DoForAllPlayers(player =>
					{
						player.HideNameTag = false;
						player.Inventory.SetInventorySlot(4, new ItemEndNav());
					});
				}
			}, 5000);
		}

        public override void LeaveState(GameLevel gameLevel)
        {
			gameLevel.DoForAllPlayers(player =>
			{
				player.RemoveAllEffects();

				ExternalGameHandler.AddPlayer(player, "hub");
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

						int visibleCountdown = (int) Math.Ceiling(countdownValue / 2D);

						player.BarHandler.AddMinorLine($"§dContinue Holding for {visibleCountdown} Second{(visibleCountdown == 1 ? "" : "s")} to Open Menu", 1);
					}
					else
					{
						_modalCountdownDict.Remove(player.Username);
					}
				});
			}
	        else
	        {
				List<SkyPlayer> remainingPlayers = gameLevel.GetAllPlayers();
		        if (remainingPlayers.Count > 0)
		        {
			        MiNET.Worlds.Level hubLevel = SkyCoreAPI.Instance.GetHubLevel();

			        foreach (SkyPlayer player in remainingPlayers)
			        {
				        if (hubLevel == null)
				        {
					        //TODO: Avoid kicking them?
					        player.Disconnect("Unable to enter hub.");
				        }
				        else
				        {
					        player.BarHandler.AddMajorLine(($"§d§lGame Ending: §r§fMoving to New Game..."), 20, 7);

							gameLevel.RemovePlayer(player);
							
							ExternalGameHandler.RequeuePlayer(player, gameLevel.GameType);
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
