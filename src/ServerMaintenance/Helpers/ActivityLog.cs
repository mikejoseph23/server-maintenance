using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerMaintenance.Helpers
{
    public class ActivityLog
    {
        public List<LogEntry> LogEntries { get; set; }

        public ActivityLog()
        {
            LogEntries = new List<LogEntry>();
        }

        public class LogEntry
        {
            public LogEntry(DateTime date, string description, bool error = false)
            {
                TheDate = date;
                Description = description;
                Error = error;
            }

            public DateTime TheDate { get; set; }

            public string Description { get; set; }

            public bool Error { get; set; }
        }

        public bool HasErrors
        {
            get { return LogEntries.Count(x => x.Error) > 0; }
        }
    }
}
