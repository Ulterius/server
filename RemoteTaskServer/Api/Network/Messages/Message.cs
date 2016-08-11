#region

using UlteriusServer.WebSocketAPI.Authentication;

#endregion

namespace UlteriusServer.Api.Network.Messages
{
    public class Message
    {

       
        public enum MessageType
        {
            Text,
            Binary
        }

        public readonly byte[] Data;
        public readonly string Json;
        public readonly MessageType Type;


     
        public Message(AuthClient authClient, byte[] data, MessageType type)
        {
            AuthClient = authClient;
            Data = data;
            Type = type;
        }

        public Message(AuthClient authClient, string json, MessageType type)
        {
            AuthClient = authClient;
            Json = json;
            Type = type;
        }

        public AuthClient AuthClient { get; set; }
    }
}