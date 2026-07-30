using System;
using Microsoft.Extensions.Configuration;

namespace ServerMaintenance
{
    class Program
    {
        // Returns 0 on success, 1 if any maintenance step failed. Task Scheduler
        // surfaces this as LastTaskResult, so a bad night is visible there even when
        // the notification email does not arrive. Previously the process died on an
        // unhandled exception and reported 0xE0434352 - a CLR crash code that says
        // nothing about what actually went wrong.
        static int Main(string[] args)
        {
            try
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                    .Build();

                var mm = new MaintenanceMan(configuration);
                return mm.RunAll() ? 0 : 1;
            }
            catch (Exception ex)
            {
                // Only reachable if configuration itself is unusable, in which case
                // there is no way to know where to send mail.
                Console.Error.WriteLine("Maintenance run failed to start: " + ex);
                return 1;
            }
        }
    }
}
