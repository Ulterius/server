#region

using System.Net;
using System.Security;
using UlteriusScreenShare;
using UlteriusServer.Utilities;

#endregion

namespace UlteriusServer.TaskServer
{
    public class ScreenShare
    {
        private readonly ScreenShareServer _server;
        //temp let people set this
        private readonly string _serverName = "Ulterius Screen Share";

        public ScreenShare()
        {
            var screenSharePort = (int) Settings.Get("ScreenShare").VncPort;
            SecureString screenSharePass = ToSecureString(Settings.Get("ScreenShare").VncPass.ToString());
            _server = new ScreenShareServer(_serverName, screenSharePass, IPAddress.Any, screenSharePort);
        }

        public string GetServerName()
        {
            return _serverName;
        }

        public bool ServerAvailable()
        {
            return _server.PortAvailable();
        }

        public bool Start()
        {
            return _server.Start();
        }

        public bool Stop()
        {
            return _server.Stop();
        }

        public static SecureString ToSecureString(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return null;
            var result = new SecureString();
            foreach (var c in source)
                result.AppendChar(c);
            return result;
        }
    }
}