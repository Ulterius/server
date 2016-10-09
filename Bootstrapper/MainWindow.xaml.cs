#region

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Windows;
using static System.IO.Path;

#endregion

namespace Bootstrapper
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string serverFile;

        public MainWindow()
        {
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 0)
            {
                var argument = args[0];
                if (argument.Equals("restart"))
                {
                    //if its still up
                    var list = Process.GetProcessesByName("Ulterius Server");
                    if (list.Length > 0)
                    {
                        list[0].Kill();
                    }
                }
            }
            var workingDir = GetDirectoryName(Assembly.GetEntryAssembly().Location);
         
            Directory.SetCurrentDirectory(workingDir);
            if (Process.GetProcessesByName(
                GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location))
                .Length > 1)
            {
                Console.WriteLine("Killing");
                Process.GetCurrentProcess().Kill();
            }
            if (Process.GetProcessesByName("Ulterius Server").Length > 0)
            {
                Process.GetCurrentProcess().Kill();
            }
            InitializeComponent();

            if (IsElevated)
            {
                UpdateMessage("Bootstrapper starting...");
                MainAsync();
            }
            else
            {
                //restart as admin, needed to ensure windows lets as start at startup
                var info = new ProcessStartInfo(Assembly.GetEntryAssembly().Location)
                {
                    Verb = "runas",
                    WorkingDirectory = workingDir
                };
                var process = new Process
                {
                    EnableRaisingEvents = true, // enable WaitForExit()
                    StartInfo = info
                };
                try
                {
                    process.Start();
                    Environment.Exit(0);
                }
                catch (Exception)
                {
                    UpdateMessage("Failed to start Ulterius -- Admin required");
                }
            }
           
        }
        private async void MainAsync()
        {
            serverFile = "server.bin";
            if (!File.Exists(serverFile))
            {
                File.WriteAllText(serverFile, string.Empty);
            }
            await Tools.CheckForUpdates(this, serverFile);
            try
            {
                UpdateMessage("Starting Ulterius...");
                Process.Start("Ulterius Server.exe");
            }
            catch (Exception)
            {
                UpdateMessage("Failed to start Ulterius");
            }
            Environment.Exit(0);

        }

        private  bool IsElevated
        {
            get
            {
                var securityIdentifier = WindowsIdentity.GetCurrent().Owner;
                return securityIdentifier != null && securityIdentifier
                    .IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid);
            }
        }
        public void UpdateMessage(string message)
        {
            messageBlock.Dispatcher.BeginInvoke((Action) (() => messageBlock.Text = message));
        }

        public void UpdateProgressBar(double value)
        {
            calculationProgressBar.Dispatcher.BeginInvoke((Action) (() => calculationProgressBar.Value = value));
        }
    }
}