using System;
using System.Collections.Concurrent;
using System.IO;
using UlteriusServer.TaskServer.Api.Models;
using static System.IO.File;
using static UlteriusServer.TaskServer.Api.Models.FileInformation;

namespace UlteriusServer.Utilities.Files
{
    public class FileManager
    {
        public static ConcurrentDictionary<string, FileInformation> Files =
            new ConcurrentDictionary<string, FileInformation>();

        public static bool AddFile(string sha, FileInformation file)
        {
            return Files.TryAdd(sha, file);
        }

        public static long GetTotalRead(string sha)
        {
            return Files[sha].BytesReceived;
        }

        public static long GetTotalSize(string sha)
        {
            return Files[sha].TotalSize;
        }

        public static FileState CurrentState(string sha)
        {
            return Files[sha].State;
        }

        public static bool Complete(string sha)
        {
            try
            {
                Files[sha].State  = FileState.Complete;
                var fileBytes = Files[sha].FileData;
                var filePath = Files[sha].FileName;
                WriteAllBytes(filePath, fileBytes);
                FileInformation removed;
                Files.TryRemove(sha, out removed);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool AddData(string sha, byte[] data)
        {
            var stream = new MemoryStream();
            stream.Write(Files[sha].FileData, 0, Files[sha].FileData.Length);
            stream.Write(data, 0, data.Length);
            Files[sha].FileData = stream.ToArray();
            Files[sha].BytesReceived = Files[sha].FileData.Length;
            return true;
        }
    }
}