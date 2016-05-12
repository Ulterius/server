#region

using System.Linq;
using RemoteTaskServer.WebServer;
using UlteriusServer.TaskServer.Api.Serialization;
using UlteriusServer.Utilities;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer.Api.Controllers.Impl
{
    public class SettingsController : ApiController
    {
        private static readonly Settings Settings = new Settings();
        private readonly WebSocket _client;
        private readonly Packets _packet;
        private readonly ApiSerializator _serializator = new ApiSerializator();

        public SettingsController(WebSocket client, Packets packet)
        {
            _client = client;
            _packet = packet;
        }


        public void ChangeWebServerPort()
        {
            var port = int.Parse(_packet.Args[0].ToString());
            Settings.Write("WebServer", "WebServerPort", port);
            var currentPort = Settings.Read("WebServer", "WebServerPort", 9999);
            var data = new
            {
                changedStatus = true,
                WebServerPort = currentPort
            };
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
        }

        public void ChangeWebFilePath()
        {
            var path = _packet.Args[0].ToString();
            Settings.Write("WebServer", "WebFilePath", path);
            var currentPath = Settings.Read("WebServer", "WebFilePath", HttpServer.defaultPath);
            var data = new
            {
                changedStatus = true,
                WebFilePath = currentPath
            };
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
        }

        public void ChangeVncPassword()
        {
            var pass = _packet.Args[0].ToString();
            Settings.Write("Vnc", "VncPass", pass);
            var currentPass = Settings.Read("Vnc", "VncPass", "");
            var data = new
            {
                changedStatus = true,
                VncPass = currentPass
            };
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
        }

        public void ChangeVncPort()
        {
            var port = int.Parse(_packet.Args[0].ToString());
            Settings.Write("Vnc", "VncPort", port);
            var currentPort = Settings.Read("Vnc", "VncPort", 5900);
            var data = new
            {
                changedStatus = true,
                VncPort = currentPort
            };
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
        }

        public void ChangeVncProxyPort()
        {
            var port = int.Parse(_packet.Args[0].ToString());
            Settings.Write("Vnc", "VncProxyPort", port);
            var currentPort = Settings.Read("Vnc", "VncProxyPort", 5900);
            var data = new
            {
                changedStatus = true,
                VncProxyPort = currentPort
            };
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
        }

        public void ChangeWebServerUse()
        {
            var useServer = (bool) _packet.Args[0];
            Settings.Write("WebServer", "UseWebServer", useServer);
            var currentStatus = Settings.Read("WebServer", "UseWebServer", false);
            var data = new
            {
                changedStatus = true,
                UseWebServer = currentStatus
            };
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
        }

        public void ChangeNetworkResolve()
        {
            var resolve = (bool) _packet.Args[0];
            Settings.Write("Network", "SkipHostNameResolve", resolve);
            var currentStatus = Settings.Read("Network", "SkipHostNameResolve", false);
            var data = new
            {
                changedStatus = true,
                SkipHostNameResolve = currentStatus
            };
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
        }

        public void ChangeTaskServerPort()
        {
            var port = int.Parse(_packet.Args[0].ToString());
            Settings.Write("TaskServer", "TaskServerPort", port);
            var currentPort = Settings.Read("TaskServer", "TaskServerPort", 8387);
            var data = new
            {
                changedStatus = true,
                TaskServerPort = currentPort
            };
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
        }

        public void ChangeLoadPlugins()
        {
            var loadPlugins = (bool) _packet.Args[0];
            Settings.Write("Plugins", "LoadPlugins", loadPlugins);
            var currentStatus = Settings.Read("Plugins", "LoadPlugins", true);
            var data = new
            {
                changedStatus = true,
                LoadPlugins = currentStatus
            };
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
        }

        public void ChangeUseTerminal()
        {
            var useTerminal = (bool) _packet.Args[0];
            Settings.Write("Terminal", "AllowTerminal", useTerminal);
            var currentStatus = Settings.Read("Terminal", "AllowTerminal", true);
            var data = new
            {
                changedStatus = true,
                AllowTerminal = currentStatus
            };
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
        }

        public void GetCurrentSettings()
        {
            var UseWebServer = Settings.Read("WebServer", "UseWebServer", true);
            var WebServerPort = Settings.Read("WebServer", "WebServerPort", 22006);
            var WebFilePath = Settings.Read("WebServer", "WebFilePath", HttpServer.defaultPath);
            var TaskServerPort = Settings.Read("TaskServer", "TaskServerPort", 22007);
            var SkipHostNameResolve = Settings.Read("Network", "SkipHostNameResolve", false);
            var VncProxyPort = Settings.Read("Vnc", "VncProxyPort", 5901);
            var VncPort = Settings.Read("Vnc", "VncPort", 5900);
            var VncPass = Settings.Read("Vnc", "VncPass", "");
            var AllowTerminal = Settings.Read("Terminal", "AllowTerminal", true);
            var LoadPlugins = Settings.Read("Plugins", "LoadPlugins", true);
            var currentSettingsData = new
            {
                UseWebServer,
                WebServerPort,
                WebFilePath,
                TaskServerPort,
                SkipHostNameResolve,
                VncPort,
                VncProxyPort,
                VncPass,
                AllowTerminal,
                LoadPlugins
            };
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, currentSettingsData);
        }
    }
}