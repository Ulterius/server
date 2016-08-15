#region

using System;
using System.Security;
using UlteriusScreenShare;
using UlteriusServer.Api.Services.Network;
using UlteriusServer.Utilities;

#endregion

namespace UlteriusServer.Api
{
    public class ScreenShareService
    {
        private readonly ScreenShareServer _server;
        //temp let people set this
        private readonly string _serverName = "Ulterius Screen Share";

        public ScreenShareService()
        {
            SecureString screenSharePass;
            var screenSharePort = (int) Settings.Get("ScreenShareService").ScreenSharePort;
            try
            {
                screenSharePass = ToSecureString(Settings.Get("ScreenShareService").ScreenSharePass.ToString());
            }
            catch (Exception)
            {
                screenSharePass = ToSecureString(string.Empty);
            }
            try
            {
                _server = new ScreenShareServer(_serverName, screenSharePass, NetworkService.GetAddress(), screenSharePort);
            }
            catch (Exception)
            {

            
            }
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