using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using MiNET.Utils;
using SkyCore.Game.Level;
using SkyCore.Player;

namespace SkyCore.Game.State.Impl
{
    public abstract class RunningState : GameState
    {

	    private class AfkPlayer
	    {

		    public int afkTime = 0;

	    }

	    public readonly Random Random = new Random();

	    public int MaxGameTime { get; set; } = 120;
		public int EndTick { get; set; } = -1; //Default value

		public int CurrentTick { get; protected set; }

		//

		private IDictionary<MiNET.Player, AfkPlayer> _afkCheckPlayers = new Dictionary<MiNET.Player, AfkPlayer>();

		public override void EnterState(GameLevel gameLevel)
	    {
		    foreach (MiNET.Player player in gameLevel.Players.Values)
		    {
				_afkCheckPlayers.Add(player, new AfkPlayer());
		    }
	    }

		public override void LeaveState(GameLevel gameLevel)
	    {
		    //
	    }

		public override bool CanAddPlayer(GameLevel gameLevel)
        {
            return false;
        }

        public override void HandleLeave(GameLevel gameLevel, SkyPlayer player)
        {
	        _afkCheckPlayers.Remove(player);

	        //Simulate removal by setting teams
	        gameLevel.SetPlayerTeam(player, null);
		}

        public override void OnTick(GameLevel gameLevel, int currentTick, out int outTick)
        {
            outTick = currentTick;

	        CurrentTick = currentTick;

			//Execute every 5 seconds (10 ticks)
	        if (currentTick % 10 == 0)
	        {
		        List<MiNET.Player> playersToRemove = null,
			                       nonAfkPlayers = null;

		        foreach (KeyValuePair<MiNET.Player, AfkPlayer> entry in _afkCheckPlayers)
		        {
			        PlayerLocation  currentLocation = entry.Key.KnownPosition,
									spawnLocation = entry.Key.SpawnPosition;

			        if (
				        Math.Abs(currentLocation.X - spawnLocation.X) < 2 &&
				        Math.Abs(currentLocation.Y - spawnLocation.Y) < 2 &&
				        Math.Abs(currentLocation.Z - spawnLocation.Z) < 2
			        )
			        {
				        entry.Value.afkTime++;

						//>6 = >30 seconds AFK
				        if (entry.Value.afkTime > 6)
				        {
					        if (playersToRemove == null)
					        {
								playersToRemove = new List<MiNET.Player>();
					        }

							playersToRemove.Add(entry.Key);
						}
			        }
			        else
			        {
				        if (nonAfkPlayers == null)
				        {
							nonAfkPlayers = new List<MiNET.Player>();
				        }
				        nonAfkPlayers.Add(entry.Key);
					}
		        }

		        if (nonAfkPlayers != null && nonAfkPlayers.Count > 0)
		        {
			        foreach (MiNET.Player player in nonAfkPlayers)
			        {
				        _afkCheckPlayers.Remove(player);
			        }
		        }

				if (playersToRemove != null && playersToRemove.Count > 0)
		        {
			        foreach (MiNET.Player player in playersToRemove)
			        {
				        if (player != null)
				        {
					        ExternalGameHandler.AddPlayer(player as SkyPlayer, "hub");

					        player.DespawnEntity();
						}

				        gameLevel.RemovePlayer(player);
						_afkCheckPlayers.Remove(player);
			        }
				}
	        }
        }

        public override StateType GetEnumState(GameLevel gameLevel)
        {
            return StateType.Running;
        }

	    public override bool DoInteractAtEntity(GameLevel gameLevel, int interactId, SkyPlayer player, SkyPlayer target)
	    {
		    return true;
	    }

	    public int GetSecondsLeft()
	    {
		    return (EndTick - CurrentTick) / 2;
	    }

	    public string GetNeatTimeRemaining(int secondsLeft)
	    {
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

		    return neatRemaining;
	    }

	}
}
