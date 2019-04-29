using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Timer.ServerMaintenance
{
    class Program
    {
        static void Main(string[] args)
        {
            var mm = new MaintenanceMan();
            mm.RunAll();
        }
    }
}
