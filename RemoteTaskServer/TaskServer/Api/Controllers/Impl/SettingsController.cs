#region

using System;
using System.Linq;
using UlteriusServer.TaskServer.Api.Serialization;
using UlteriusServer.Utilities;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer.Api.Controllers.Impl
{
    public class SettingsController : ApiController
    {
        private static readonly Settings settings = new Settings();
        private readonly WebSocket client;
        private readonly Packets packet;
        private readonly ApiSerializator serializator = new ApiSerializator();

        public SettingsController(WebSocket client, Packets packet)
        {
            this.client = client;
            this.packet = packet;
        }

        private string GenerateAPIKey()
        {
            var res = "";
            var rnd = new Random();
            while (res.Length < 35)
                res += new Func<Random, string>(r =>
                {
                    var c = (char) (r.Next(123)*DateTime.Now.Millisecond%123);
                    return char.IsLetterOrDigit(c) ? c.ToString() : "";
                })(rnd);
            return res;
        }

        public void ChangeWebServerPort()
        {
            var port = int.Parse(packet.args.First().ToString());
            settings.Write("WebServer", "WebServerPort", port);
            var currentPort = settings.Read("WebServer", "WebServerPort", 9999);
            var data = new
            {
                changedStatus = true,
                newPort = currentPort
            };
            serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
        }

        public void ChangeWebFilePath()
        {
            var path = packet.args.First().ToString();
            settings.Write("WebServer", "WebFilePath", path);
            var currentPath = settings.Read("WebServer", "WebFilePath", "");
            var data = new
            {
                changedStatus = true,
                newPath = currentPath
            };
            serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
        }

        public void ChangeVncPassword()
        {
            var pass = packet.args.First().ToString();
            settings.Write("Vnc", "VncPass", pass);
            var currentPass = settings.Read("Vnc", "VncPass", "");
            var data = new
            {
                changedStatus = true,
                newPass = currentPass
            };
            serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
        }

        public void ChangeVncPort()
        {
            var port = int.Parse(packet.args.First().ToString());
            settings.Write("Vnc", "VncPort", port);
            var currentPort = settings.Read("Vnc", "VncPort", 5900);
            var data = new
            {
                changedStatus = true,
                newPort = currentPort
            };
            serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
        }
        public void ChangeVncProxyPort()
        {
            var port = int.Parse(packet.args.First().ToString());
            settings.Write("Vnc", "VncProxyPort", port);
            var currentPort = settings.Read("Vnc", "VncProxyPort", 5900);
            var data = new
            {
                changedStatus = true,
                newPort = currentPort
            };
            serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
        }

        public void ChangeWebServerUse()
        {
            var useServer = (bool) packet.args.First();
            settings.Write("WebServer", "UseWebServer", useServer);
            var currentStatus = settings.Read("WebServer", "UseWebServer", false);
            var data = new
            {
                changedStatus = true,
                useWebServer = currentStatus
            };
            serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
        }

        public void ChangeNetworkResolve()
        {
            var resolve = (bool) packet.args.First();
            settings.Write("Network", "SkipHostNameResolve", resolve);
            var currentStatus = settings.Read("Network", "SkipHostNameResolve", false);
            var data = new
            {
                changedStatus = true,
                newStatus = currentStatus
            };
            serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
        }

        public void ChangeTaskServerPort()
        {
            var port = int.Parse(packet.args.First().ToString());
            settings.Write("TaskServer", "TaskServerPort", port);
            var currentPort = settings.Read("TaskServer", "TaskServerPort", 8387);
            var data = new
            {
                changedStatus = true,
                newPort = currentPort
            };
            serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
        }

        public void GetCurrentSettings()
        {
            var UseWebServer = settings.Read("WebServer", "UseWebServer", false);
            var WebServerPort = settings.Read("WebServer", "WebServerPort", 9999);
            var WebFilePath = settings.Read("WebServer", "WebFilePath", "");
            var TaskServerPort = settings.Read("TaskServer", "TaskServerPort", 8387);
            var SkipHostNameResolve = settings.Read("Network", "SkipHostNameResolve", false);
            var VncProxyPort = settings.Read("Vnc", "VncProxyPort", 5901);
            var VncPort = settings.Read("Vnc", "VncPort", 5900);
            var VncPass = settings.Read("Vnc", "VncPass", "");
            var currentSettingsData = new
            {
                UseWebServer,
                WebServerPort,
                WebFilePath,
                TaskServerPort,
                SkipHostNameResolve,
                VncPort,
                VncProxyPort,
                VncPass
            };
            serializator.Serialize(client, packet.endpoint, packet.syncKey, currentSettingsData);
        }

        public void GenerateNewAPiKey()
        {
            var keyGenerated = false;
            string newKey = null;
            var currentKey = settings.Read("TaskServer", "ApiKey", "");
            var oldKey = packet.apiKey;
            if (oldKey.Equals(currentKey))
            {
                keyGenerated = true;
                newKey = GenerateAPIKey();
                settings.Write("TaskServer", "ApiKey", newKey);
            }
            var data = new
            {
                keyGenerated,
                newKey
            };
            serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
        }
    }
}