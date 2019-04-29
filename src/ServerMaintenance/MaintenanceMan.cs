using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ServerMaintenance.Helpers;

namespace ServerMaintenance
{
    public class MaintenanceMan
    {
        private ActivityLog _log;

        public MaintenanceMan()
        {
            _log = new ActivityLog();
        }

        public void RunAll()
        {
            var jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            try
            {
                // Backup SQL Server files.
                var backupScriptPath = ConfigurationManager.AppSettings["SqlScriptPath"];
                _log.LogEntries.Add(new ActivityLog.LogEntry(DateTime.Now, "Backing up SQL Server files to " + backupScriptPath));
                var scriptRunner = new SqlScriptRunner(backupScriptPath);
                scriptRunner.Run();
                _log.LogEntries.Add(new ActivityLog.LogEntry(DateTime.Now, "Done Backing up SQL Server files."));

                // Delete backups over x days old.
                _log.LogEntries.Add(new ActivityLog.LogEntry(DateTime.Now, "Cleaning up files older than " + ConfigurationManager.AppSettings["MaxAgeOfBackupsInDays"] + " days."));
                var cleaner = new FolderCleaner(ConfigurationManager.AppSettings["SqlBackupPath"]);
                cleaner.Run();
                _log.LogEntries.Add(new ActivityLog.LogEntry(DateTime.Now, "Done cleaning up files. " + cleaner.DeletedFiles.Count + " files were deleted."));
                if (cleaner.DeletedFiles.Count > 0) _log.LogEntries.Add(new ActivityLog.LogEntry(DateTime.Now, JsonConvert.SerializeObject(cleaner.DeletedFiles)));
            }
            catch (Exception ex)
            {
                _log.LogEntries.Add(new ActivityLog.LogEntry(DateTime.Now, "Error Occurred", true));
                _log.LogEntries.Add(new ActivityLog.LogEntry(DateTime.Now, JsonConvert.SerializeObject(ex, Formatting.Indented, jsonSettings), true));
                throw ex;
            }
            finally
            {
                // TODO: Email a confirmation that this task was performed.
                var msg = new System.Net.Mail.MailMessage();
                msg.To.Add(new MailAddress(ConfigurationManager.AppSettings["NotificationRecipient"]));
                msg.From = new MailAddress(ConfigurationManager.AppSettings["NotificationFromAddress"]);
                msg.Subject = ConfigurationManager.AppSettings["NotificationSubject"];

                if (_log.HasErrors)
                {
                    msg.Subject = "Error Occurred " + msg.Subject;
                    msg.Priority = MailPriority.High;
                }

                msg.Body = "Activity Log: \n\n";
                msg.Body += JsonConvert.SerializeObject(_log, Formatting.Indented, jsonSettings);
                msg.IsBodyHtml = false;
                var client = new SmtpClient();
                client.Send(msg);
            }
        }
    }
}
