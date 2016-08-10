#region

using System;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;

#endregion

namespace UlteriusServer.Utilities.Security
{
    public static class WindowsAuth
    {
        public static bool Auth(string password)
        {
            var authenticated = false;
            //this will fix most domain logins, try first
            var username = Environment.UserDomainName + "\\" + Environment.UserName;
            using (var context = new PrincipalContext(ContextType.Machine))
            {
                try
                {
                    authenticated = context.ValidateCredentials(username, password);
                }
                catch (Exception)
                {
                    authenticated = false;
                }
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