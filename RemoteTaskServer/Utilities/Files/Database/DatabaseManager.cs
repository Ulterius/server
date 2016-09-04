#region

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using UlteriusServer.Utilities.Files.Ntfs;

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





        public List<string> Search(string value)
        {
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
                    try
                    {
                        var fileNameLength = reader.ReadInt32();
                        var fileName = Encoding.UTF8.GetString(reader.ReadBytes(fileNameLength));
                        var directoryLength = reader.ReadInt32();
                        var directory = Encoding.UTF8.GetString(reader.ReadBytes(directoryLength));
                        var size = reader.ReadInt64();
                        if (Operators.LikeString(fileName, $"*{value}*", CompareMethod.Text))
                        {
                            results.Add(Path.Combine(directory, fileName));
                        }
                    }
                    catch (EndOfStreamException e)
                    {

                        break;
                    }
                }

            }
            return results;
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
                        var driveName = drive.Replace("\\", "").Replace("/", "");
                        Console.WriteLine("scanning " + driveName);
                        var data = new MftEnumerator(driveName);
                        foreach (var currentFile in data)
                        {
                            if (currentFile != null)
                            {
                                try
                                {
                                    var fileName = Encoding.UTF8.GetBytes(Path.GetFileName(currentFile));
                                    var directory = Encoding.UTF8.GetBytes(Path.GetDirectoryName(currentFile) ?? "null");
                                    //slows down the scan

                                    //  long size = WinApi.GetFileSizeA(currentFile);
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
                        data = null;
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
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