using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
   
        public static bool AddFile(AuthClient client, string filePath, string synckey)
        {
            var file = new ApprovedFile
            {
                FileName = Path.GetFileName(filePath),
                DestinationPath = filePath,
                Client = client
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
                    var client = file.Client;
                    var destinationPath = file.DestinationPath;
                    var keybytes = Encoding.UTF8.GetBytes(Rsa.SecureStringToString(client.AesKey));
                    var iv = Encoding.UTF8.GetBytes(Rsa.SecureStringToString(client.AesIv));
                    var decryptedData = Security.Aes.Decrypt(fileData, keybytes, iv);
                    byte[] bytes = new byte[decryptedData.Length * sizeof(char)];
                    System.Buffer.BlockCopy(decryptedData.ToCharArray(), 0, bytes, 0, bytes.Length);
                    System.IO.File.WriteAllBytes(destinationPath, bytes);
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
            public AuthClient Client;
        }
    }
}