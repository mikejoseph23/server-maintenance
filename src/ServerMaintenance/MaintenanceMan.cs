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

        public void RunAll()
        {
            try
            {
                // Backup SQL Server files.
                var backupScriptPath = _configuration["SqlScriptPath"];
                _log.LogEntries.Add(new ActivityLog.LogEntry(DateTime.Now, "Backing up SQL Server files to " + backupScriptPath));
                var scriptRunner = new SqlScriptRunner(backupScriptPath, _configuration);
                scriptRunner.Run();
                _log.LogEntries.Add(new ActivityLog.LogEntry(DateTime.Now, "Done Backing up SQL Server files."));

                // Delete backups over x days old.
                _log.LogEntries.Add(new ActivityLog.LogEntry(DateTime.Now, "Cleaning up files older than " + _configuration["MaxAgeOfBackupsInDays"] + " days."));
                var cleaner = new FolderCleaner(_configuration["SqlBackupPath"], _configuration);
                cleaner.Run();
                _log.LogEntries.Add(new ActivityLog.LogEntry(DateTime.Now, "Done cleaning up files. " + cleaner.DeletedFiles.Count + " files were deleted."));
                if (cleaner.DeletedFiles.Count > 0) _log.LogEntries.Add(new ActivityLog.LogEntry(DateTime.Now, JsonSerializer.Serialize(cleaner.DeletedFiles, JsonOptions)));
            }
            catch (Exception ex)
            {
                _log.LogEntries.Add(new ActivityLog.LogEntry(DateTime.Now, "Error Occurred", true));
                _log.LogEntries.Add(new ActivityLog.LogEntry(DateTime.Now, JsonSerializer.Serialize(ex, JsonOptions), true));
                throw;
            }
            finally
            {
                // Email a confirmation that this task was performed.
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
