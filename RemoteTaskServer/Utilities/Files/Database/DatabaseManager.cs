#region

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;


#endregion

namespace UlteriusServer.Utilities.Files.Database
{
    //handles data saving/loading, appending/reading
    internal class DatabaseManager
    {
        private readonly string _path;


        public DatabaseManager(string path)
        {
            _path = path;
        }

        public string CurrentDrive { get; set; }




        public IEnumerable<string>  Search(string value)
        {
            if (value.Contains("*"))
            {
                value = value.Replace("*", "");
            }
            var results = new List<string>();
            using (var file = System.IO.File.OpenRead(_path))
            using (var deflate = new DeflateStream(file, CompressionMode.Decompress))
            using (var reader = new BinaryReader(deflate))
            {
                //read the metadata
                var lastUpdate = reader.ReadInt64();
                //read until exception
                while (true)
                {

                    int fileNameLength;
                    string fileName;
                    int directoryLength;
                    string directory;
                    long size;
                    try
                    {
                        fileNameLength = reader.ReadInt32();
                        fileName = Encoding.UTF8.GetString(reader.ReadBytes(fileNameLength));
                        directoryLength = reader.ReadInt32();
                        directory = Encoding.UTF8.GetString(reader.ReadBytes(directoryLength));
                        size = reader.ReadInt64();
                    }
                    catch (EndOfStreamException)
                    {

                        break;
                    }
                    if (Operators.LikeString(fileName, $"*{value}*", CompareMethod.Text))
                    {
                        yield return Path.Combine(directory, fileName);
                    }
                }
            }
        }

      
        public bool OutOfSync()
        {
            return NeedsUpdate();
        }

        private DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }


        private bool NeedsUpdate()
        {
            using (var file = System.IO.File.OpenRead(_path))
            using (var deflate = new DeflateStream(file, CompressionMode.Decompress))
            using (var reader = new BinaryReader(deflate))
            {
                try
                {
                    var lastUpdate = UnixTimeStampToDateTime(reader.ReadInt64());

                    var hours = (DateTime.Now - lastUpdate).TotalHours;
                    //update the DB once every 24 hours
                    if (hours > 24)
                    {

                        return true;
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Database broke, redo.");
                    return true;
                }
                return false;
            }
        }

        public static IEnumerable<string> GetFileList(string fileSearchPattern, string rootFolderPath)
        {
            var pending = new Queue<string>();
            pending.Enqueue(rootFolderPath);
            string[] tmp;
            while (pending.Count > 0)
            {
                rootFolderPath = pending.Dequeue();
                try
                {
                    tmp = Directory.GetFiles(rootFolderPath, fileSearchPattern);
                }
                catch (Exception)
                {
                    continue;
                }
                foreach (var t in tmp)
                {
                    yield return t;
                }
                try
                {
                    tmp = Directory.GetDirectories(rootFolderPath);
                }
                catch (Exception)
                {
                    continue;
                }
                foreach (var t in tmp)
                {
                    pending.Enqueue(t);
                }
            }
        }


        public bool CreateDatabase()
        {
            using (var file = System.IO.File.Create(_path))
            using (var deflate = new DeflateStream(file, CompressionMode.Compress))
            using (var writer = new BinaryWriter(deflate))
            {
                var dateTime = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds;
                writer.Write(dateTime);
                var drives = Directory.GetLogicalDrives();
                foreach (var drive in drives)
                {
                    try
                    {
                        CurrentDrive = drive;
                        Scanning = true;
                        var driveName = drive.Replace("\\", "/");
                        Console.WriteLine("scanning " + driveName);
                        foreach (var i in GetFileList("*.*", driveName))
                        {
                            try
                            {
                                var fileName = Encoding.UTF8.GetBytes(Path.GetFileName(i));
                                var directory = Encoding.UTF8.GetBytes(Path.GetDirectoryName(i) ?? "null");
                                long size = -1;
                                writer.Write(fileName.Length);
                                writer.Write(fileName);
                                writer.Write(directory.Length);
                                writer.Write(directory);
                                writer.Write(size);
                            }
                            catch (Exception)
                            {
                                //path too long
                            }
                        }
                    }
                    catch (Exception)
                    {

                        Scanning = false;
                    }
                }
                Scanning = false;
                return true;
            }
        }

        public bool Scanning { get; set; }
    }
}