#region

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Web.Script.Serialization;
using UlteriusServer.Forms.Utilities;
using UlteriusServer.TaskServer.Network;
using UlteriusServer.TaskServer.Services.Network;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Security;
using UlteriusServer.WebSocketAPI;
using UlteriusServer.WebSocketAPI.Authentication;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer
{
    internal class TaskManagerServer
    {
        public static ConcurrentDictionary<string, AuthClient> AllClients { get; set; }

        public static ScreenShare ScreenShare { get; set; }

        public static void Start()
        {
            AllClients = new ConcurrentDictionary<string, AuthClient>();
            ScreenShare = new ScreenShare();
            var port = (int) Settings.Get("TaskServer").TaskServerPort;
            var cancellation = new CancellationTokenSource();
            var endpoint = new IPEndPoint(NetworkUtilities.GetAddress(), port);
            var server = new WebSocketEventListener(endpoint, new WebSocketListenerOptions
            {
                PingTimeout = TimeSpan.FromSeconds(15),
                NegotiationTimeout = TimeSpan.FromSeconds(15),
                WebSocketSendTimeout = TimeSpan.FromSeconds(15),
                WebSocketReceiveTimeout = TimeSpan.FromSeconds(15),
                ParallelNegotiations = Environment.ProcessorCount*2,
                NegotiationQueueCapacity = 256,
                TcpBacklog = 1000
            });

            server.OnConnect += HandleConnect;
            server.OnDisconnect += HandleDisconnect;
            server.OnPlainTextMessage += HandlePlainTextMessage;
            server.OnEncryptedMessage += HandleEncryptedMessage;
            server.OnError += HandleError;
            server.Start();
            Log("Task Server started at " + endpoint);
        }

        private static void HandleEncryptedMessage(WebSocket websocket, byte[] message)
        {
            var authKey = websocket.GetHashCode().ToString();
            AuthClient authClient;
            if (AllClients.TryGetValue(authKey, out authClient))
            {
                var packetManager = new PacketManager(authClient, message);
                var packet = packetManager.GetPacket();
                packet?.HandlePacket();
            }
        }

        private static void HandleError(WebSocket websocket, Exception error)
        {
            Console.WriteLine(error.StackTrace + " " + error.Message);
        }

        private static void HandlePlainTextMessage(WebSocket websocket, string message)
        {
            var authKey = websocket.GetHashCode().ToString();
            AuthClient authClient;
            if (AllClients.TryGetValue(authKey, out authClient))
            {
                var packetManager = new PacketManager(authClient, message);
                var packet = packetManager.GetPacket();
                packet?.HandlePacket();
            }
        }

        private static void HandleDisconnect(WebSocket clientSocket)
        {
            AuthClient temp = null;
            if (AllClients.TryRemove(clientSocket.GetHashCode().ToString(), out temp))
            {
                Console.WriteLine("Disconnection from " + clientSocket.RemoteEndpoint);
                var userCount = AllClients.Count;
                var extra = userCount < 1 ? "s" : string.Empty;
                UlteriusTray.ShowMessage($"There are now {userCount} user{extra} connected.", "A user disconnected!");
            }
        }

        private static void HandleConnect(WebSocket clientSocket)
        {
            Console.WriteLine("Connection from " + clientSocket.RemoteEndpoint);
            var client = new AuthClient(clientSocket);
            var rsa = new Rsa();
            rsa.GenerateKeyPairs();
            client.PublicKey = rsa.PublicKey;
            client.PrivateKey = rsa.PrivateKey;
            AllClients.AddOrUpdate(clientSocket.GetHashCode().ToString(), client, (key, value) => value);
            SendWelcomeMessage(client, clientSocket);
        }

        private static void SendWelcomeMessage(AuthClient client, WebSocket clientSocket)
        {
            var welcomeMessage = new JavaScriptSerializer().Serialize(new
            {
                endpoint = "connectedToUlterius",
                results = new
                {
                    message = "Ulterius server online!",
                    publicKey = Rsa.SecureStringToString(client.PublicKey)
                }
            });
            clientSocket.WriteStringAsync(welcomeMessage, CancellationToken.None);
            var userCount = AllClients.Count;
            var extra = userCount > 1 ? "s" : string.Empty;
            UlteriusTray.ShowMessage($"There are now {userCount} user{extra} connected.", "A new user connected!");
        }

        private static void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}