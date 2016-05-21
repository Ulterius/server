#region

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Web.Script.Serialization;
using UlteriusServer.Authentication;
using UlteriusServer.Forms.Utilities;
using UlteriusServer.TaskServer.Api.Controllers;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Security;
using UlteriusServer.WebSocketAPI;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer
{
    internal class TaskManagerServer
    {
        public static ConcurrentDictionary<string, AuthClient> AllClients { get; set; }
        public static ConcurrentDictionary<string, ApiController> ApiControllers { get; set; }

        public static void Start()
        {
            AllClients = new ConcurrentDictionary<string, AuthClient>();
            ApiControllers = new ConcurrentDictionary<string, ApiController>();
    
            var port = (int) Settings.Get("TaskServer").TaskServerPort;  
            var cancellation = new CancellationTokenSource();
            var endpoint = new IPEndPoint(IPAddress.Parse( /*NetworkUtilities.GetIPv4Address()*/ "0.0.0.0"), port);
            var server = new WebSocketEventListener(endpoint, new WebSocketListenerOptions
            {
                SubProtocols = new[] {"text"},
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
            server.OnMessage += HandleMessage;
            server.OnError += HandleError;
            server.Start();
            Log("Task TServer started at " + endpoint);
        }

        private static void HandleError(WebSocket websocket, Exception error)
        {
            Console.WriteLine(error.StackTrace + " " + error.Message);
        }

        private static void HandleMessage(WebSocket websocket, string message)
        {
            ApiController controller;
            ApiControllers.TryGetValue(websocket.GetHashCode().ToString(), out controller);
            if (controller != null)
            {
                var packet = new Packets(controller.AuthClient, message);
                controller.HandlePacket(packet);
            }
        }

        private static void HandleDisconnect(WebSocket clientSocket)
        {

            AuthClient temp = null;
            if (AllClients.TryRemove(clientSocket.GetHashCode().ToString(), out temp))
            {
                ApiController temp2 = null;
                if (ApiControllers.TryRemove(clientSocket.GetHashCode().ToString(), out temp2))
                {
                    Console.WriteLine("Disconnection from " + clientSocket.RemoteEndpoint);
                    var userCount = AllClients.Count;
                    var extra = userCount < 1 ? "s" : string.Empty;
                  UlteriusTray.ShowMessage($"There are now {userCount} user{extra} connected.", "A user disconnected!");
                }
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
            var apiController = new ApiController(clientSocket)
            {
                //set the auth Client so we can use it later
                AuthClient = client
            };
            AllClients.AddOrUpdate(clientSocket.GetHashCode().ToString(), client, (key, value) => value);
            ApiControllers.AddOrUpdate(clientSocket.GetHashCode().ToString(), apiController,
                (key, value) => value);
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