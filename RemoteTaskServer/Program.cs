#region



#endregion

using System;
using System.Runtime;
using UlteriusServer.Forms.Utilities;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Usage;

namespace UlteriusServer
{
    internal class Program
    {
        //Evan will have to support me and my cat once this gets released into the public.

       
        private static void Main(string[] args)

        {

            ProfileOptimization.SetProfileRoot(AppEnvironment.DataPath);
            ProfileOptimization.StartProfile("Startup.Profile");

            var ulterius = new Ulterius();
            ulterius.Start();
            var hardware = new HardwareSurvey();
            hardware.Setup();
            //TODO Gdk tray icons
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