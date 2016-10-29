#region

using System;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

#endregion

namespace UlteriusServer.Api.Network.Models
{
    public class JobModel
    {
        public string Name { get; set; }

        public string Type { get; set; }
       

        public string Schedule { get; set; }

        [JsonIgnore]
        public bool Running { get; set; }

        [JsonIgnore]
        private CancellationTokenSource Source { get; set; }

        public void StopExecute()
        {
            Running = false;
            Source.Cancel();
        }

        private async Task RunScript()
        {
            if (!File.Exists(Name))
            {
                return;
            }
            if (Type.Equals("Powershell"))
            {
                using (var powerShellInstance = PowerShell.Create())
                {
                    var scriptContents = File.ReadAllText(Name);
                    powerShellInstance.AddScript(scriptContents);
                    // prepare a new collection to store output stream objects
                    var outputCollection = new PSDataCollection<PSObject>();
                    outputCollection.DataAdded += outputCollection_DataAdded;
                    powerShellInstance.Streams.Error.DataAdded += Error_DataAdded;
                    // begin invoke execution on the pipeline
                    // use this overload to specify an output stream buffer
                    var result = powerShellInstance.BeginInvoke<PSObject, PSObject>(null, outputCollection);
                    while (result.IsCompleted == false)
                    {
                        Thread.Sleep(1000);
                    }
                    await
                        Console.Out.WriteLineAsync("Execution has stopped. The pipeline state: " +
                                                   powerShellInstance.InvocationStateInfo.State);
                }
            }
            else if (Type.Equals("cmd"))
            {
                try
                {
                    await RunProcessAsync(Name);
                }
                catch (Exception ex)
                {

                    Console.WriteLine(ex.Message);
                }
            }
        }

        public async Task Execute()
        {
            Running = true;
            Source = new CancellationTokenSource();
            await Task.Run(RunScript, Source.Token);
            Running = false;
        }

        private Task RunProcessAsync(string fileName)
        {
            // there is no non-generic TaskCompletionSource
            var tcs = new TaskCompletionSource<bool>();

            var process = new Process
            {
                StartInfo =
                {
                    FileName = fileName,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(Name),
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            process.Exited += (sender, args) =>
            {
                tcs.SetResult(true);
                process.Dispose();
            };

            process.Start();

            return tcs.Task;
        }

        private void Error_DataAdded(object sender, DataAddedEventArgs e)
        {
        }

        private void outputCollection_DataAdded(object sender, DataAddedEventArgs e)
        {
        }
    }
}