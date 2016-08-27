#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        public static ConcurrentDictionary<Guid, AuthClient> AllClients { get; set; }

        public static ScreenShareService ScreenShareService { get; set; }
        public static FileSearchService FileSearchService { get; set; }

        /// <summary>
        ///     Start the API Server
        /// </summary>
        public static void Start()
        {
            var apiPort = (int) Settings.Get("TaskServer").TaskServerPort;
            AllClients = new ConcurrentDictionary<Guid, AuthClient>();
            ScreenShareService = new ScreenShareService();
            FileSearchService = new FileSearchService(Path.Combine(AppEnvironment.DataPath, "fileindex.bin"));
            FileSearchService.Start();
            var address = NetworkService.GetAddress();
            var endPoints = new List<IPEndPoint> {new IPEndPoint(address, apiPort), new IPEndPoint(address, 29999)};


            var server = new WebSocketEventListener(endPoints, new WebSocketListenerOptions
            {
                PingTimeout = TimeSpan.FromSeconds(15),
                NegotiationTimeout = TimeSpan.FromSeconds(15),
                WebSocketSendTimeout = TimeSpan.FromSeconds(15),
                WebSocketReceiveTimeout = TimeSpan.FromSeconds(15),
                ParallelNegotiations = Environment.ProcessorCount*2,
                NegotiationQueueCapacity = 256,
                TcpBacklog = 1000,
                OnHttpNegotiation = (request, response) =>
                {
                    if (request.Cookies["ConnectionId"] == null)
                        response.Cookies.Add(new Cookie("ConnectionId", Guid.NewGuid().ToString()));
                }
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
        ///     Handles encrypted binary messages
        /// </summary>
        /// <param name="clientSocket"></param>
        /// <param name="message"></param>
        private static void HandleEncryptedMessage(WebSocket clientSocket, byte[] message)
        {
            var connectionId = CookieManager.GetConnectionId(clientSocket);
            AuthClient authClient;
            if (AllClients.TryGetValue(connectionId, out authClient))
            {
                var packetManager = new PacketManager(authClient, clientSocket, message);
                var packet = packetManager.GetPacket();
                packet?.HandlePacket();
            }
        }

        private static void HandleError(WebSocket websocket, Exception error)
        {
            Console.WriteLine(error.StackTrace + " " + error.Message);
        }

        /// <summary>
        ///     Handles plaintext JSON packets.
        /// </summary>
        /// <param name="clientSocket"></param>
        /// <param name="message"></param>
        private static void HandlePlainTextMessage(WebSocket clientSocket, string message)
        {
            var connectionId = CookieManager.GetConnectionId(clientSocket);
            AuthClient authClient;
            if (AllClients.TryGetValue(connectionId, out authClient))
            {
                var packetManager = new PacketManager(authClient, clientSocket, message);
                var packet = packetManager.GetPacket();
                packet?.HandlePacket();
            }
        }


        /// <summary>
        ///     Remove a client when it disconnects
        /// </summary>
        /// <param name="clientSocket"></param>
        private static void HandleDisconnect(WebSocket clientSocket)
        {
            var connectionId = CookieManager.GetConnectionId(clientSocket);
            AuthClient temp = null;
            if (AllClients.TryRemove(connectionId, out temp))
            {
                Console.WriteLine("Disconnection from " + clientSocket.RemoteEndpoint);
                var userCount = AllClients.Count;
                var extra = userCount < 1 ? "s" : string.Empty;
                UlteriusTray.ShowMessage($"There are now {userCount} user{extra} connected.", "A user disconnected!");
            }
        }

        /// <summary>
        ///     When a client connects, assign them a unique RSA keypair for handshake.
        /// </summary>
        /// <param name="clientSocket"></param>
        private static void HandleConnect(WebSocket clientSocket)
        {
            var connectionId = CookieManager.GetConnectionId(clientSocket);
            AuthClient authClient;
            AllClients.TryGetValue(connectionId, out authClient);
            if (authClient != null) return; 
            Console.WriteLine("Connection from " + clientSocket.RemoteEndpoint);
            var client = new AuthClient();
            var rsa = new Rsa();
            rsa.GenerateKeyPairs();
            client.PublicKey = rsa.PublicKey;
            client.PrivateKey = rsa.PrivateKey;
            AllClients.AddOrUpdate(connectionId, client, (key, value) => value);
            SendWelcomeMessage(client, clientSocket);
        }

        /// <summary>
        ///     Sends a new user their unique RSA keypair
        /// </summary>
        /// <param name="client"></param>
        /// <param name="clientSocket"></param>
        private static async void SendWelcomeMessage(AuthClient client, WebSocket clientSocket)
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
            await clientSocket.WriteStringAsync(welcomeMessage, CancellationToken.None);
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