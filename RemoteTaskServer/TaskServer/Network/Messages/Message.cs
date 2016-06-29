using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlteriusServer.WebSocketAPI.Authentication;

namespace UlteriusServer.TaskServer.Network.Messages
{
    public class Message
    {
        public AuthClient AuthClient { get; set; }
        public readonly string Json;
        public readonly byte[] Data;
        public readonly MessageType Type;

        public enum MessageType
        {
            Text,
            Binary
        }

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
    }
}
