#region

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UlteriusServer.Api.Network.PacketHandlers;
using UlteriusServer.WebSocketAPI.Authentication;
using static UlteriusServer.Api.Network.PacketManager;

#endregion

namespace UlteriusServer.Api.Network.Messages
{
    public class Packet
    {
        private readonly Type _packetHandler;
        public List<object> Args;
        public AuthClient AuthClient;
        public string EndPoint;
        public PacketTypes PacketType;
        public string SyncKey;

        public Packet(AuthClient authClient, string endPoint, string syncKey, List<object> args, PacketTypes packetType,
            Type packetHandler)
        {
            AuthClient = authClient;
            EndPoint = endPoint;
            SyncKey = syncKey;
            Args = args;
            PacketType = packetType;
            _packetHandler = packetHandler;
        }

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
                    switch (PacketType)
                    {
                        case PacketTypes.GetWindowsData:
                            handler.HandlePacket(this);
                            return;
                        case PacketTypes.AesHandshake:
                            handler.HandlePacket(this);
                            return;
                        case PacketTypes.Authenticate:
                            handler.HandlePacket(this);
                            return;
                    }
                    PacketType = PacketTypes.NoAuth;
                    handler = Activator.CreateInstance(typeof(ErrorPacketHandler));
                    handler.HandlePacket(this);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                PacketType = PacketTypes.InvalidOrEmptyPacket;
                dynamic handler = Activator.CreateInstance(typeof(ErrorPacketHandler));
                handler.HandlePacket(this);
            }
        }
    }
}