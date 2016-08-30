#region



#endregion

using UlteriusServer.Forms.Utilities;
using UlteriusServer.Utilities.Usage;

namespace UlteriusServer
{
    internal class Program
    {
        //Evan will have to support me and my cat once this gets released into the public.

        public static bool Headers = false;
        private static void Main(string[] args)

        {
            if (args.Length > 0)
            {
                //Don't even care, we have no other arguments
                Headers = true;
            }
            var ulterius = new Ulterius();
            ulterius.Start();
            var hardware = new HardwareSurvey();
            hardware.Setup();
            UlteriusTray.ShowTray();
        }
    }
}