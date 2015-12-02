using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using RemoteTaskServer.Api;
using RemoteTaskServer.Utilities;
using RemoteTaskServer.WebSocketAPI;

namespace RemoteTaskServer.Server

{
    [Serializable]
    public class Packets
    {

        public string command;
        public string format;
        public Uri query;
        public string paramaters;
        public PacketType packetType;
        public string senderID;

        public Packets(PacketType type, string senderID)
        {
            
            this.senderID = senderID;
            packetType = type;

            Console.WriteLine();
        }

        public Packets(byte[] packetBytes, int readBytes)
        {


            var decodedQuery = WebSocketFunctions.DecodeMessage(packetBytes, readBytes);
            Uri myUri = null;
            try
            {
               myUri = new Uri(decodedQuery);
            }
            catch (Exception)
            {
               

                return;
            }
            query = myUri;
            command = myUri.Host;

            format = "JSON";

            senderID = "server";
            switch (command)
            {
                case "showprocesslist":
                    Console.WriteLine("Request Process");
                    packetType = PacketType.RequestProcess;
                break;
                default:
                    break;
            }
           
        }

        public static string GetIPv4Address()
        {
            var ips = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (var i in ips.Where(i => i.AddressFamily == AddressFamily.InterNetwork))
            {
                return i.ToString();
            }
            return "127.0.0.1";
        }
    }

    public enum PacketType
    {
        RequestProcess
    }
}