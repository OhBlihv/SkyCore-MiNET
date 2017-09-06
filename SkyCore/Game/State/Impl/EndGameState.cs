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

namespace SkyCore.Game.State.Impl
{
    public abstract class EndGameState : GameState
    {
        public override void EnterState(GameLevel gameLevel)
        {
			ThreadPool.QueueUserWorkItem(state =>
			{
				try
				{
					Thread.Sleep(5000);
					gameLevel.DoForAllPlayers(player =>
					{
						player.SendTitle("§7in 5 seconds...", TitleType.SubTitle);
						player.SendTitle("§c§lGAME RESTARTING");
					});
					Thread.Sleep(5000);

					MiNET.Player[] remainingPlayers = gameLevel.GetAllPlayers();
					if (remainingPlayers.Length > 0)
					{
						Level hubLevel = SkyCoreAPI.Instance.Context.LevelManager.Levels.FirstOrDefault(l => l.LevelId.Equals("Overworld", StringComparison.InvariantCultureIgnoreCase));

						foreach (MiNET.Player player in remainingPlayers)
						{
							if (hubLevel == null)
							{
								//TODO: Avoid kicking them?
								player.Disconnect("Unable to enter hub.");
							}
							else
							{
								hubLevel.AddPlayer(player, true);
							}
						}

						Thread.Sleep(2000);
					}

					gameLevel.UpdateGameState(new VoidGameState());
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
			});
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
