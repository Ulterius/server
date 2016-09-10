#region



#endregion

using UlteriusServer.Forms.Utilities;
using UlteriusServer.Utilities.Usage;

namespace UlteriusServer
{
    internal class Program
    {
        //Evan will have to support me and my cat once this gets released into the public.

       
        private static void Main(string[] args)

        {
            var ulterius = new Ulterius();
            ulterius.Start();
            var hardware = new HardwareSurvey();
            hardware.Setup();
            UlteriusTray.ShowTray();
           
        }
    }
}