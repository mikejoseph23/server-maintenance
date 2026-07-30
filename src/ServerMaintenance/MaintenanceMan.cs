using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ServerMaintenance.Helpers;

namespace ServerMaintenance
{
    public class MaintenanceMan
    {
        private readonly ActivityLog _log;
        private readonly IConfiguration _configuration;
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        public MaintenanceMan(IConfiguration configuration)
        {
            _log = new ActivityLog();
            _configuration = configuration;
        }

        /// <summary>
        /// Runs every maintenance step and reports the outcome. Returns false if any
        /// step failed.
        /// </summary>
        /// <remarks>
        /// The steps are deliberately independent. They used to share one try block,
        /// and the catch rethrew, so a failed backup killed the process before cleanup
        /// ever ran - old .BAK files then piled up untouched for months while the
        /// retention setting looked like it was being honoured. A step that fails now
        /// gets logged and the next one still runs.
        /// </remarks>
        public bool RunAll()
        {
            RunBackups();
            RunCleanup();
            SendNotification();

            return !_log.HasErrors;
        }

        private void RunBackups()
        {
            try
            {
                var backupScriptPath = _configuration["SqlScriptPath"];
                _log.LogEntries.Add(new ActivityLog.LogEntry(DateTime.Now, "Backing up SQL Server files to " + backupScriptPath));
                var scriptRunner = new SqlScriptRunner(backupScriptPath, _configuration);
                scriptRunner.Run();
                _log.LogEntries.Add(new ActivityLog.LogEntry(DateTime.Now, "Done Backing up SQL Server files."));
            }
            catch (Exception ex)
            {
                LogError("Error Occurred backing up SQL Server files.", ex);
            }
        }

        private void RunCleanup()
        {
            try
            {
                // Delete backups over x days old.
                _log.LogEntries.Add(new ActivityLog.LogEntry(DateTime.Now, "Cleaning up files older than " + _configuration["MaxAgeOfBackupsInDays"] + " days."));
                var cleaner = new FolderCleaner(_configuration["SqlBackupPath"], _configuration);
                cleaner.Run();
                _log.LogEntries.Add(new ActivityLog.LogEntry(DateTime.Now, "Done cleaning up files. " + cleaner.DeletedFiles.Count + " files were deleted."));
                if (cleaner.DeletedFiles.Count > 0) _log.LogEntries.Add(new ActivityLog.LogEntry(DateTime.Now, JsonSerializer.Serialize(cleaner.DeletedFiles, JsonOptions)));
            }
            catch (Exception ex)
            {
                LogError("Error Occurred cleaning up old backups.", ex);
            }
        }

        /// <summary>
        /// Email a confirmation that this task was performed.
        /// </summary>
        private void SendNotification()
        {
            try
            {
                var msg = new MailMessage();
                var recipients = _configuration["Notification:Recipients"].Replace(";", ",").Split(',');

                foreach (var recipient in recipients)
                {
                    msg.To.Add(new MailAddress(recipient.Trim()));
                }

                msg.From = new MailAddress(_configuration["Notification:FromAddress"]);
                msg.Subject = _configuration["Notification:Subject"];

                if (_log.HasErrors)
                {
                    msg.Subject = "Error Occurred " + msg.Subject;
                    msg.Priority = MailPriority.High;
                }

                msg.Body = GetEmailBody();
                msg.IsBodyHtml = false;

                using var client = CreateSmtpClient();
                client.Send(msg);
            }
            catch (Exception ex)
            {
                // Losing the report is itself a failure worth a non-zero exit code -
                // it is the only thing that makes a bad night visible.
                LogError("Error Occurred sending the notification email.", ex);
                Console.Error.WriteLine("Failed to send notification email: " + ex.Message);
            }
        }

        /// <summary>
        /// Records a failed step. Must never throw - it is the last thing standing
        /// between a bad night and a silent one.
        /// </summary>
        /// <remarks>
        /// This used to serialize the exception with System.Text.Json, which refuses
        /// to write Exception.TargetSite (a MethodBase) and threw NotSupportedException
        /// from inside the catch block - taking down the run it was supposed to report.
        /// ToString() gives the message, the inner exceptions and the stack trace,
        /// which is everything the email needs anyway.
        /// </remarks>
        private void LogError(string description, Exception ex)
        {
            _log.LogEntries.Add(new ActivityLog.LogEntry(DateTime.Now, description, true));
            _log.LogEntries.Add(new ActivityLog.LogEntry(DateTime.Now, ex.ToString(), true));
        }

        private SmtpClient CreateSmtpClient()
        {
            var deliveryMethod = _configuration["Smtp:DeliveryMethod"];

            if (string.Equals(deliveryMethod, "SpecifiedPickupDirectory", StringComparison.OrdinalIgnoreCase))
            {
                return new SmtpClient
                {
                    DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                    PickupDirectoryLocation = _configuration["Smtp:PickupDirectoryLocation"]
                };
            }
            else
            {
                var client = new SmtpClient
                {
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Host = _configuration["Smtp:Host"],
                    Port = int.Parse(_configuration["Smtp:Port"] ?? "25"),
                    EnableSsl = bool.Parse(_configuration["Smtp:EnableSsl"] ?? "false")
                };

                var username = _configuration["Smtp:Username"];
                var password = _configuration["Smtp:Password"];
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    client.Credentials = new NetworkCredential(username, password);
                }

                return client;
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
