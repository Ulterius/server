#region

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using UlteriusServer.Authentication;
using UlteriusServer.TaskServer.Api.Controllers;
using UlteriusServer.TaskServer.Services.Network;
using UlteriusServer.Utilities;
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
            var settings = new Settings();
            var port = settings.Read("TaskServer", "TaskServerPort", 8387);
            var cancellation = new CancellationTokenSource();
            var endpoint = new IPEndPoint(IPAddress.Parse(NetworkUtilities.GetIPv4Address()), port);
            var server = new WebSocketEventListener(endpoint);
        
            server.OnConnect += HandleConnect;
            server.OnDisconnect += HandleDisconnect;
            server.OnMessage += HandleMessage;
            server.OnError += HandleError;
            server.Start();
            Log("Task TServer started at " + endpoint);
        }

        private static void HandleError(WebSocket websocket, Exception error)
        {
        }

        private static void HandleMessage(WebSocket websocket, string message)
        {
            foreach (var apiController in
                ApiControllers.Select(controller => controller.Value)
                    .Where(apiController => apiController.Client == websocket))
            {
                var packet = new Packets(message);
                apiController.HandlePacket(packet);
            }
        }

        private static void HandleDisconnect(WebSocket clientSocket)
        {
            foreach (var client in AllClients)
            {
                if (client.Value.Client != clientSocket) continue;
                AuthClient temp = null;
                ApiController temp2 = null;
                AllClients.TryRemove(client.Key, out temp);
                ApiControllers.TryRemove(client.Key, out temp2);
                Console.WriteLine("Disconnection from " + clientSocket.RemoteEndpoint);
                Console.WriteLine(ApiControllers.Count);
            }
        }

        private static void HandleConnect(WebSocket clientSocket)
        {
            Console.WriteLine("Connection from " + clientSocket.RemoteEndpoint);
            var client = new AuthClient(clientSocket);
            var apiController = new ApiController(clientSocket)
            {
                //set the auth Client so we can use it later
                authClient = client
            };
            AllClients.AddOrUpdate(client.GetHashCode().ToString(), client, (key, value) => value);
            ApiControllers.AddOrUpdate(apiController.authClient.GetHashCode().ToString(), apiController,
                (key, value) => value);
            SendWelcomeMessage(clientSocket);
        }

        private static void SendWelcomeMessage(WebSocket clientSocket)
        {
            var welcomeMessage = new JavaScriptSerializer().Serialize(new
            {
                endpoint = "connectedToUlterius",
                results = new
                {
                    message = "Ulterius server online!",
                    authRequired = true
                }
            });
            clientSocket.WriteStringAsync(welcomeMessage, CancellationToken.None);
        }

        private static void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}