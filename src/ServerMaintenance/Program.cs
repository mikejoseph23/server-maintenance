using System;
using Microsoft.Extensions.Configuration;

namespace ServerMaintenance
{
    class Program
    {
        static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var mm = new MaintenanceMan(configuration);
            mm.RunAll();
        }
    }
}
