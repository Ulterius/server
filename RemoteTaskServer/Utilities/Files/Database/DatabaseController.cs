#region

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace UlteriusServer.Utilities.Files.Database
{
    //keeps everything together
    internal class DatabaseController
    {
        private readonly DatabaseManager _databaseManager;
        private readonly string _path;

        public DatabaseController(string path)
        {
            _path = path;
            _databaseManager = new DatabaseManager(path);
        }



        public string GetCurrentDrive()
        {
            return _databaseManager.CurrentDrive;
        }

        public bool IsScanning()
        {
            return _databaseManager.Scanning;
        }

        private void Scan()
        {
            _databaseManager.Scanning = true;
            _databaseManager.CreateDatabase();
            _databaseManager.Scanning = false;
        }

        public List<string> Search(string keyword)
        {
          
            return _databaseManager.Search(keyword).ToList();
        }

        public bool Refresh()
        {
            return _databaseManager.CreateDatabase();
        }

        public void Start()
        {
            if (System.IO.File.Exists(_path))
            {
                if (_databaseManager.OutOfSync())
                {

                    Scan();
                }
            }
            else
            {
                Console.WriteLine("Getting ready to scan");
                Scan();
            }
        }
    }
}