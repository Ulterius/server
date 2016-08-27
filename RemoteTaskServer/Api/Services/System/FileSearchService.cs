#region

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UlteriusServer.Utilities.Files.Database;

#endregion

namespace UlteriusServer.Api.Services.System
{
    public class FileSearchService
    {
        private readonly string _cachePath;
        private DatabaseController databaseController;

        public FileSearchService(string path)
        {
            _cachePath = path;
        }

        public string CurrentScanDrive()
        {
            return databaseController.GetCurrentDrive();
        }

        public bool IsScanning()
        {
            return databaseController.IsScanning();
        }

        public List<string> Search(string keyword)
        {
            return databaseController?.Search(keyword);
        }

        public void Start()
        {
            Task.Run(() => {
                databaseController = new DatabaseController(_cachePath);
                databaseController.Start();
                Console.WriteLine("File Database Ready");
            });
        }
    }
}