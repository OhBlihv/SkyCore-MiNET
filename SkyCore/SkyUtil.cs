using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net.Core;

namespace SkyCore
{
    public class SkyUtil
    {

        public static bool log(String line)
		{
            Console.WriteLine("[SkyCore] " + line);

	        return true;
        }
        
    }
}
