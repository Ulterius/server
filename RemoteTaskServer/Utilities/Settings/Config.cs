#region

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UlteriusServer.WebServer;

#endregion

namespace UlteriusServer.Utilities.Settings
{
    internal class Config
    {
        public static bool Empty;


        public General General { get; set; } = new General();
        public WebServer WebServer { get; set; } = new WebServer();
        public TaskServer TaskServer { get; set; } = new TaskServer();
        public Network Network { get; set; } = new Network();
        public Plugins Plugins { get; set; } = new Plugins();
        public ScreenShareService ScreenShareService { get; set; } = new ScreenShareService();
        public Terminal Terminal { get; set; } = new Terminal();
        public Webcams Webcams { get; set; } = new Webcams();
        public Debug Debug { get; set; } = new Debug();


        public static Config Load()
        {
            var fileName = "Config.json";
            var filePath = Path.Combine(AppEnvironment.DataPath, fileName);
            if (!File.Exists(filePath))
            {
                Empty = true;
                GenerateDefault();
            }
            var json = File.ReadAllText(filePath);
            try
            {
                return JsonConvert.DeserializeObject<Config>(json);
            }
            catch (JsonReaderException)
            {
                var settingsPath = Path.Combine(AppEnvironment.DataPath, "Config.json");
                //Handle settings failing to create, rarely happens but it does.
                File.Delete(settingsPath);
                GenerateDefault();
                json = File.ReadAllText(settingsPath);
                return JsonConvert.DeserializeObject<Config>(json);
            }
        }

      

        private static void GenerateDefault()
        {
         
            var fileName = "Config.json";
            var filePath = Path.Combine(AppEnvironment.DataPath, fileName);
            File.WriteAllText(filePath, JsonConvert.SerializeObject(new Config(), Formatting.Indented));
        }

        public static object GetRaw()
        {
            //Thanks microsoft
            var fileName = "Config.json";
            var filePath = Path.Combine(AppEnvironment.DataPath, fileName);
            var settings = JsonConvert.DeserializeObject<IDictionary<string, object>>(File.ReadAllText(filePath));
            return settings;
        }
    }

    

    internal class General
    {
        public bool RunStartup { get; set; } = true;
        public bool UploadLogs { get; set; } = true;
        public string Github { get; set; } = "https://github.com/Ulterius";
        public string ServerIssues { get; set; } = "https://github.com/Ulterius/server/issues";
        public string ClientIssues { get; set; } = "https://github.com/Ulterius/client/issues";
    }

    internal class WebServer
    {
        public string WebFilePath { get; set; } = HttpServer.DefaultPath;
        public int WebServerPort { get; set; } = 22006;
        public bool ToggleWebServer { get; set; } = true;
    }

    internal class TaskServer
    {
        public int TaskServerPort { get; set; } = 22007;
    }

    internal class Network
    {
        public bool SkipHostNameResolve { get; set; }
        public bool UpnpEnabled { get; set; } = true;
        public bool BindLocal { get; set; }
    }

    internal class Plugins
    {
        public bool LoadPlugins { get; set; } = true;
    }

    internal class ScreenShareService
    {
        public int ScreenSharePort { get; set; } = 22009;
        public int ScreenShareFps { get; set; } = 60;
    }

    internal class Terminal
    {
        public bool AllowTerminal { get; set; } = true;
        public int TerminalPort { get; set; } = 22008;
    }

    internal class Webcams
    {
        public bool UseWebcams { get; set; } = true;
        public int WebcamPort { get; set; } = 22010;
    }

    internal class Debug
    {
        public bool TraceDebug { get; set; }
    }
}