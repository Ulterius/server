using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using RemoteTaskServer.Utilities;

namespace RemoteTaskServer.Api
{
    class SettingsApi
    {
        static Settings settings = new Settings();


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
            else {
                return new JavaScriptSerializer().Serialize(
                new
                {
                    keyGenerated = false
                });
            }
        }

        public string ChangeNetworkResolve(string status)
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
        public string ChangeWebServerPort(string port)
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
        public string ChangeTaskServerPort(string port)
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
        public string ChangeWebFilePath(string path)
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
        public string ChangeWebServerUse(string status)
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