#region

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UlteriusServer.Api;
using UlteriusServer.Api.Services.System;
using UlteriusServer.Forms.Utilities;
using UlteriusServer.Properties;
using UlteriusServer.TerminalServer;
using UlteriusServer.Utilities;
using UlteriusServer.WebCams;

#endregion

namespace UlteriusServer
{
    internal class Program
    {

        //Evan will have to support me and my cat once this gets released into the public.
     
        private static void Main(string[] args)

        {
          
            var ulterius = new Ulterius();
            ulterius.Start();
          

        }
    }
}