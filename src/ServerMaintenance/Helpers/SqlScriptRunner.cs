using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace ServerMaintenance.Helpers
{
    public class SqlScriptRunner
    {
        private readonly string _path;
        private readonly IConfiguration _configuration;

        public SqlScriptRunner(string path, IConfiguration configuration)
        {
            if (!File.Exists(path)) throw new Exception("The file '" + path + "' does not exist.");
            _path = path;
            _configuration = configuration;
        }

        public void Run()
        {
            var sqlText = File.ReadAllText(_path);

            sqlText = sqlText.Replace("[BACKUP_PATH]", _configuration["SqlBackupPath"]);
            sqlText = sqlText.Replace("[EXCLUDED_DATABASES]", GetExcludedDatabaseList());
            var serverConnection = new ServerConnection(_configuration["DbServerName"]);
            var server = new Server(serverConnection);
            server.ConnectionContext.ExecuteNonQuery(sqlText);
        }

        /// <summary>
        /// Builds the quoted NOT IN list for the backup cursor: the four system
        /// databases, plus anything named in the ExcludedDatabases setting.
        /// </summary>
        /// <remarks>
        /// Per-server exclusions belong in configuration, not in a hand-edited copy
        /// of the .sql on each box. iadev-prod had one skipping RpsCompare_UAT and
        /// a deploy silently replaced it with the repo version.
        /// </remarks>
        private string GetExcludedDatabaseList()
        {
            var names = new List<string> { "master", "model", "msdb", "tempdb" };

            foreach (var child in _configuration.GetSection("ExcludedDatabases").GetChildren())
            {
                if (!string.IsNullOrWhiteSpace(child.Value)) names.Add(child.Value.Trim());
            }

            return string.Join(", ", names.Select(n => "'" + n.Replace("'", "''") + "'"));
        }
    }
}
