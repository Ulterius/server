#region

using System.Web.Script.Serialization;
using UlteriusServer.Utilities;

#endregion

namespace UlteriusServer.Windows.Api
{
    internal class SettingsApi
    {
        private static readonly Settings settings = new Settings();


        public static string GenerateNewAPiKey(string oldKey)
        {
            var currentKey = settings.Read("TaskServer", "ApiKey", "");
            if (oldKey.Equals(currentKey))
            {
                var newKey = Tools.GenerateAPIKey();
                settings.Write("TaskServer", "ApiKey", newKey);

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

        public static void GenerateSettings()
        {
            settings.Write("WebServer", "UseWebServer", false);
            settings.Write("WebServer", "WebServerPort", 9999);
           settings.Write("WebServer", "WebFilePath", "");
            settings.Write("TaskServer", "TaskServerPort", 8387);
            settings.Write("Network", "SkipHostNameResolve", false);
        }

        public static string GetCurrentSettings()
        {
            var UseWebServer = settings.Read("WebServer", "UseWebServer", false);
            var WebServerPort = settings.Read("WebServer", "WebServerPort", 9999);
            var WebFilePath = settings.Read("WebServer", "WebFilePath", "");
            var TaskServerPort = settings.Read("TaskServer", "TaskServerPort", 8387);
            var SkipHostNameResolve = settings.Read("Network", "SkipHostNameResolve",  false);
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
            settings.Write("Network", "SkipHostNameResolve", status);
            var currentStatus = settings.Read("Network",  "SkipHostNameResolve", false);
            return new JavaScriptSerializer().Serialize(
                new
                {
                    statusUpdated = true,
                    resolveNetworks = currentStatus
                });
        }

        public static string ChangeWebServerPort(string port)
        {
            settings.Write("WebServer", "WebServerPort", port);
            var currentStatus = settings.Read("WebServer", "WebServerPort", 9999);
            return new JavaScriptSerializer().Serialize(
                new
                {
                    statusUpdated = true,
                    webServerPort = currentStatus
                });
        }

        public static string ChangeTaskServerPort(string port)
        {
            settings.Write("TaskServer", "TaskServerPort", port);
            var currentStatus = settings.Read("TaskServer",  "TaskServerPort",  8387);
            return new JavaScriptSerializer().Serialize(
                new
                {
                    statusUpdated = true,
                    taskServerPort = currentStatus
                });
        }

        public static string ChangeWebFilePath(string path)
        {
            settings.Write("WebServer", "WebFilePath", path);
            var currentStatus = settings.Read("WebServer", "WebFilePath", "");
            return new JavaScriptSerializer().Serialize(
                new
                {
                    statusUpdated = true,
                    webFilePath = currentStatus
                });
        }

        public static string ChangeWebServerUse(string status)
        {
            settings.Write("WebServer", "UseWebServer", status);
            var currentStatus = settings.Read("WebServer", "UseWebServer",  false);
            return new JavaScriptSerializer().Serialize(
                new
                {
                    statusUpdated = true,
                    useWebServer = currentStatus
                });
        }
    }
}