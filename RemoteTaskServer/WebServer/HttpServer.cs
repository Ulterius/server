#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using MassTransit.Transports;
using Newtonsoft.Json;
using UlteriusServer.Api.Services.Network;
using UlteriusServer.Properties;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Files;
using UlteriusServer.WebServer.RemoteTaskServer.WebServer;
using File = System.IO.File;

#endregion

namespace UlteriusServer.WebServer
{
    internal class HttpServer
    {
        public static string DefaultPath = AppEnvironment.DataPath +
                                           @"\client\";

        private static readonly IDictionary<string, string> MimeTypeMappings =
            new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                #region extension to MIME type list
                {".asf", "video/x-ms-asf"},
                {".asx", "video/x-ms-asf"},
                {".avi", "video/x-msvideo"},
                {".bin", "application/octet-stream"},
                {".cco", "application/x-cocoa"},
                {".crt", "application/x-x509-ca-cert"},
                {".css", "text/css"},
                {".svg", "image/svg+xml"},
                {".svgz", "image/svg+xml"},
                {".deb", "application/octet-stream"},
                {".der", "application/x-x509-ca-cert"},
                {".dll", "application/octet-stream"},
                {".dmg", "application/octet-stream"},
                {".ear", "application/java-archive"},
                {".eot", "application/octet-stream"},
                {".exe", "application/octet-stream"},
                {".flv", "video/x-flv"},
                {".gif", "image/gif"},
                {".hqx", "application/mac-binhex40"},
                {".htc", "text/x-component"},
                {".htm", "text/html"},
                {".html", "text/html"},
                {".ico", "image/x-icon"},
                {".img", "application/octet-stream"},
                {".iso", "application/octet-stream"},
                {".jar", "application/java-archive"},
                {".jardiff", "application/x-java-archive-diff"},
                {".jng", "image/x-jng"},
                {".jnlp", "application/x-java-jnlp-file"},
                {".jpeg", "image/jpeg"},
                {".jpg", "image/jpeg"},
                {".js", "application/x-javascript"},
                {".mml", "text/mathml"},
                {".mng", "video/x-mng"},
                {".mov", "video/quicktime"},
                {".mp3", "audio/mpeg"},
                {".mpeg", "video/mpeg"},
                {".mpg", "video/mpeg"},
                {".msi", "application/octet-stream"},
                {".msm", "application/octet-stream"},
                {".msp", "application/octet-stream"},
                {".pdb", "application/x-pilot"},
                {".pdf", "application/pdf"},
                {".pem", "application/x-x509-ca-cert"},
                {".pl", "application/x-perl"},
                {".pm", "application/x-perl"},
                {".png", "image/png"},
                {".prc", "application/x-pilot"},
                {".ra", "audio/x-realaudio"},
                {".rar", "application/x-rar-compressed"},
                {".rpm", "application/x-redhat-package-manager"},
                {".rss", "text/xml"},
                {".run", "application/x-makeself"},
                {".sea", "application/x-sea"},
                {".shtml", "text/html"},
                {".sit", "application/x-stuffit"},
                {".swf", "application/x-shockwave-flash"},
                {".tcl", "application/x-tcl"},
                {".tk", "application/x-tcl"},
                {".txt", "text/plain"},
                {".war", "application/java-archive"},
                {".wbmp", "image/vnd.wap.wbmp"},
                {".wmv", "video/x-ms-wmv"},
                {".xml", "text/xml"},
                {".xpi", "application/x-xpinstall"},
                {".zip", "application/zip"}

                #endregion
            };

        public static int GlobalPort;

        private readonly string[] _indexFiles =
        {
            "index.html",
            "index.htm",
            "default.html",
            "default.htm"
        };

        private HttpListener _listener;
        private string _rootDirectory;
        private Thread _serverThread;

        /// <summary>
        ///     Construct server with given port.
        /// </summary>
        /// <param name="path">Directory path to serve.</param>
        /// <param name="port">Port of the server.</param>
        public HttpServer(string path, int port)
        {
            Initialize(path, port);
        }

        /// <summary>
        ///     Construct server with suitable port.
        /// </summary>
        /// <param name="path">Directory path to serve.</param>
        public HttpServer(string path)
        {
            //get an empty port
   
            var l = new TcpListener(NetworkService.GetAddress(), 0);
            l.Start();
            var port = ((IPEndPoint) l.LocalEndpoint).Port;
            l.Stop();
            Initialize(path, port);
        }

        public int Port { get; private set; }

        public static void Setup()
        {
            var useWebServer = Convert.ToBoolean(Settings.Get("WebServer").ToggleWebServer);
            if (useWebServer)
            {
                var root = Settings.Get("WebServer").WebFilePath.ToString();
                var port = (int) Settings.Get("WebServer").WebServerPort;
                GlobalPort = port;
                var httpServer = new HttpServer(root, port);
                Console.WriteLine(Resources.Program_Main_Web_Server_is_running_on_this_port__ + httpServer.Port);
            }
        }

