#region

using UlteriusServer.Api.Network.Messages;

#endregion

namespace UlteriusServer.Api.Network
{
    public abstract class PacketHandler
    {
        /// <summary>
        /// Usiing the HandlePacket void we are able to handle each endpoint in their own functions 
        /// </summary>
        public abstract void HandlePacket(Packet packet);
    }
}