#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using UlteriusServer.Api.Network.Messages;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Security;
using UlteriusServer.Utilities.Settings;
using UlteriusServer.WebSocketAPI.Authentication;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.Api.Network.PacketHandlers
{
    public class ServerPacketHandler : PacketHandler
    {
        private AuthClient _authClient;
        private MessageBuilder _builder;
        private WebSocket _client;
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
            var config = Config.Load();
            var webServerPort = config.WebServer.WebServerPort;
            var apiPort = config.TaskServer.TaskServerPort;
            var webcamPort = config.Webcams.WebcamPort;
            var terminalPort = config.Terminal.TerminalPort;
            var screenSharePort = config.ScreenShareService.ScreenSharePort;
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
            var versionInfo = new
            {
                productVersion = new Version(Application.ProductVersion)
            };
            _builder.WriteMessage(versionInfo);
        }


        public void GetLogs()
        {
            string logData;
            var stringBuilder = new StringBuilder();
            try
            {
                using (
                    var fs = new FileStream(Path.Combine(AppEnvironment.DataPath, "server.log"), FileMode.Open,
                        FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Encoding.Default))
                {
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();
                        if (line != null)
                        {
                            stringBuilder.AppendLine(line);
                        }
                    }
                    logData = stringBuilder.ToString();
                }
            }
            catch (Exception)
            {
                logData = string.Empty;
            }
            var logsPath = Path.Combine(AppEnvironment.DataPath, "Logs");
            var exceptionList = new List<ExceptionModel>();
            if (Directory.Exists(logsPath))
            {
                exceptionList = (from filePath in Directory.GetFiles(logsPath)
                    let fileName = Path.GetFileName(filePath)?.Split(Convert.ToChar("_"))
                    select new ExceptionModel
                    {
                        Type = fileName[0],
                        Date = fileName[1].Replace(".json", ""),
                        Json = File.ReadAllText(filePath)
                    }).ToList();
            }
            var debugInfo = new
            {
                serverLog = logData,
                exceptions = exceptionList
            };
            _builder.WriteMessage(debugInfo);
        }

        public void RestartServer()
        {
            if (UlteriusApiServer.RunningAsService)
            {
                var restartServiceScript = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "restartservice.bat");
                var serviceRestartInfo = new ProcessStartInfo(restartServiceScript)
                {
                    WindowStyle = ProcessWindowStyle.Minimized
                };
                Process.Start(serviceRestartInfo);
            }
            else
            {
                var restartServerScript = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "restartulterius.bat");
                var startInfo = new ProcessStartInfo(restartServerScript)
                {
                    WindowStyle = ProcessWindowStyle.Minimized
                };
                Process.Start(startInfo);
            }
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
                case PacketManager.PacketTypes.CheckVersion:
                    CheckForUpdate();
                    break;
                case PacketManager.PacketTypes.GetLogs:
                    GetLogs();
                    break;
            }
        }

        public class ExceptionModel
        {
            public string Type { get; set; }
            public string Json { get; set; }
            public string Date { get; set; }
        }
    }
}