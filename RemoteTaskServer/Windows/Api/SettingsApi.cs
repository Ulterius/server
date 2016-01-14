#region

using System.IO;
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
            var apiKeyData = new
            {
                keyGenerated = false,
                key = ""
            };
            if (oldKey.Equals(currentKey))
            {
                var newKey = Tools.GenerateAPIKey();
                settings.Write("TaskServer", "ApiKey", newKey);
                apiKeyData = new
                {
                    keyGenerated = true,
                    key = newKey
                };
                return
                    new JavaScriptSerializer().Serialize(
                        new
                        {
                            endpoint = "generateNewKey",
                            results = apiKeyData
                        });
            }
            return new JavaScriptSerializer().Serialize(
                new
                {
                    endpoint = "generateNewKey",
                    results = apiKeyData
                });
        }

        public static void GenerateSettings()
        {
            if (!File.Exists("UlteriusServer.ini"))
            {
                settings.Write("WebServer", "UseWebServer", false);
                settings.Write("WebServer", "WebServerPort", 9999);
                settings.Write("WebServer", "WebFilePath", "");
                settings.Write("TaskServer", "TaskServerPort", 8387);
                settings.Write("Network", "SkipHostNameResolve", false);
            }
        }

        public static string GetCurrentSettings()
        {
            var UseWebServer = settings.Read("WebServer", "UseWebServer", false);
            var WebServerPort = settings.Read("WebServer", "WebServerPort", 9999);
            var WebFilePath = settings.Read("WebServer", "WebFilePath", "");
            var TaskServerPort = settings.Read("TaskServer", "TaskServerPort", 8387);
            var SkipHostNameResolve = settings.Read("Network", "SkipHostNameResolve", false);

            var currentSettingsData = new
            {
                UseWebServer,
                WebServerPort,
                WebFilePath,
                TaskServerPort,
                SkipHostNameResolve
            };
            return new JavaScriptSerializer().Serialize(
                new
                {
                    endpoint = "getCurrentSettings",
                    results = currentSettingsData
                });
        }

        public static string ChangeNetworkResolve(string status)
        {
            settings.Write("Network", "SkipHostNameResolve", status);
            var currentStatus = settings.Read("Network", "SkipHostNameResolve", false);
            var networkResolveData = new
            {
                changedStatus = true,
                resolveNetworks = currentStatus
            };
            return new JavaScriptSerializer().Serialize(
                new
                {
                    endpoint = "changeNetworkResolve",
                    results = networkResolveData
                });
        }

        public static string ChangeWebServerPort(string port)
        {
            settings.Write("WebServer", "WebServerPort", port);
            var currentStatus = settings.Read("WebServer", "WebServerPort", 9999);
            var changeWebServerData = new
            {
                changedStatus = true,
                webServerPort = currentStatus
            };
            return new JavaScriptSerializer().Serialize(
                new
                {
                    endpoint = "changeWebServerPort",
                    results = changeWebServerData
                });
        }

        public static string ChangeTaskServerPort(string port)
        {
            settings.Write("TaskServer", "TaskServerPort", port);
            var currentStatus = settings.Read("TaskServer", "TaskServerPort", 8387);
            var changeTaskServerData = new
            {
                changedStatus = true,
                askServerPort = currentStatus
            };
            return new JavaScriptSerializer().Serialize(
                new
                {
                    endpoint = "changeTaskServerPort",
                    results = changeTaskServerData
                });
        }

        public static string ChangeWebFilePath(string path)
        {
            settings.Write("WebServer", "WebFilePath", path);
            var currentStatus = settings.Read("WebServer", "WebFilePath", "");
            var changeWebFileData = new
            {
                changedStatus = true,
                webFilePath = currentStatus
            };
            return new JavaScriptSerializer().Serialize(
                new
                {
                    endpoint = "changeWebFilePath",
                    results = changeWebFileData
                });
        }

        public static string ChangeWebServerUse(string status)
        {
            settings.Write("WebServer", "UseWebServer", status);
            var currentStatus = settings.Read("WebServer", "UseWebServer", false);
            var changeWebServerUseData = new
            {
                changedStatus = true,
                useWebServer = currentStatus
            };
            return new JavaScriptSerializer().Serialize(
                new
                {
                    endpoint = "toggleWebServer",
                    results = changeWebServerUseData
                });
        }
    }
}