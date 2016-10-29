#region

using System;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Management;
using UlteriusServer.Api.Win32;

#endregion

namespace UlteriusServer.Utilities.Security
{
    public static class AuthUtils
    {

        public static bool Authenticate(string password)
        {
            switch (Tools.RunningPlatform())
            {
                case Tools.Platform.Linux:
                    return false;
                case Tools.Platform.Mac:
                    return AuthMacOs(password);
                case Tools.Platform.Windows:
                    return AuthWindows(password);
                default:
                    return false;
            }
        }
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


        
        private static bool AuthWindows(string password)
        {
            var authenticated = false;
            var envName = Tools.GetUsernameAsService();
           
            var username = Environment.UserDomainName + "\\" + envName;
            //this will fix most domain logins, try first

            using (var context = new PrincipalContext(ContextType.Machine))
            {
                authenticated = context.ValidateCredentials(username, password);
            }
            //lets try a controller 
            if (!authenticated)
            {
                try
                {
                    var domainContext = new DirectoryContext(DirectoryContextType.Domain, Environment.UserDomainName,
                        username, password);
                    var domain = Domain.GetDomain(domainContext);
                    var controller = domain.FindDomainController();
                    //controller logged in if we didn't throw.
                    authenticated = true;
                }
                catch (Exception)
                {
                    authenticated = false;
                }
            }
            return authenticated;
        }
    }
}