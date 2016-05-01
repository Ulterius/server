using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using UlteriusServer.Utilities.Security;

namespace UlteriusServer.Utilities.Files
{
    public class FileManager
    {
        public static ConcurrentDictionary<string, ApprovedFile> Whitelist =
            new ConcurrentDictionary<string, ApprovedFile>();

        public static bool AddFile(string password, string filePath, string fileKey)
        {
            var file = new ApprovedFile
            {
                FileName = Path.GetFileName(filePath),
                DestinationPath = filePath,
                Password = password
            };
            return Whitelist.TryAdd(fileKey, file);
        }

        public static bool OnWhitelist(string fileKey)
        {
            ApprovedFile file;
            return Whitelist.TryGetValue(fileKey, out file);
        }

        public static bool RemoveFile(string fileKey)
        {
            ApprovedFile file;
            return Whitelist.TryRemove(fileKey, out file);
        }

        public static bool DecryptFile(string fileKey, byte[] fileData)
        {
            ApprovedFile file;
            if (Whitelist.TryGetValue(fileKey, out file))
            {
                try
                {
                    var passwordBytes = Encoding.UTF8.GetBytes(file.Password);
                    var decryptedFile = UlteriusAes.DecryptFile(fileData, passwordBytes);
                    var destinationPath = file.DestinationPath;
                    System.IO.File.WriteAllBytes(destinationPath, decryptedFile);
                    RemoveFile(fileKey);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return false;
        }

        public class ApprovedFile
        {
            public string DestinationPath;
            public string FileName;
            public string Password;
        }
    }
}