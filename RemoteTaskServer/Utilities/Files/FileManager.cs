using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using MiscUtil.Xml.Linq.Extensions;
using UlteriusServer.Authentication;
using UlteriusServer.TaskServer.Api.Models;
using UlteriusServer.Utilities.Security;
using static UlteriusServer.TaskServer.Api.Models.FileInformation;
using Aes = System.Security.Cryptography.Aes;

namespace UlteriusServer.Utilities.Files
{
    public class FileManager
    {
        public static ConcurrentDictionary<string, ApprovedFile> Whitelist = new ConcurrentDictionary<string, ApprovedFile>();
   
        public static bool AddFile(string password, string filePath, string synckey)
        {
            var file = new ApprovedFile
            {
                FileName = Path.GetFileName(filePath),
                DestinationPath = filePath,
                Password = password
            };      
            return Whitelist.TryAdd(synckey, file);
        }

        public static bool OnWhitelist(string syncKey)
        {
            ApprovedFile file;
            return Whitelist.TryGetValue(syncKey, out file);

        }

        public static bool RemoveFile(string syncKey)
        {
            ApprovedFile file;
            return Whitelist.TryRemove(syncKey, out file);
        }

        public static bool DecryptFile(string syncKey, byte[] fileData)
        {
            ApprovedFile file;
            if (Whitelist.TryGetValue(syncKey, out file))
            {
                try
                {

                    byte[] passwordBytes = Encoding.UTF8.GetBytes(file.Password);
                    passwordBytes = SHA256.Create().ComputeHash(passwordBytes);
                    var decryptedFile = Utilities.Security.Aes.DecryptFile(fileData, passwordBytes);
                    var destinationPath = file.DestinationPath;
                    System.IO.File.WriteAllBytes(destinationPath, decryptedFile);
                    RemoveFile(syncKey);
                    return true;
                }
                catch (Exception)
                {

                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public class ApprovedFile
        {
            public string FileName;
            public string DestinationPath;
            public string Password;
        }
    }
}