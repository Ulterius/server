#region

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Ionic.Zip;
using Nito.AsyncEx;

#endregion

namespace Bootstrapper
{
    //temporary bootstrapper for the server
    //lets us update so people won't have to keep redownloading installers
    internal class Program
    {
        private const int SW_HIDE = 0;

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static bool IsValidSha1(string sha1)
        {
            return new Regex("[a-fA-F0-9]{40}").IsMatch(sha1);
        }

        private static async Task<bool> CheckForUpdates()
        {
            try
            {
                if (File.Exists("server.zip"))
                {
                    File.Delete("server.zip");
                }
                if (File.Exists("Bootstrapper-old.exe"))
                {
                    File.Delete("Bootstrapper-old.exe");
                }
                var serverFile = "server.bin";

                if (!File.Exists(serverFile))
                {
                    File.WriteAllText(serverFile, string.Empty);
                }
                var localHash = File.ReadAllText(serverFile);

                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(5);
                    var remoteHash = await httpClient.GetStringAsync("https://ulterius.xyz/updates/server.txt");
                    if (!IsValidSha1(remoteHash)) return false;
                    if (localHash.Equals(remoteHash)) return false;
                    //update needed
                    if (!await DownloadUpdate()) return false;
                    if (!ExtractUpdate()) return false;
                    File.WriteAllText("server.bin", remoteHash);
                    File.Delete("server.zip");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return false;
            }
        }

        private static bool ExtractUpdate()
        {
            try
            {
                //get the full location of the assembly with DaoTests in it
                var fullPath = Assembly.GetAssembly(typeof(Program)).Location;

                //get the folder that's in
                var theDirectory = Path.GetDirectoryName(fullPath);
                using (var zip = ZipFile.Read("server.zip"))
                {
                    if (zip.ContainsEntry("Bootstrapper.exe"))
                    {
                        //We are updating the updater!
                        File.Move("Bootstrapper.exe", "Bootstrapper-old.exe");
                    }
                    zip.ExtractAll(theDirectory, ExtractExistingFileAction.OverwriteSilently);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return false;
            }
        }

        private static async Task<bool> DownloadUpdate()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(5);
                    using (var response = await httpClient.GetAsync("https://ulterius.xyz/updates/server.zip"))
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
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return false;
            }
        }

        private static void Main(string[] args)
        {
            var handle = GetConsoleWindow();
            // Hide
            ShowWindow(handle, SW_HIDE);

            if (Process.GetProcessesByName(
                Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location))
                .Length > 1) Process.GetCurrentProcess().Kill();

            if (Process.GetProcessesByName("Ulterius Server").Length > 0)
            {
                // server is running so kill
                return;
            }
            var filestream = new FileStream("bootstrap.log",
                FileMode.Create);
            var streamwriter = new StreamWriter(filestream) {AutoFlush = true};
            Console.SetOut(streamwriter);
            Console.SetError(streamwriter);

            AsyncContext.Run(() => MainAsync(args));
        }

        private static async void MainAsync(string[] args)
        {
            Console.WriteLine("Update:" + await CheckForUpdates());
            try
            {
                Process.Start("Ulterius Server.exe");
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to start Ulterius");
            }
            Environment.Exit(0);
        }
    }
}