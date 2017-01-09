#region

using System;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AgentInterface.Api.Win32;
using UlteriusServer.Utilities;

#endregion

namespace UlteriusServer.Api.Services.Update
{
    public class UpdateService
    {
    

        public void Start()
        {
            var updaterChecker = new Task(Updater);
            updaterChecker.Start();
        }
        private async void Updater()
        {
            while (true)
            {
                try

    {
                    var updatedNeeded = await CheckForClientUpdates();
                    if (updatedNeeded)
                    {
                        Console.WriteLine("Client was updated");
                    }
                    if (SessionInfo.GetSessionLockState((uint)System.Diagnostics.Process.GetCurrentProcess().SessionId) == SessionInfo.LockState.Unlocked)
                    {
                        Console.WriteLine("Checking for Updates");
                        CheckForServerUpdates();
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine(ex.Message);
                }
                await Task.Delay(new TimeSpan(0, 30, 0));
            }
        }

        private void CheckForServerUpdates()
        {
            var file = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                 "Ulterius Updater.exe /silentall -nofreqcheck");
            ProcessStarter.PROCESS_INFORMATION procInfo;
            ProcessStarter.StartProcessAndBypassUAC(file, out procInfo);
        }


        private async Task<bool> DownloadUpdate()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(5);
                    using (var response = await httpClient.GetAsync("https://ulterius.io/updates/client.zip"))
                    {
                        response.EnsureSuccessStatusCode();
                        using (
                            var fileStream = new FileStream("client.zip", FileMode.Create, FileAccess.Write,
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


        private async Task<bool> CheckForClientUpdates()
        {
            try
            {
                if (File.Exists("client.zip"))
                {
                    File.Delete("client.zip");
                }
                var clientFile = "client.bin";

                if (!File.Exists(clientFile))
                {
                    File.WriteAllText(clientFile, string.Empty);
                }
                var localHash = File.ReadAllText(clientFile);

                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(5);
                    var remoteHash = await httpClient.GetStringAsync("https://ulterius.io/updates/client.txt");
                    if (!IsValidSha1(remoteHash)) return false;
                    if (localHash.Equals(remoteHash)) return false;
                    //update needed
                    if (!await DownloadUpdate()) return false;
                    if (!Tools.InstallClient()) return false;
                    File.WriteAllText("client.bin", remoteHash);
                    File.Delete("client.zip");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return false;
            }
        }

        private bool IsValidSha1(string sha1)
        {
            return new Regex("[a-fA-F0-9]{40}").IsMatch(sha1);
        }
    }
}