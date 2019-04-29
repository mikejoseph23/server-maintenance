using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace ServerMaintenance.Helpers
{
    public class SqlScriptRunner
    {
        private readonly string _path;
        private readonly string _connectionString = ConfigurationManager.AppSettings["ConnectionString"];

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

            var conn = new SqlConnection(_connectionString);
            var server = new Server(new ServerConnection(conn));
            server.ConnectionContext.ExecuteNonQuery(sqlText);
        }
    }
}
