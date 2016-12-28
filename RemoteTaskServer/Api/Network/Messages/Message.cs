#region

using vtortola.WebSockets;

#endregion

namespace UlteriusServer.Api.Network.Messages
{
    public class Message
    {
        /// <summary>
        /// This enum contains the three different message types for packets
        /// Text packets are plain text json
        /// Binary packets are encrypted, either json or screen share frames
        /// Service packets are being passed locally to the ulterius agent.
        /// </summary>
        public enum MessageType
        {
            Text,
            Binary,
            Service
        }

        public readonly byte[] Data;
        public readonly string PlainTextData;
        public readonly MessageType Type;


        public Message(WebSocket remoteClient, byte[] data, MessageType type)
        {
            RemoteClient = remoteClient;
            Data = data;
            Type = type;
        }

        public Message(WebSocket remoteClient, string plainTextData, MessageType type)
        {
            RemoteClient = remoteClient;
            PlainTextData = plainTextData;
            Type = type;
        }

        public Message(string message, MessageType type)
        {
            PlainTextData = message;
            Type = type;
        }

        public WebSocket RemoteClient { get; set; }
    }
}