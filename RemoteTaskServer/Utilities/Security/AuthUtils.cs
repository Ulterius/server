#region

using System;
using System.Diagnostics;
using System.Security.Principal;
using AgentInterface.Api.Win32;
using UlteriusServer.Api.Network.Models;

#endregion

namespace UlteriusServer.Utilities.Security
{
    public static class AuthUtils
    {

      
        private static bool AuthMacOs(string password)
        {
            try
            {
                var username = Environment.UserName;
                //As far as I know there is no MacOS Api for validating a user, this will have to do for now.
                //Someone is gonna stab me
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        WorkingDirectory = "/home",
                        FileName = "dscl",
                        Arguments = $"/Local/Default -authonly {username} {password}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    }
                };
                process.Start();
                var count = 0;
                while (!process.StandardOutput.EndOfStream)
                {
                    var line = process.StandardOutput.ReadLine();
                    count++;
                }
                //if the count is 0 we input a valid command. dscl does not return anything for a valid password 
                return count == 0;
            }
            catch (Exception)
            {

                return false;
            }
        }

        public static LoginInformation AuthWindows(string username, string password)
        {
            var info = new LoginInformation();
            try
            {
                
                var domainName = Environment.UserDomainName;
                if (username.Contains("\\"))
                {
                    var splitName = username.Split('\\');
                    domainName = splitName[0];
                    username = splitName[1];
                }
                using (var wim = new WindowsIdentityImpersonator(domainName, username, password))
                {
                    wim.BeginImpersonate();
                    {
                        info.IsAdmin = WinApi.IsAdministratorByToken(WindowsIdentity.GetCurrent());
                        info.LoggedIn = true;
                        info.Message = $"Logged in successfully as {username}";
                    }
                    wim.EndImpersonate();
                }
            }
            catch (Exception ex)
            {
                info.IsAdmin = false;
                info.LoggedIn = false;
                info.Message = ex.Message;
               
            }
            return info;
        }
    }
}