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
                if (AuthClient == null)
                {
                    return;
                }
                //Build a handler Workshop
                //THIS COULD BE BETTER
                dynamic handler = Activator.CreateInstance(_packetHandler);
                //no auth needed for these
                if (AuthClient.Authenticated)
                {
                    Task.Run(() => { handler.HandlePacket(this); });
                }
                else
                {
                    switch (EndPoint)
                    {
                        case EndPoints.GetWindowsData:
                        case EndPoints.ListPorts:
                        case EndPoints.AesHandshake:
                        case EndPoints.Authenticate:
                            handler.HandlePacket(this);
                            return;
                    }
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
    }
}