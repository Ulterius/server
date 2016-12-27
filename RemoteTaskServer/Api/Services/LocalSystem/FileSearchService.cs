#region

using System;
using System.Collections.Generic;
using System.Threading;
using UlteriusServer.Utilities.Files.Database;

#endregion

namespace UlteriusServer.Api.Services.LocalSystem
{
    public class FileSearchService
    {

        public void FileWorkThread()
        {
            try
            {
                _databaseController = new DatabaseController(_cachePath);
                _databaseController.Start();
            }
            catch (Exception ex)
            {
                // log errors
            }
        }

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

            Thread thread = new Thread(FileWorkThread);
            thread.Start();

        }
    }
}