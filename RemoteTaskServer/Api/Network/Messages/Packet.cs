#region

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UlteriusServer.Api.Network.PacketHandlers;
using UlteriusServer.WebSocketAPI.Authentication;
using vtortola.WebSockets;
using static UlteriusServer.Api.Network.PacketManager;

#endregion

namespace UlteriusServer.Api.Network.Messages
{
    public class Packet
    {
        private readonly Type _packetHandler;
        public List<object> Args;
        public AuthClient AuthClient;
        public WebSocket Client;
        public string EndPointName;
        public EndPoints EndPoint;
        public string SyncKey;

        /// <summary>
        ///     Create a packet
        /// </summary>
        /// <param name="authClient"></param>
        /// <param name="client"></param>
        /// <param name="endPointName"></param>
        /// <param name="syncKey"></param>
        /// <param name="args"></param>
        /// <param name="endPoint"></param>
        /// <param name="packetHandler"></param>
        public Packet(AuthClient authClient, WebSocket client, string endPointName, string syncKey, List<object> args,
            EndPoints endPoint,
            Type packetHandler)
        {
            AuthClient = authClient;
            Client = client;
            EndPointName = endPointName;
            SyncKey = syncKey;
            Args = args;
            EndPoint = endPoint;
            _packetHandler = packetHandler;
        }
      
        /// <summary>
        ///     Executes a packet based on its handler
        /// </summary>
        public void HandlePacket()
        {
            try
            {
                //If no auth client is present we don't need to proceed.
                if (AuthClient == null) return;
                //Create an instance of our handler
                dynamic handler = Activator.CreateInstance(_packetHandler);
                //Authentication is needed to run most packets, however some work without being logged in.
                if (AuthClient.Authenticated || !NeedsAuth(EndPoint))
                {
                    Task.Run(() => { handler.HandlePacket(this); });
                }
                else
                {
                    EndPointName = "noauth";
                    EndPoint = EndPoints.NoAuth;
                    handler = Activator.CreateInstance(typeof(ErrorPacketHandler));
                    handler.HandlePacket(this);
                }
            }
            catch (Exception)
            {
                EndPointName = "invalidpacket";
                EndPoint = EndPoints.InvalidOrEmptyPacket;
                dynamic handler = Activator.CreateInstance(typeof(ErrorPacketHandler));
                handler.HandlePacket(this);
            }
        }

        private bool NeedsAuth(EndPoints endPoint)
        {
            switch (endPoint)
            {
                case EndPoints.GetWindowsData:
                case EndPoints.ListPorts:
                case EndPoints.AesHandshake:
                case EndPoints.Authenticate:
                    return false;
                default:
                    return true;
            }
        }
    }
}