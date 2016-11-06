#region

using System;
using System.Collections;
using System.Linq;
using System.Runtime;
using Topshelf;
using UlteriusServer.Forms.Utilities;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Usage;

#endregion

namespace UlteriusServer
{
    internal class Program
    {
        //Evan will have to support me and my cat once this gets released into the public.


        private static void Main(string[] args)

        {


            if (System.Diagnostics.Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)).Count() > 1) return;
          
            ProfileOptimization.SetProfileRoot(AppEnvironment.DataPath);
            ProfileOptimization.StartProfile("Startup.Profile");

            if (args.Length > 0)
            {

                HostFactory.Run(x => //1
                {
                    x.Service<UlteriusAgent>(s => //2
                    {
                        s.ConstructUsing(name => new UlteriusAgent()); //3
                        s.WhenStarted(tc => tc.Start()); //4
                        s.WhenStopped(tc => tc.Stop());
                        s.WhenSessionChanged((se, e, id) => { se.HandleEvent(e, id); }); //5
                    });
                    x.RunAsLocalSystem(); //6
                    x.EnableSessionChanged();
                    x.SetDescription("The server that powers Ulterius"); //7
                    x.SetDisplayName("Ulterius Server"); //8
                    x.SetServiceName("UlteriusServer"); //9
                });
            }
            else
            {
                var ulterius = new Ulterius();
                ulterius.Start();
                var hardware = new HardwareSurvey();
                hardware.Setup();
                if (Tools.RunningPlatform() == Tools.Platform.Windows)
                {
                    UlteriusTray.ShowTray();
                }
                else
                {
                    Console.ReadKey(true);
                }
            }
        }
    }
}