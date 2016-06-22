#region

using UlteriusServer.TaskServer.Network.Messages;

#endregion

namespace UlteriusServer.TaskServer.Network
{
    public abstract class PacketHandler
    {
        public abstract void HandlePacket(Packet packet);
    }
}