#region

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using UlteriusServer.Api.Network;
using UlteriusServer.Api.Network.Messages;
using UlteriusServer.Api.Services.Network;
using UlteriusServer.Api.Services.System;
using UlteriusServer.Api.Services.Update;
using UlteriusServer.Forms.Utilities;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Security;
using UlteriusServer.WebSocketAPI;
using UlteriusServer.WebSocketAPI.Authentication;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.Api
{
    internal class UlteriusApiServer
    {
        public static MessageQueueManager MessageQueueManager = new MessageQueueManager();
        public static ConcurrentDictionary<string, AuthClient> AllClients { get; set; }
        public static ScreenShareService ScreenShareService { get; set; }
        public static FileSearchService FileSearchService { get; set; }

        /// <summary>
        /// Start the API Server
        /// </summary>
        public static void Start()
        {
            AllClients = new ConcurrentDictionary<string, AuthClient>();
            var clientUpdateService = new ClientUpdateService();
            ScreenShareService = new ScreenShareService();
            FileSearchService = new FileSearchService(Path.Combine(AppEnvironment.DataPath, "fileindex.bin"));
            FileSearchService.Start();
            var port = (int) Settings.Get("TaskServer").TaskServerPort;
            var cancellation = new CancellationTokenSource();
            var address = NetworkService.GetAddress();
            var endpoint = new IPEndPoint(address, port);
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
            Log("Api Server started at " + address);
        }


        /// <summary>
        /// Handles encrypted binary messages 
        /// </summary>
        /// <param name="websocket"></param>
        /// <param name="message"></param>
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

        /// <summary>
        /// Handles plaintext JSON packets.
        /// </summary>
        /// <param name="websocket"></param>
        /// <param name="message"></param>
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


        /// <summary>
        /// Remove a client when it disconnects
        /// </summary>
        /// <param name="clientSocket"></param>
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

        /// <summary>
        /// When a client connects, assign them a unique RSA keypair for handshake.
        /// </summary>
        /// <param name="clientSocket"></param>
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

        /// <summary>
        /// Sends a new user their unique RSA keypair
        /// </summary>
        /// <param name="client"></param>
        /// <param name="clientSocket"></param>
        private static void SendWelcomeMessage(AuthClient client, WebSocket clientSocket)
        {
            var welcomeMessage = JsonConvert.SerializeObject(new
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