#region

using UlteriusServer.Api.Network.Messages;

#endregion

namespace UlteriusServer.Api.Network
{
    public abstract class PacketHandler
    {
        public abstract void HandlePacket(Packet packet);
    }
}