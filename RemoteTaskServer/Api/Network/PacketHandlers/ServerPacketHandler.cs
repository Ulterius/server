#region

using System;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Reflection;
using System.Xml;
using UlteriusServer.Api.Network.Messages;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Security;
using UlteriusServer.WebSocketAPI.Authentication;

#endregion

namespace UlteriusServer.Api.Network.PacketHandlers
{
    public class ServerPacketHandler : PacketHandler
    {
        private AuthClient _authClient;
        private MessageBuilder _builder;
        private Packet _packet;


        public void AesHandshake()
        {
            try
            {
                var authKey = _authClient.Client.GetHashCode().ToString();
                AuthClient authClient;
                UlteriusApiServer.AllClients.TryGetValue(authKey, out authClient);
                if (authClient != null)
                {
                    var privateKey = authClient.PrivateKey;
                    var encryptedKey = _packet.Args[0].ToString();
                    var encryptedIv = _packet.Args[1].ToString();
                    authClient.AesKey = Rsa.Decryption(privateKey, encryptedKey);
                    authClient.AesIv = Rsa.Decryption(privateKey, encryptedIv);
                    authClient.AesShook = true;
                    //update the auth client
                    UlteriusApiServer.AllClients[authKey] = authClient;
                    var endData = new
                    {
                        shook = true
                    };
                    _builder.WriteMessage(endData);
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
                _builder.WriteMessage(endData);
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
            //first trying using local machine
            using (var context = new PrincipalContext(ContextType.Machine))
            {
                authenticated = context.ValidateCredentials(GetUsername(), password);
            }

            var authKey = _authClient.Client.GetHashCode().ToString();
            AuthClient authClient;
            UlteriusApiServer.AllClients.TryGetValue(authKey, out authClient);
            if (authClient != null)
            {
                if (authClient.Authenticated)
                {
                    _builder.WriteMessage(new
                    {
                        authenticated,
                        message = "Already logged in."
                    });
                    return;
                }
                authClient.Authenticated = authenticated;
                UlteriusApiServer.AllClients[authKey] = authClient;
            }
            var authenticationData = new
            {
                authenticated,
                message = authenticated ? "Login was successfull" : "Login was unsuccessful"
            };
            _builder.WriteMessage(authenticationData);
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
                        _builder.WriteMessage(data);
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
                        _builder.WriteMessage(data);
                    }
                    else
                    {
                        var data = new
                        {
                            update = false,
                            message = "You have the latest version."
                        };
                        _builder.WriteMessage(data);
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
                    _builder.WriteMessage(data);
                }
            }
            var endData = new
            {
                update = false,
                error = "No connection",
                message = "Unable to connect to the internet to check for update."
            };
            _builder.WriteMessage(endData);
        }

        public void RestartServer()
        {
            var data = new
            {
                serverRestarting = true
            };
            _builder.WriteMessage(data);
            // Starts a new instance of the program itself
            var fileName = Assembly.GetExecutingAssembly().Location;
            Process.Start(fileName);

            // Closes the current process
            Environment.Exit(0);
        }

        public override void HandlePacket(Packet packet)
        {
            _authClient = packet.AuthClient;
            _packet = packet;
            _builder = new MessageBuilder(_authClient, _packet.EndPoint, _packet.SyncKey);
            switch (_packet.PacketType)
            {
                case PacketManager.PacketTypes.Authenticate:
                    Login();
                    break;
                case PacketManager.PacketTypes.AesHandshake:
                    AesHandshake();
                    break;
                case PacketManager.PacketTypes.RestartServer:
                    RestartServer();
                    break;
                case PacketManager.PacketTypes.CheckUpdate:
                    CheckForUpdate();
                    break;
            }
        }
    }
}