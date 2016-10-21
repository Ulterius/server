#region

using System;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Reflection;
using UlteriusServer.Api.Network.Messages;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Security;
using UlteriusServer.WebSocketAPI.Authentication;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.Api.Network.PacketHandlers
{
    public class ServerPacketHandler : PacketHandler
    {
        private AuthClient _authClient;
        private WebSocket _client;
        private MessageBuilder _builder;
        private Packet _packet;


        public void AesHandshake()
        {
            try
            {

                var connectionId = CookieManager.GetConnectionId(_client);
                AuthClient authClient;
                UlteriusApiServer.AllClients.TryGetValue(connectionId, out authClient);
                if (authClient != null)
                {
                    var privateKey = authClient.PrivateKey;
                    var encryptedKey = _packet.Args[0].ToString();
                    var encryptedIv = _packet.Args[1].ToString();
                    authClient.AesKey = Rsa.Decryption(privateKey, encryptedKey);
                    authClient.AesIv = Rsa.Decryption(privateKey, encryptedIv);
                    authClient.AesShook = true;
                    //update the auth client
                    UlteriusApiServer.AllClients[connectionId] = authClient;
                    var endData = new
                    {
                        shook = true
                    };
                    _builder.WriteMessage(endData);
                }
                else
                {
                    throw new Exception("AuthWindows client is null");
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

        public void ListPorts()
        {
            var webServerPort = (ushort)Settings.Get("WebServer").WebServerPort;
            var apiPort = (ushort)Settings.Get("TaskServer").TaskServerPort;
            var webcamPort = (ushort)Settings.Get("Webcams").WebcamPort;
            var terminalPort = (ushort)Settings.Get("Terminal").TerminalPort;
            var screenSharePort = (ushort)Settings.Get("ScreenShareService").ScreenSharePort;
            var portData = new
            {
              webServerPort,
              apiPort,
              webcamPort,
              terminalPort,
              screenSharePort
            };
            _builder.WriteMessage(portData);
        }

        public void Login()
        {

            var authenticated = false;
            var connectionId = CookieManager.GetConnectionId(_client);
            var password = _packet.Args[0].ToString();
            authenticated = !string.IsNullOrEmpty(password) && AuthUtils.Authenticate(password);
            AuthClient authClient;
            UlteriusApiServer.AllClients.TryGetValue(connectionId, out authClient);
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
                UlteriusApiServer.AllClients[connectionId] = authClient;
            }
            var authenticationData = new
            {
                authenticated,
                message = authenticated ? "Login was successful" : "Login was unsuccessful"
            };
            _builder.WriteMessage(authenticationData);
        }

        public void CheckForUpdate()
        {
        }

        public void RestartServer()
        {
            var data = new
            {
                serverRestarting = true
            };
            _builder.WriteMessage(data);
            // Starts a new instance of the program itself
            // Starts a new instance of the program itself
            var fileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "Bootstrapper.exe");
            var startInfo = new ProcessStartInfo(fileName)
            {
                WindowStyle = ProcessWindowStyle.Minimized,
                Arguments = "restart"
            };
            Process.Start(startInfo);
            Environment.Exit(0);
        }

        public override void HandlePacket(Packet packet)
        {
            _client = packet.Client;
            _authClient = packet.AuthClient;
            _packet = packet;
            _builder = new MessageBuilder(_authClient, _client, _packet.EndPoint, _packet.SyncKey);
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
                case PacketManager.PacketTypes.ListPorts:
                   ListPorts();
                    break;
                case PacketManager.PacketTypes.CheckUpdate:
                    CheckForUpdate();
                    break;
            }
        }
    }
}
