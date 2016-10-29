#region

using vtortola.WebSockets;

#endregion

namespace UlteriusServer.Api.Network.Messages
{
    public class Message
    {
        public enum MessageType
        {
            Text,
            Binary,
            Service
        }

        public readonly byte[] Data;
        public readonly string Json;
        public readonly MessageType Type;


        public Message(WebSocket remoteClient, byte[] data, MessageType type)
        {
            RemoteClient = remoteClient;
            Data = data;
            Type = type;
        }

        public Message(WebSocket remoteClient, string json, MessageType type)
        {
            RemoteClient = remoteClient;
            Json = json;
            Type = type;
        }

        public Message(string message, MessageType type)
        {
            Json = message;
            Type = type;
        }

        public WebSocket RemoteClient { get; set; }
    }
}