#region

using System;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Web.Script.Serialization;
using UlteriusServer.TaskServer;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.Authentication
{
    internal class UlteriusLoginDecoder
    {
        private static readonly byte INVALID_PASSWORD = 3;
        private static readonly byte AUTHENTICATED = 2;

        private string GetUsername()
        {
            return Environment.UserName;
        }
        public bool Login(string password)
        {
            var code = 3;
            if (string.IsNullOrEmpty(password))
            {
                code = INVALID_PASSWORD;
            }
            using (var context = new PrincipalContext(ContextType.Machine))
            {
                code = context.ValidateCredentials(GetUsername(), password) ? 2 : 3;
            }
            var authenticated = code == AUTHENTICATED;
            return authenticated;
        }

        public string Login(string password, WebSocket clientSocket)
        {
            var code = 3;
            if (string.IsNullOrEmpty(password))
            {
                code = INVALID_PASSWORD;
            }
            using (var context = new PrincipalContext(ContextType.Machine))
            {
                code = context.ValidateCredentials(GetUsername(), password) ? 2 : 3;
            }
            var authenticated = code == AUTHENTICATED;
            foreach (var client in TaskManagerServer.AllClients.Where(client => client.Value.Client == clientSocket))
            {
                if (code == INVALID_PASSWORD)
                {
                    client.Value.Authenticated = false;
                }
                else if (code == AUTHENTICATED)
                {
                    client.Value.Authenticated = true;
                }
            }
            var authenticationData = new JavaScriptSerializer().Serialize(new
            {
                endpoint = "authentication",
                results = new
                {
                    authenticated,
                    message = authenticated ? "Login was successfull" : "Login was unsuccessful"
                }
            });
            return authenticationData;
        }
    }
}