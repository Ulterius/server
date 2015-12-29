#region

using System.Web.Script.Serialization;
using RemoteTaskServer.Utilities;

#endregion

namespace UlteriusServer.Api
{
    internal class SettingsApi
    {
        private static readonly Settings settings = new Settings();


        public static string GenerateNewAPiKey(string oldKey)
        {
            var currentKey = settings.Read("ApiKey", "TaskServer");
            if (oldKey.Equals(currentKey))
            {
                var newKey = Tools.GenerateAPIKey();
                settings.Write("ApiKey", newKey, "TaskServer");

                return
                    new JavaScriptSerializer().Serialize(
                        new
                        {
                            keyGenerated = true,
                            key = newKey
                        });
            }
            return new JavaScriptSerializer().Serialize(
                new
                {
                    keyGenerated = false
                });
        }

        public static string GetCurrentSettings()
        {
            var UseWebServer = settings.Read("UseWebServer", "WebServer");
            var WebServerPort = settings.Read("WebServerPort", "WebServer");
            var WebFilePath = settings.Read("WebFilePath", "WebServer");
            var TaskServerPort = settings.Read("TaskServerPort", "TaskServer");
            var SkipHostNameResolve = settings.Read("SkipHostNameResolve", "Network");
            return new JavaScriptSerializer().Serialize(
                new
                {
                    UseWebServer,
                    WebServerPort,
                    WebFilePath,
                    TaskServerPort,
                    SkipHostNameResolve
                });
        }

        public static string ChangeNetworkResolve(string status)
        {
            settings.Write("SkipHostNameResolve", status, "Network");
            var currentStatus = settings.Read("SkipHostNameResolve", "Network");
            return new JavaScriptSerializer().Serialize(
                new
                {
                    statusUpdated = true,
                    resolveNetworks = currentStatus
                });
        }

        public static string ChangeWebServerPort(string port)
        {
            settings.Write("WebServerPort", port, "WebServer");
            var currentStatus = settings.Read("WebServerPort", "WebServer");
            return new JavaScriptSerializer().Serialize(
                new
                {
                    statusUpdated = true,
                    webServerPort = currentStatus
                });
        }

        public static string ChangeTaskServerPort(string port)
        {
            settings.Write("TaskServerPort", port, "TaskServer");
            var currentStatus = settings.Read("TaskServerPort", "TaskServer");
            return new JavaScriptSerializer().Serialize(
                new
                {
                    statusUpdated = true,
                    taskServerPort = currentStatus
                });
        }

        public static string ChangeWebFilePath(string path)
        {
            settings.Write("WebFilePath", path, "WebServer");
            var currentStatus = settings.Read("WebFilePath", "WebServer");
            return new JavaScriptSerializer().Serialize(
                new
                {
                    statusUpdated = true,
                    webFilePath = currentStatus
                });
        }

        public static string ChangeWebServerUse(string status)
        {
            settings.Write("UseWebServer", status, "WebServer");
            var currentStatus = settings.Read("UseWebServer", "WebServer");
            return new JavaScriptSerializer().Serialize(
                new
                {
                    statusUpdated = true,
                    useWebServer = currentStatus
                });
        }
    }
}