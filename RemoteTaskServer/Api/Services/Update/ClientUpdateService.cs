#region

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using UlteriusServer.Utilities;

#endregion

namespace UlteriusServer.Api.Services.Update
{
    //temporary, just for launch 
    public class ClientUpdateService
    {
        private readonly BackgroundWorker backgroundWorker;

        public ClientUpdateService()
        {
            backgroundWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            backgroundWorker.DoWork += BackgroundWorkerOnDoWork;
            backgroundWorker.RunWorkerAsync();
        }

        private void BackgroundWorkerOnDoWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker) sender;
            while (!worker.CancellationPending)
            {
                using (var webpage = new WebClient())
                {
                    webpage.Headers[HttpRequestHeader.UserAgent] =
                        "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2";
                    try
                    {
                        var hash = webpage.DownloadString("https://ulterius.xyz/updates/client.txt").Trim();
                        //incase cloudflare was downloaded
                        if (IsValidSha1(hash))
                        {
                            Console.WriteLine("Client needs to be updated");
                            var localHash = File.ReadAllText("client.bin");
                            if (!localHash.Equals(hash))
                            {
                                webpage.DownloadFile(new Uri("https://ulterius.xyz/updates/client.zip"), "client.zip");
                                if (File.Exists("client.zip"))
                                {
                                    Console.WriteLine("Updating Client");
                                    Tools.InstallClient();
                                    File.WriteAllText("client.bin", hash);
                                    Console.WriteLine("Client Updated");
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        //update failed
                    }
                    Thread.Sleep(new TimeSpan(0, 30, 0));
                }
            }
        }

        private bool IsValidSha1(string sha1)
        {
            return new Regex("[a-fA-F0-9]{40}").IsMatch(sha1);
        }
    }
}