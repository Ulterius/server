#region

using System;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json.Linq;

#endregion

namespace RemoteTaskServer.Api
{
    internal class WindowsApi
    {
        [DllImport("shell32.dll", EntryPoint = "#261",
            CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void GetUserTilePath(
            string username,
            uint whatever, // 0x80000000
            StringBuilder picpath, int maxLength);

        private static string GetUserTilePath(string username)
        {
            // username: use null for current user
            var sb = new StringBuilder(1000);
            GetUserTilePath(username, 0x80000000, sb, sb.Capacity);
            return sb.ToString();
        }

        private static string GetUsername()
        {
            return Environment.UserName;
        }

        public static string GetWindowsInformation()
        {
            return JObject.FromObject(new
            {
                avatar = GetUserAvatar(),
                username = GetUsername()
            }).ToString();
        }

        public static string VerifyPassword(string password)
        {
            var valid = false;
            using (var context = new PrincipalContext(ContextType.Machine))
            {
                valid = context.ValidateCredentials(GetUsername(), password);
            }
            return JObject.FromObject(new
            {
                validLogin = valid,
                message = valid ? "Login was successfull" : "Login was unsuccessful"
            }).ToString();
        }

        private void ShutDownSystem()
        {
            Process.Start("shutdown", "/s /t 0");    // starts the shutdown application 
                                                     // the argument /s is to shut down the computer
                                                     // the argument /t 0 is to tell the process that 
                                                     // the specified operation needs to be completed 
                                                     // after 0 seconds
        }


        private static string GetUserAvatar()
        {
            var avatar = GetUserTile(null);
            var ms = new MemoryStream();
            avatar.Save(ms, ImageFormat.Png);
            var arr = new byte[ms.Length];

            ms.Position = 0;
            ms.Read(arr, 0, (int) ms.Length);
            ms.Close();

            var strBase64 = Convert.ToBase64String(arr);

            return strBase64;
        }

        private static Image GetUserTile(string username)
        {
            return Image.FromFile(GetUserTilePath(username));
        }
    }
}