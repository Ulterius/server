using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Ionic.Zip;

namespace Bootstrapper
{
    public static class Tools
    {
        private static bool IsValidSha1(string sha1)
        {
            return new Regex("[a-fA-F0-9]{40}").IsMatch(sha1);
        }

        private static MainWindow mainWindow;
        private static async Task<bool> DownloadUpdate()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(5);
                    using (var response = await httpClient.GetAsync("https://ulterius.io/updates/server.zip"))
                    {
                        response.EnsureSuccessStatusCode();
                        using (
                            var fileStream = new FileStream("server.zip", FileMode.Create, FileAccess.Write,
                                FileShare.None))
                        {
                            //copy the content from response to filestream
                            await response.Content.CopyToAsync(fileStream);
                            return true;
                        }
                    }
                }
            }
            catch (Exception)
            {
              //  MessageBox.Show(mainWindow, "Error, please restart and try again: " + ex.Message);
                return false;
            }
        }

        private static void WriteFailure(string message)
        {
            mainWindow.calculationProgressBar.Value = 100;
            mainWindow.UpdateMessage(message);
        }
        public static string GetSha1Hash(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            {
                using (var sha = new SHA1Managed())
                {
                    var checksum = sha.ComputeHash(stream);
                    return BitConverter.ToString(checksum)
                        .Replace("-", string.Empty);
                }
            }
        }

        private static bool ExtractUpdate(string remoteHash)
        {
            var sha = GetSha1Hash("server.zip");
            if (!sha.ToUpper().Equals(remoteHash))
            {
                if (File.Exists("server.zip"))
                {
                    File.Delete("server.zip");
                }
                WriteFailure("Invalid SHA1 on update package...");
                return false;
            }
            try
            {
                //get the full location of the assembly with DaoTests in it
                var fullPath = Assembly.GetAssembly(typeof(MainWindow)).Location;

                //get the folder that's in
                var theDirectory = Path.GetDirectoryName(fullPath);
                using (var zip = ZipFile.Read("server.zip"))
                {
                    if (zip.ContainsEntry("Bootstrapper.exe"))
                    {
                        //We are updating the updater!
                        try
                        {
                            File.Move("Bootstrapper.exe", "Bootstrapper-old.exe");
                        }
                        catch (FileNotFoundException)
                        {

                          //The file isn't there
                        }
                    }
                    zip.ExtractAll(theDirectory, ExtractExistingFileAction.OverwriteSilently);
                    return true;
                }
            }
            catch (Exception)
            {
            //   MessageBox.Show(mainWindow, "Error, please restart and try again: " + ex.Message);

                return false;
            }
        }

        public static async Task<bool> CheckForUpdates(MainWindow file, string serverFile)
        {
            mainWindow = file;
            try
            {
                mainWindow.UpdateMessage("House cleaning...");
                if (File.Exists("server.zip"))
                {
                    File.Delete("server.zip");
                }
                if (File.Exists("Bootstrapper-old.exe"))
                {
                    File.Delete("Bootstrapper-old.exe");
                }

                var localHash = File.ReadAllText(serverFile);

                using (var httpClient = new HttpClient())
                {
                    mainWindow.UpdateProgressBar(25);
                    mainWindow.UpdateMessage("Checking for updates...");
                    httpClient.Timeout = TimeSpan.FromSeconds(5);
                    var remoteHash = await httpClient.GetStringAsync("https://ulterius.io/updates/server.txt");
                    if (!IsValidSha1(remoteHash))
                    {
                        WriteFailure("Invalid SHA detected. Not updating...");
                        return false;
                    }

                    if (localHash.Equals(remoteHash))
                    {
                        WriteFailure("No updates found...");
                        return false;
                    }
                    //update needed
                    mainWindow.UpdateProgressBar(50);
                    mainWindow.UpdateMessage("Downloading update package...");
                    if (!await DownloadUpdate())
                    {
                        WriteFailure("Update Package Failed to Download...");
                        return false;
                    }
                    mainWindow.UpdateProgressBar(75);
                    mainWindow.UpdateMessage("Unpacking update");

                    var extracted = ExtractUpdate(remoteHash);
                    if (!extracted)
                    {
                        WriteFailure("Error unpacking update...");
                        return false;
                    }
                    mainWindow.UpdateProgressBar(100);
                    mainWindow.UpdateMessage("Ready to launch!");
                    File.WriteAllText("server.bin", remoteHash);
                    File.Delete("server.zip");
                    return true;
                }
            }
            catch (Exception)
            {
               // MessageBox.Show(mainWindow, "Error, please restart and try again: " + ex.Message);
                return false;
            }
        }
    }
}
