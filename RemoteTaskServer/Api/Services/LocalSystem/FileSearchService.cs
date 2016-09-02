#region

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UlteriusServer.Utilities.Files.Database;

#endregion

namespace UlteriusServer.Api.Services.LocalSystem
{
    public class FileSearchService
    {
        private readonly string _cachePath;
        private DatabaseController _databaseController;

        public FileSearchService(string path)
        {
            _cachePath = path;
        }

        public string CurrentScanDrive()
        {
            return _databaseController.GetCurrentDrive();
        }

        public bool IsScanning()
        {
            return _databaseController.IsScanning();
        }

        public List<string> Search(string keyword)
        {
            return _databaseController?.Search(keyword);
        }

        public void Start()
        {
            Task.Run(() => {
                _databaseController = new DatabaseController(_cachePath);
                _databaseController.Start();
                Console.WriteLine("File Database Ready");
            });
        }
    }
}