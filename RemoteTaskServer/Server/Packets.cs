using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Web;
using RemoteTaskServer.WebSocketAPI;

namespace RemoteTaskServer.Server

{
    [Serializable]
    public class Packets
    {
        public string command;
        public string action;
        public PacketType packetType;
        public string paramaters;
        public Uri query;
        public string senderID;

        public Packets(PacketType type, string senderID)
        {
            this.senderID = senderID;
            packetType = type;

            Console.WriteLine();
        }
        private string GetQueryString(string url, string key)
        {
            string query_string = string.Empty;

            var uri = new Uri(url);
            var newQueryString = HttpUtility.ParseQueryString(uri.Query);
            if (newQueryString[key] != null)
            {
                query_string = newQueryString[key].ToString();
            }
          

            return query_string;
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

            action = GetQueryString(decodedQuery, "command");

            senderID = "server";
            switch (command)
            {
                case "requestprocessinformation":
                    Console.WriteLine("Request Process Information");
                    packetType = PacketType.RequestProcess;
                    break;
                case "requestcpuinformation":
                    Console.WriteLine("Request CPU Information");
                    packetType = PacketType.RequestCpuInformation;
                    break;
                case "requestosinformation":
                    Console.WriteLine("Request OS Information");
                    packetType = PacketType.RequestOsInformation;
                    break;
                case "requestnetworkinformation":
                    Console.WriteLine("Request Network Information");
                    packetType = PacketType.RequestNetworkInformation;
                    break;
                case "requestsysteminformation":
                    Console.WriteLine("Request System Information");
                    packetType = PacketType.RequestSystemInformation;
                    break;
                case "startprocess":
                    Console.WriteLine("Starting Process " + action);
                    packetType = PacketType.StartProcess;
                    break;
                case "killprocess":
                    Console.WriteLine("Killing Process " + action);
                    packetType = PacketType.KillProcess;
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
        RequestProcess,
        RequestCpuInformation,
        RequestOsInformation,
        RequestNetworkInformation,
        RequestSystemInformation,
        StartProcess,
        KillProcess
    }
}