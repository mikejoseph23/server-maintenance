using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace ServerMaintenance.Helpers
{
    internal class FolderCleaner
    {
        private readonly string _folderPath;

        public List<DeletedFile> DeletedFiles { get; set; }

        public FolderCleaner(string folderPath)
        {
            DeletedFiles = new List<DeletedFile>();
            if (!Directory.Exists(folderPath)) throw new Exception("The folder '" + folderPath + "' does not exist.");
            _folderPath = folderPath;
        }

        public void Run(bool recursive = false)
        {
            var di = new DirectoryInfo(_folderPath);
            var expirationDate = DateTime.Now.AddDays(-(int.Parse(ConfigurationManager.AppSettings["MaxAgeOfBackupsInDays"])));

            foreach (var fi in di.GetFiles())
            {
                if (fi.LastWriteTime >= expirationDate) continue;
                DeletedFiles.Add(new DeletedFile() { DateEntered = DateTime.Now, LastWriteTime = fi.LastWriteTime, FilePath = fi.FullName });
                File.Delete(fi.FullName);
            }
        }

        public class DeletedFile
        {
            public DateTime DateEntered { get; set; }

            public DateTime LastWriteTime { get; set; }

            public string FilePath { get; set; }
        }
    }
}