﻿using System;
using System.Configuration;
using System.Net.Mail;
using System.Text;
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
                // Email a confirmation that this task was performed.
                var msg = new MailMessage();
                var recipients = ConfigurationManager.AppSettings["NotificationRecipient"].Replace(";", ",").Split(',');

                foreach (var recipient in recipients)
                {
                    msg.To.Add(new MailAddress(recipient.Trim()));
                }

                msg.From = new MailAddress(ConfigurationManager.AppSettings["NotificationFromAddress"]);
                msg.Subject = ConfigurationManager.AppSettings["NotificationSubject"];

                if (_log.HasErrors)
                {
                    msg.Subject = "Error Occurred " + msg.Subject;
                    msg.Priority = MailPriority.High;
                }

                msg.Body = GetEmailBody();
                msg.IsBodyHtml = false;
                var client = new SmtpClient();
                client.Send(msg);
            }
        }

        private string GetEmailBody()
        {
            var sb = new StringBuilder();

            foreach (var log in _log.LogEntries)
            {
                var code = log.Error ? "ERROR" : "FYI";
                var line = $"{log.TheDate.ToShortDateString()} {log.TheDate.ToShortTimeString()} - {code} - {log.Description}";
                sb.AppendLine(line);
            }

            return sb.ToString();
        }
    }
}
