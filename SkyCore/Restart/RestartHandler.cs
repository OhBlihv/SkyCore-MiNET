using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MiNET.Utils;
using SkyCore.Game;
using SkyCore.Game.Level;
using SkyCore.Player;
using SkyCore.Util;
using Timer = System.Threading.Timer;

namespace SkyCore.Restart
{
	public class RestartHandler
	{

		private static readonly RestartTask RestartTask = new RestartTask(DateTime.Now.AddHours(12)); //Default to 12 hours from now
		public static Timer Timer;

		public static void Start()
		{
			if (Timer != null)
			{
				throw new Exception("RestartHandler already running!");
			}
			
			RunnableTask.RunTask(() =>
			{
				var autoEvent = new AutoResetEvent(false);

				Timer = new Timer(RestartTask.RestartTick, autoEvent, 1000, 1000);

				autoEvent.WaitOne();

				TriggerReboot();
			});
		}

		public static void TriggerReboot()
		{
			if (Timer != null)
			{
				Timer.Dispose();
				Timer = null;
			}
			else
			{
				SkyUtil.log("Attempted to double-trigger a reboot! Ignoring...");
				return; //If Timer == null, there should already be a reboot happening.
			}
			
			SkyUtil.log("Shutting down using Restart Handler");
			SkyUtil.log("Moving all players to first available hub");

			//Remove this instance from the game pool
			ushort.TryParse(Config.GetProperty("port", "19132"), out var hostPort);
			string gameType = SkyCoreAPI.Instance.GameType;
			if (ExternalGameHandler.GameRegistrations.TryGetValue(gameType, out GamePool gamePool))
			{
				gamePool.RemoveInstance(new InstanceInfo { HostAddress = "local", HostPort = hostPort });
			}

			//Remove ourselves from the list, and force players to a new hub
			if (gamePool != null && gameType.Equals("hub"))
			{
				if (gamePool.GetAllInstances().Count > 0)
				{
					foreach (SkyPlayer player in SkyCoreAPI.Instance.GetAllOnlinePlayers())
					{
						ExternalGameHandler.AddPlayer(player, "hub");
					}

					//Wait for players to leave
					Thread.Sleep(5000);
				}
				//else - No hub servers to send players to. Forfeit all players.
			}
			else
			{
				SkyCoreAPI.IsRebootQueued = true;

				if (SkyCoreAPI.Instance.GameModes.TryGetValue(gameType, out CoreGameController localGameController) && 
				    !localGameController.GameLevels.IsEmpty)
				{
					ICollection<GameLevel> gameLevels = localGameController.GameLevels.Values;

					long expiryForceTime = DateTimeOffset.Now.ToUnixTimeSeconds() + 300; //5 minute timeout if the game is still running
					int totalPlayers;

					do
					{
						totalPlayers = 0;

						foreach (GameLevel gameLevel in gameLevels)
						{
							if (gameLevel.CurrentState.GetEnumState(gameLevel).IsJoinable())
							{
								//We're allowed to kick all players
								foreach (SkyPlayer player in gameLevel.GetAllPlayers())
								{
									ExternalGameHandler.AddPlayer(player, "hub");
								}
							}
							else
							{
								totalPlayers += gameLevel.GetAllPlayers().Count;
							}
						}

						if (totalPlayers > 0)
						{
							SkyUtil.log(
								$"Waiting for {totalPlayers} to finish games before closing this instance {expiryForceTime - DateTimeOffset.Now.ToUnixTimeSeconds()} seconds remaining.");

							Thread.Sleep(15000); //Check every 1 second if the game has finished
						}
					} while (totalPlayers > 0 && DateTimeOffset.Now.ToUnixTimeSeconds() < expiryForceTime);
				}
			}

			SkyUtil.log("Shutting down...");

			//Start Actual Shutdown

			SkyCoreAPI.Instance.OnDisable();
			SkyCoreAPI.Instance.Context.PluginManager.Plugins.Remove(SkyCoreAPI.Instance);
			SkyCoreAPI.Instance.Context.Server.StopServer();

			Environment.Exit(0);
		}
		
	}

	class RestartTask
	{

		public DateTime RestartTime;

		public RestartTask(DateTime restartTime)
		{
			RestartTime = restartTime;
		}

		public void RestartTick(Object stateInfo)
		{
			try
			{
				DateTime currentTime = DateTime.Now;

				int secondsBetween = GetSecondDifference(currentTime, RestartTime);

				//Console.WriteLine($"({secondsBetween} Seconds To Reboot).", currentTime, RestartTime);

				switch (secondsBetween)
				{
					case 0: 
						((AutoResetEvent) stateInfo).Set(); //Trigger Shutdown of Timer
						break;

					//Countdowns
					case 60: SkyUtil.log("Rebooting in 60 Seconds");
						break;
					case 30: SkyUtil.log("Rebooting in 30 Seconds");
						break;
					case 10: SkyUtil.log("Rebooting in 10 Seconds");
						break;
					case 5: SkyUtil.log("Rebooting in 5 Seconds");
						break;
					case 3: SkyUtil.log("Rebooting in 3 Seconds");
						break;
					case 2: SkyUtil.log("Rebooting in 2 Seconds");
						break;
					case 1: SkyUtil.log("Rebooting in 1 Seconds");
						break;
						
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}

		private int GetSecondDifference(DateTime currentTime, DateTime futureTime)
		{
			return (int) (futureTime - currentTime).TotalSeconds;
		}

	}
	
}
