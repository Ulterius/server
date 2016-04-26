#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using RemoteTaskServer.WebServer;
using UlteriusServer.TaskServer.Api.Models;
using UlteriusServer.TaskServer.Api.Serialization;
using UlteriusServer.TaskServer.Services.Network;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Files;
using vtortola.WebSockets;
using static UlteriusServer.TaskServer.Api.Models.FileInformation;
using File = System.IO.File;

#endregion

namespace UlteriusServer.TaskServer.Api.Controllers.Impl
{
    public class FileController : ApiController
    {
        private readonly WebSocket _client;
        private readonly Packets _packet;
        private readonly ApiSerializator _serializator = new ApiSerializator();


        public FileController(WebSocket client, Packets packet)
        {
            _client = client;
            this._packet = packet;
        }

        public void CreateFileTree()
        {
            var argumentSize = _packet.Args.Count;
            var path = _packet.Args.First().ToString();
            var deepWalk = false;
            if (argumentSize > 1)
            {
                deepWalk = (bool) _packet.Args[1];
            }
            var tree = new FileTree(path, deepWalk);
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, tree);
        }

        public void SearchFile()
        {
        }


        public void RequestFile()
        {
            var path = _packet.Args.First().ToString();
            if (File.Exists(path))
            {
                var fileName = Path.GetFileName(path);
                var totalSize = new FileInfo(path).Length;
                ProcessFile(path, totalSize);
            }
            else
            {
                var data = new
                {
                    path,
                    fileValid = false
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
            }
        }

        public void RemoveFile()
        {
            var path = _packet.Args.First().ToString();
            //make sure we can only remove tempfiles for now
            if (File.Exists(path) && path.Contains("temp"))
            {
                try
                {
                    File.Delete(path);
                    var deleteData = new
                    {
                        deleted  = true,
                        message = "File removed."
                    };
                    _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, deleteData);
                }
                catch (Exception e)
                {

                    var deleteDataException = new
                    {
                        deleted = false,
                        message = e.Message
                    };
                    _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, deleteDataException);
                }
            }
            else
            {

                var deleteData = new
                {
                    deleted = false,
                    message = "File does not exist or cannot be deleted"
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, deleteData);
            }
        }

        public void ProcessFile(string path, long totalSize)
        {
            System.IO.FileInfo file = new System.IO.FileInfo(path);
            file.Directory.Create(); // If the directory already exists, this method does nothing.
            var fileName = Path.GetFileName(path);
            var settings = new Settings();
            var webPath = settings.Read("WebServer", "WebFilePath", HttpServer.defaultPath);
            var ip = NetworkUtilities.GetIPv4Address();
            var httpPort = HttpServer.GlobalPort;
            var data = File.ReadAllBytes(path);
            var encryptedFile = _serializator.SerializeFile(_client, data);
            try
            {
                if (encryptedFile != null)
                {
                    var tempPath = Path.Combine(webPath + "temp\\", fileName);
                    File.WriteAllBytes(tempPath, encryptedFile);
                    var tempWebPath = $"http://{ip}:{httpPort}/temp/{fileName}";
                    var downloadData = new
                    {
                        tempWebPath,
                        totalSize
                    };
                    _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, downloadData);
                }
                else
                {
                    var errorData = new
                    {
                        error = true,
                        message = "Unable to encrypt file"
                    };
                    _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, errorData);
                }

            }
            catch (Exception e)
            {

                var exceptionData = new
                {
                    error = true,
                    message = e.Message
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, exceptionData);
            }
        }


        public void AddData()
        {
            var sha = _packet.Args.First().ToString();
            try
            {
                if (FileManager.CurrentState(sha) == FileState.Uploading)
                {
                    var dataArray = JsonConvert.DeserializeObject(_packet.Args[1].ToString(), typeof (sbyte[]));
                    var unsigned = (byte[]) (Array) dataArray;
                    FileManager.AddData(sha, unsigned);

                    var receivedData = FileManager.GetTotalRead(sha);
                    var totalSize = FileManager.GetTotalSize(sha);
                    if (receivedData >= totalSize)
                    {
                        var complete = FileManager.Complete(sha);
                        if (complete)
                        {
                            var completeData = new
                            {
                                receivedData,
                                totalSize,
                                message = "Upload Complete."
                            };
                            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, completeData);
                        }
                        else
                        {
                            var issueData = new
                            {
                                error = true,
                                receivedData,
                                totalSize,
                                message = "Upload Completed, but unable to create file."
                            };
                            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, issueData);
                        }
                    }
                    else
                    {
                        var uploadData = new
                        {
                            receivedData,
                            totalSize,
                            message = "Uploading, please wait..."
                        };
                        _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, uploadData);
                    }
                }
            }
            catch (Exception e)
            {
                var uploadError = new
                {
                    error = true,
                    message = e.Message
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, uploadError);
            }
        }

        public void StoreFile()
        {
            var filePath = _packet.Args.First().ToString();
            var totalSize = (long) _packet.Args[1];
            var sha = _packet.Args[2].ToString();
            var file = new FileInformation
            {
                FileName = filePath,
                TotalSize = totalSize,
                State = FileState.Uploading
            };
            var added = FileManager.AddFile(sha, file);
            if (added)
            {
                var storeData = new
                {
                    stored = true,
                    message = "File information set"
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, storeData);
            }
            else
            {
                var storeData = new
                {
                    stored = false,
                    message = "Unable to store file information"
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, storeData);
            }
        }
    }
}