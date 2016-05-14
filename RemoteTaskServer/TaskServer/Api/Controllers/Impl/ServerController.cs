#region

using System;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Reflection;
using System.Xml;
using UlteriusServer.Authentication;
using UlteriusServer.TaskServer.Api.Serialization;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Security;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer.Api.Controllers.Impl
{
    public class ServerController : ApiController
    {
        private readonly WebSocket _client;
        private readonly Packets _packet;
        private readonly ApiSerializator _serializator = new ApiSerializator();

        public ServerController(WebSocket client, Packets packet)
        {
            _client = client;
            _packet = packet;
        }

        public void AesHandshake()
        {
            try
            {
                var authKey = _client.GetHashCode().ToString();
                AuthClient authClient;
                TaskManagerServer.AllClients.TryGetValue(authKey, out authClient);
                if (authClient != null)
                {
                    var privateKey = authClient.PrivateKey;
                    var encryptedKey = _packet.Args[0].ToString();
                    var encryptedIv = _packet.Args[1].ToString();
                    authClient.AesKey = Rsa.Decryption(privateKey, encryptedKey);
                    authClient.AesIv = Rsa.Decryption(privateKey, encryptedIv);
                    authClient.AesShook = true;
                    //update the auth client
                    TaskManagerServer.AllClients[authKey] = authClient;
                    var endData = new
                    {
                        shook = true
                    };
                    _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, endData);
                }
                else
                {
                    throw new Exception("Auth client is null");
                }
            }
            catch (Exception e)
            {
                var endData = new
                {
                    shook = false,
                    message = e.Message
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, endData);
            }
        }

        private string GetUsername()
        {
            return Environment.UserName;
        }

        public void Login()
        {
            var password = _packet.Args[0].ToString();
            bool authenticated;
            using (var context = new PrincipalContext(ContextType.Machine))
            {
                authenticated = context.ValidateCredentials(GetUsername(), password);
            }
            var authKey = _client.GetHashCode().ToString();
            AuthClient authClient;
            TaskManagerServer.AllClients.TryGetValue(authKey, out authClient);
            if (authClient != null)
            {
                authClient.Authenticated = authenticated;
                TaskManagerServer.AllClients[authKey] = authClient;
            }
            var authenticationData = new
            {
                authenticated,
                message = authenticated ? "Login was successfull" : "Login was unsuccessful"
            };
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, authenticationData);
        }

        public void CheckForUpdate()
        {
            var isError = false;
            var errorData = "";
            if (Tools.HasInternetConnection)
            {
                try
                {
                    var releasePageUrl = "";
                    var changeNotes = "";
                    Version newVersion = null;
                    const string versionConfig = "https://raw.github.com/StrikeOrg/ulterius-server/master/version.xml";
                    var reader = new XmlTextReader(versionConfig);
                    reader.MoveToContent();
                    var elementName = "";
                    try
                    {
                        if ((reader.NodeType == XmlNodeType.Element) && (reader.Name == "ulteriusserver"))
                        {
                            while (reader.Read())
                            {
                                switch (reader.NodeType)
                                {
                                    case XmlNodeType.Element:
                                        elementName = reader.Name;
                                        break;
                                    default:
                                        if ((reader.NodeType == XmlNodeType.Text) && reader.HasValue)
                                        {
                                            switch (elementName)
                                            {
                                                case "version":
                                                    newVersion = new Version(reader.Value);
                                                    break;
                                                case "changeNotes":
                                                    changeNotes = reader.Value;
                                                    break;
                                                case "url":
                                                    releasePageUrl = reader.Value;
                                                    break;
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        isError = true;
                        errorData = e.Message;
                    }
                    finally
                    {
                        reader.Close();
                    }

                    var applicationVersion = Assembly.GetExecutingAssembly().GetName().Version;
                    if (isError)
                    {
                        var data = new
                        {
                            update = false,
                            error = errorData,
                            message = "Error retrieving update information: " + errorData
                        };
                        _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
                    }
                    else if (applicationVersion.CompareTo(newVersion) < 0)
                    {
                        var data = new
                        {
                            update = true,
                            url = releasePageUrl,
                            newVersion = newVersion.ToString(),
                            changeNotes,
                            message = "New version available: " + newVersion
                        };
                        _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
                    }
                    else
                    {
                        var data = new
                        {
                            update = false,
                            message = "You have the latest version."
                        };
                        _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
                    }
                }
                catch (Exception e)
                {
                    var data = new
                    {
                        update = false,
                        error = e.Message,
                        message = "General bad thing has happened: " + e.Message
                    };
                    _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
                }
            }
            var endData = new
            {
                update = false,
                error = "No connection",
                message = "Unable to connect to the internet to check for update."
            };
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, endData);
        }

        public void RestartServer()
        {
            var data = new
            {
                serverRestarting = true
            };
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
            var fileName = Assembly.GetExecutingAssembly().Location;
            Process.Start(fileName);
            Environment.Exit(0);
        }
    }
}