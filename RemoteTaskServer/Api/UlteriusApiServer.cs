#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UlteriusServer.Api.Network;
using UlteriusServer.Api.Network.Messages;
using UlteriusServer.Api.Services.LocalSystem;
using UlteriusServer.Api.Services.Network;
using UlteriusServer.Api.Services.Update;
using UlteriusServer.Forms.Utilities;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Security;
using UlteriusServer.WebSocketAPI;
using UlteriusServer.WebSocketAPI.Authentication;
using vtortola.WebSockets;
using vtortola.WebSockets.Http;
using ScreenShareService = UlteriusServer.Api.Services.LocalSystem.ScreenShareService;

#endregion

namespace UlteriusServer.Api
{
    internal class UlteriusApiServer
    {
        public static ConcurrentDictionary<Guid, AuthClient> AllClients { get; set; }
        public static bool RunningAsService { get; set; }
        public static ScreenShareService ScreenShareService { get; set; }
        public static FileSearchService FileSearchService { get; set; }
        public static CronJobService CronJobService { get; set; }
        private static int _bufferSize = 1024 * 8;
        private static int _bufferPoolSize;

        /// <summary>
        ///     Start the API Server
        /// </summary>
        public static void Start()
        {
            _bufferPoolSize = 100 * _bufferSize;
            PacketLoader.LoadPackets();
            var config = Config.Load();
            var clientUpdateService = new UpdateService();
            clientUpdateService.Start();
            FileSearchService = new FileSearchService(Path.Combine(AppEnvironment.DataPath, "fileIndex.db"));
            FileSearchService.Start();
            CronJobService = new CronJobService(Path.Combine(AppEnvironment.DataPath, "jobs.json"), Path.Combine(AppEnvironment.DataPath, "scripts"));
            CronJobService.ConfigureJobs();
            var apiPort = config.TaskServer.TaskServerPort;
            AllClients = new ConcurrentDictionary<Guid, AuthClient>();
            ScreenShareService = new ScreenShareService();
            var address = NetworkService.GetAddress();
            var webCamPort = config.Webcams.WebcamPort;
            var screenSharePort = config.ScreenShareService.ScreenSharePort;
            var listenEndPoints = new Uri[] {
                new Uri($"ws://{address}:{apiPort}"),
                new Uri($"ws://{address}:{webCamPort}"),
                new Uri($"ws://{address}:{screenSharePort}")
            };
            var options = new WebSocketListenerOptions
            {
                PingMode = PingMode.LatencyControl,
                NegotiationTimeout = TimeSpan.FromSeconds(30),
                PingTimeout = TimeSpan.FromSeconds(5),
                ParallelNegotiations = 16,
                NegotiationQueueCapacity = 256,
                BufferManager = BufferManager.CreateBufferManager(_bufferPoolSize, _bufferSize),
                Logger = NullLogger.Instance,
                HttpAuthenticationHandler = async (request, response) =>
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(1));
                    if (request.Cookies["ConnectionId"] == null)
                        response.Cookies.Add(new Cookie("ConnectionId", Guid.NewGuid().ToString()));
                    return true;
                }
            };
            options.Transports.ConfigureTcp(tcp =>
            {
                tcp.BacklogSize = 1000; // max pending connections waiting to be accepted
                tcp.ReceiveBufferSize = _bufferSize;
                tcp.SendBufferSize = _bufferSize;
                tcp.LingerState = new LingerOption(true, 0);
                tcp.NoDelay = true;
                tcp.IsAsync = true;
                
                tcp.ReceiveTimeout = TimeSpan.FromSeconds(1);
                tcp.SendTimeout = TimeSpan.FromSeconds(3);
            });

            var server = new WebSocketEventListener(listenEndPoints, options);
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
        private static async void HandleConnect(WebSocket clientSocket)
        {
            var connectionId = CookieManager.GetConnectionId(clientSocket);
            AuthClient authClient;
            AllClients.TryGetValue(connectionId, out authClient);
            var host = new Uri($"ws://{clientSocket.HttpRequest.Headers[RequestHeader.Host]}", UriKind.Absolute);

            if (authClient != null)
            {

                if (RunningAsService)
                {
                    MessageQueueManager agentManager;
                    if (!authClient.MessageQueueManagers.TryGetValue(22005, out agentManager))
                    {
                        if (authClient.MessageQueueManagers.TryAdd(22005,
                            new MessageQueueManager()))
                        {
                            Console.WriteLine("Service Manager Started");
                        }
                    }
                }
                MessageQueueManager manager;
                //check if a manager for that port exist, if not, create one
                
                if (!authClient.MessageQueueManagers.TryGetValue(host.Port, out manager))
                {
                    if (authClient.MessageQueueManagers.TryAdd(host.Port,
                        new MessageQueueManager()))
                    {
                        Console.WriteLine($"Manager started for {host.Port}");
                    }
                }
                return;
            }
            Console.WriteLine("Connection from " + clientSocket.RemoteEndpoint);
            var rsa = new Rsa();
            rsa.GenerateKeyPairs();
            var client = new AuthClient
            {
                PublicKey = rsa.PublicKey,
                PrivateKey = rsa.PrivateKey,
                MessageQueueManagers = new ConcurrentDictionary<int, MessageQueueManager>()
            };
          
            client.MessageQueueManagers.TryAdd(host.Port, new MessageQueueManager());
            AllClients.AddOrUpdate(connectionId, client, (key, value) => value);
            await SendWelcomeMessage(client, clientSocket);
        }

        /// <summary>
        ///     Sends a new user their unique RSA keypair
        /// </summary>
        /// <param name="client"></param>
        /// <param name="clientSocket"></param>
        private static async Task SendWelcomeMessage(AuthClient client, WebSocket clientSocket)
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
            if (clientSocket != null)
            {
                try
                {
                    await clientSocket.WriteStringAsync(welcomeMessage, CancellationToken.None);
                    var userCount = AllClients.Count;
                    var extra = userCount > 1 ? "s" : string.Empty;
                    UlteriusTray.ShowMessage($"There are now {userCount} user{extra} connected.", "A new user connected!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to write welcome message " + ex.Message );
                }
            }
        }

        private static void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}