        /// <summary>
        ///     Stop server and dispose all functions.
        /// </summary>
        public void Stop()
        {
            _serverThread.Abort( );
            _listener.Stop();
        }

        private async void Listen()
        {
            var prefix = $"http://*:{Port}/";
            var username = Environment.GetEnvironmentVariable("USERNAME");
            var userdomain = Environment.GetEnvironmentVariable("USERDOMAIN");
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://*:" + Port + "/");

            try
            {
                _listener.Start();
            }
            catch (HttpListenerException ex)
            {
                if (ex.ErrorCode == 5)
                {
                    Console.WriteLine(@"You need to run the following args:");
                    Console.WriteLine(@"netsh http add urlacl url={0} user={1}\{2} listen=yes",
                        prefix, userdomain, username);
                }
                else
                {
                    throw;
                }
            }
            while (true)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    Process(context);
                }
                catch (Exception)
                {
                    //ignore
                }
            }
        }

        public bool SaveFile(string fileKey, byte[] data)
        {
            if (FileManager.OnWhitelist(fileKey))
            {
                var saved = FileManager.DecryptFile(fileKey, data);
                return saved;
            }
            return false;
        }

        private void HandleUpload(HttpListenerContext context)
        {
            var request = context.Request;
            if (request.Url.AbsolutePath.Contains("upload"))
            {
                var fileKey = request.Headers["File-Key"];
                var parser = new MultipartParser(context.Request.InputStream);

                using (var writer = new StreamWriter(context.Response.OutputStream, Encoding.UTF8))
                {
                    if (parser.Success)
                    {
                        var saved = SaveFile(fileKey, parser.FileContents);

                        var responseObject = new
                        {
                            fileKey,
                            success = saved,
                            message = "File Uploaded!"
                        };
                        var json = JsonConvert.SerializeObject(responseObject);
    
                        writer.WriteLine(json);
                    }
                    else
                    {
                        var responseObject = new
                        {
                            fileKey,
                            success = false,
                            message = "The posted file was not recognised."
                        };
                        var json = JsonConvert.SerializeObject(responseObject);
                        writer.WriteLine(json);
                    }
                    context.Response.AddHeader("Access-Control-Allow-Origin", "*");
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = 200;
                    context.Response.Close();
                    context.Response.OutputStream.Close();
                }
            }
        }

        private void Process(HttpListenerContext context)
        {
            var request = context.Request;
            if (request.HttpMethod == "OPTIONS")
            {
                context.Response.AddHeader("Access-Control-Allow-Origin", "*");
                context.Response.AddHeader("Access-Control-Allow-Methods", "POST,GET,OPTIONS");
                context.Response.AddHeader("Access-Control-Allow-Headers",
                    "Content-Type, Accept, X-Requested-With, File-Key");
            }
            if (request.HttpMethod == "POST")
            {
                HandleUpload(context);
                return;
            }
            var filename = context.Request.Url.AbsolutePath;
            filename = filename.Substring(1);

            if (string.IsNullOrEmpty(filename))
            {
                foreach (
                    var indexFile in
                        _indexFiles.Where(indexFile => File.Exists(Path.Combine(_rootDirectory, indexFile))))
                {
                    filename = indexFile;
                    break;
                }
            }

            filename = HttpUtility.UrlDecode(Path.Combine(_rootDirectory, filename));

            if (File.Exists(filename))
            {
                try
                {
                    Stream input = new FileStream(filename, FileMode.Open);

                    //Adding permanent http response headers
                    string mime;

                    context.Response.ContentType = MimeTypeMappings.TryGetValue(Path.GetExtension(filename),
                        out mime)
                        ? mime
                        : "application/octet-stream";
                    context.Response.ContentLength64 = input.Length;
                    context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                    context.Response.AddHeader("Last-Modified", File.GetLastWriteTime(filename).ToString("r"));
                    context.Response.AddHeader("Access-Control-Allow-Origin", "*");
                    var buffer = new byte[1024*16];
                    int nbytes;
                    while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                        context.Response.OutputStream.Write(buffer, 0, nbytes);
                    input.Close();
                    context.Response.OutputStream.Flush();

                    context.Response.StatusCode = (int) HttpStatusCode.OK;
                }
                catch (Exception)
                {
                    context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
                }
            }
            else

            {
                if (request.Url.AbsolutePath.Contains("upload"))
                {
                    context.Response.StatusCode = 200;
                }
                else
                {
                    context.Response.StatusCode = (int) HttpStatusCode.NotFound;
                }
            }

            context.Response.OutputStream.Close();
        }

        private void Initialize
            (string path, int port)
        {
            _rootDirectory = path;
            Port = port;
            _serverThread = new Thread(Listen);
            _serverThread.Start();
        }
    }
}