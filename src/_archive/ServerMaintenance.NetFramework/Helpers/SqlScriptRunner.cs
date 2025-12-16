using System;
using System.Configuration;
using System.IO;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace ServerMaintenance.Helpers
{
    public class SqlScriptRunner
    {
        private readonly string _path;
        private readonly string _dbServerName = ConfigurationManager.AppSettings["DbServerName"];

        public SqlScriptRunner(string path)
        {
            if (!File.Exists(path)) throw new Exception("The file '" + path + "' does not exist.");
            _path = path;
        }

        public void Run()
        {
            var sqlText = File.ReadAllText(_path);

            // FUTURE: Make this more configurable:
            sqlText = sqlText.Replace("[BACKUP_PATH]", ConfigurationManager.AppSettings["SqlBackupPath"]);
            var serverConnection = new ServerConnection(_dbServerName);
            var server = new Server(serverConnection);
            server.ConnectionContext.ExecuteNonQuery(sqlText);
        }
    }
}
