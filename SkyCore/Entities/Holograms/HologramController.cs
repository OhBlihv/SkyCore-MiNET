using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiNET.Utils;
using MiNET.Worlds;

namespace SkyCore.Entities.Holograms
{
    class HologramController
    {

        private static readonly List<Hologram> _hologramList = new List<Hologram>();

        private Thread _tickingThread = null;
        private bool _isTicking = false;

        private void KickStartThread()
        {
            if (!_isTicking)
            {
                if (_tickingThread != null && _tickingThread.IsAlive)
                {
                    _tickingThread.Abort();
                    _tickingThread = null;
                }
                
                _tickingThread = new Thread(HologramTicker);
            }
        }
        
        public static void AddHologram(Level level, PlayerLocation spawnLocation, Delegate hologramRunnable)
        {
            
        }

        public static void RemoveHologram(Hologram hologram)
        {
            _hologramList.Remove(hologram);
            hologram.DespawnEntity();
        }

        private void HologramTicker()
        {
            while (_isTicking)
            {
                foreach (Hologram hologram in _hologramList)
                {
                    if (hologram is TickingHologram)
                    {
                        hologram.OnTick();
                    }
                }
            }
        }

    }
}
