#region

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security;
using System.Text;
using UlteriusServer.Utilities.Extensions;
using UlteriusServer.Utilities.Security;

#endregion

namespace UlteriusServer.Utilities.Files
{
    public class FileManager
    {
        //A thread safe collection that stores a list of approved files.
        public static ConcurrentDictionary<string, ApprovedFile> Whitelist =
            new ConcurrentDictionary<string, ApprovedFile>();

        //Add a file to the white list 
        public static bool AddFile(SecureString password, string filePath, string fileKey)
        {
            //Create a new approved file
            var file = new ApprovedFile
            {
                FileName = Path.GetFileName(filePath),
                DestinationPath = filePath,
                Password = password
            };
            return Whitelist.TryAdd(fileKey, file);
        }
        //Checks if a file is on the white list based on its file key
        public static bool OnWhitelist(string fileKey)
        {
            ApprovedFile file;
            return Whitelist.TryGetValue(fileKey, out file);
        }
        //Remove a file from the white list
        public static bool RemoveFile(string fileKey)
        {
            ApprovedFile file;
            return Whitelist.TryRemove(fileKey, out file);
        }
        //Decrypts a file to be stored on the local computer
        public static bool DecryptFile(string fileKey, byte[] fileData)
        {

            ApprovedFile file;
            //Check if the file is on the white list
            if (Whitelist.TryGetValue(fileKey, out file))
            {
                try
                {
                    //decrypt the file
                    var passwordBytes = Encoding.UTF8.GetBytes(file.Password.ToUnsecureString());
                    var decryptedFile = UlteriusAes.DecryptFile(fileData, passwordBytes);
                    var destinationPath = file.DestinationPath;
                    //Write the files data to disk
                    System.IO.File.WriteAllBytes(destinationPath, decryptedFile);
                    //Remove the file from the white list
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
        //A simple model that lets us store where the file should go, its name and the password that protects it. 
        public class ApprovedFile
        {
            public string DestinationPath;
            public string FileName;
            public SecureString Password;
        }
    }
}