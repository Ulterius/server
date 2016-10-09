#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UlteriusServer.Api.Network.Messages;
using UlteriusServer.Api.Services.Network;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Extensions;
using UlteriusServer.Utilities.Files;
using UlteriusServer.Utilities.Security;
using UlteriusServer.WebSocketAPI.Authentication;
using vtortola.WebSockets;
using ZetaLongPaths;
using static UlteriusServer.Api.Network.PacketManager;
using File = System.IO.File;

#endregion

namespace UlteriusServer.Api.Network.PacketHandlers
{
    public class FilePacketHandler : PacketHandler
    {


        private MessageBuilder _builder;
        private AuthClient _authClient;
        private WebSocket _client;
        private Packet _packet;

        public void CreateFileTree()
        {
            var argumentSize = _packet.Args.Count;
            var path = _packet.Args[0].ToString();
            var deepWalk = false;
            if (argumentSize > 1)
            {
                deepWalk = (bool) _packet.Args[1];
            }
            var tree = new FileTree(path, deepWalk);
            _builder.WriteMessage(tree);
        }

 

        public long DirSize(ZlpDirectoryInfo d)
        {
            // Add file sizes.
            var fis = d.GetFiles();
            var size = fis.Sum(fi => fi.Length);
            // Add subdirectory sizes.
            var dis = d.GetDirectories();
            size += dis.Sum(di => DirSize(di));
            return size;
        }

     

        private List<string> Search(string keyword)
        {
            Console.WriteLine(keyword);
            return UlteriusApiServer.FileSearchService.Search(keyword);
        }

        public void SearchFile()
        {
            try
            {
                if (!UlteriusApiServer.FileSearchService.IsScanning())
                {
                    var query = _packet.Args[0].ToString();
                    if (query.Length < 3)
                    {
                        var shortResponse = new
                        {
                            success = false,
                            message = "Query not long enough, must be 3 characters."
                        };
                        _builder.WriteMessage(shortResponse);
                        return;
                    }
                    var stopwatch = Stopwatch.StartNew();

                    var searchResults = Search(query);
                    var totalResults = searchResults.Count();


                    stopwatch.Stop();
                    var searchGenerationTime = stopwatch.ElapsedMilliseconds;
                    var data = new
                    {
                        success = true,
                        searchGenerationTime,
                        totalResults,
                        searchResults
                    };
                    _builder.WriteMessage(data);
                }
                else
                {
                    Console.WriteLine("Scan running");
                    var error = new
                    {
                        success = false,
                        message =
                            $"File index is currently scanning drive: {UlteriusApiServer.FileSearchService.CurrentScanDrive()}"
                    };
                    _builder.WriteMessage(error);
                }
            }
            catch (Exception e)
            {
                var error = new
                {
                    success = false,
                    message = e.Message
                };
                _builder.WriteMessage(error);
            }
        }

     


        public void RequestFile()
        {
            var path = _packet.Args[0].ToString();
            var password = _packet.Args[1].ToString();
            if (File.Exists(path))
            {
                var totalSize = new FileInfo(path).Length;
                ProcessFile(path, password, totalSize);
            }
            else
            {
                var data = new
                {
                    path,
                    fileValid = false
                };
                _builder.WriteMessage(data);
            }
        }

        public void RemoveFile()
        {
            var fileName = _packet.Args[0].ToString();
       
            var webPath = Settings.Get("WebServer").WebFilePath.ToString();
            var tempFolderPath = webPath + "temp\\";
            string[] filePaths = Directory.GetFiles(tempFolderPath, "*.*",
                SearchOption.TopDirectoryOnly);
            foreach (var file in filePaths)
            {
                var path = Path.GetFileName(file);
                if (path != null && path.Equals(fileName))
                {
                    if (File.Exists(file))
                    {
                        try
                        {
                            File.Delete(file);
                            var deleteData = new
                            {
                                deleted = true,
                                message = "File removed."
                            };
                            _builder.WriteMessage(deleteData);
                        }
                        catch (Exception e)
                        {
                            var deleteDataException = new
                            {
                                deleted = false,
                                message = e.Message
                            };
                            _builder.WriteMessage(deleteDataException);
                        }
                    }
                    else
                    {
                        var deleteData = new
                        {
                            deleted = false,
                            message = "File does not exist or cannot be deleted"
                        };
                        _builder.WriteMessage(deleteData);
                    }
                }
            }
        }

        public void ApproveFile()
        {
            var fileKey = _packet.Args[0].ToString();
            var destPath = _packet.Args[1].ToString();
            var password = _packet.Args[2].ToString().ToSecureString();
            FileManager.AddFile(password, destPath, fileKey);
            var approved = new
            {
                fileApproved = true,
                message = "File added to whitelist"
            };
            _builder.WriteMessage(approved);
        }


        public void ProcessFile(string path, string password, long totalSize)
        {
            var webPath = Settings.Get("WebServer").WebFilePath.ToString();
            var tempFolderPath = webPath + "temp\\";
            if (!Directory.Exists(tempFolderPath))
            {
                Directory.CreateDirectory(tempFolderPath);
            }
            var file = new FileInfo(path);
            file.Directory?.Create(); // If the directory already exists, this method does nothing.


            var fileName = Path.GetFileName(path);

            var ip = NetworkService.GetAddress();
            var port = (int) Settings.Get("WebServer").WebServerPort;

            var passwordBytes = Encoding.UTF8.GetBytes(password);

            if (File.Exists(path))
            {
                try
                {
                    var tempPath = Path.Combine(tempFolderPath, fileName);
                    UlteriusAes.EncryptFile(passwordBytes, path, tempPath);
                    var tempWebPath = $"http://{ip}:{port}/temp/{fileName}";
                    var downloadData = new
                    {
                        tempWebPath,
                        totalSize
                    };
                    _builder.WriteMessage(downloadData);
                }
                catch (Exception e)
                {

                    var exceptionData = new
                    {
                        error = true,
                        message = e.Message
                    };

                    _builder.WriteMessage(exceptionData);
                }
            }
            else
            {
                var errorData = new
                {
                    error = true,
                    message = "Unable to encrypt file"
                };
                _builder.WriteMessage(errorData);
            }
        }

        public override void HandlePacket(Packet packet)
        {
            _client = packet.Client;
            _authClient = packet.AuthClient;
            _packet = packet;
            _builder = new MessageBuilder(_authClient, _client, _packet.EndPoint, _packet.SyncKey);
            switch (_packet.PacketType)
            {
                case PacketTypes.SearchFiles:
                    SearchFile();
                    break;
                case PacketTypes.ApproveFile:
                    ApproveFile();
                    break;
                case PacketTypes.RequestFile:
                    RequestFile();
                    break;
                case PacketTypes.RemoveFile:
                    RemoveFile();
                    break;
                case PacketTypes.CreateFileTree:
                    CreateFileTree();
                    break;
            }
        }

     
    }
}