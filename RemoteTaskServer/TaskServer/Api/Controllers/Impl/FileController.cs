#region

using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UlteriusServer.TaskServer.Api.Models;
using UlteriusServer.TaskServer.Api.Serialization;
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
        private readonly Packets packet;
        private readonly ApiSerializator serializator = new ApiSerializator();


        public FileController(WebSocket client, Packets packet)
        {
            _client = client;
            this.packet = packet;
        }

        public void CreateFileTree()
        {
            var argumentSize = packet.args.Count;
            var path = packet.args.First().ToString();
            var deepWalk = false;
            if (argumentSize > 1)
            {
                deepWalk = (bool) packet.args[1];
            }
            var tree = new FileTree(path, deepWalk);
            serializator.Serialize(_client, packet.endpoint, packet.syncKey, tree);
        }

        public void SearchFile()
        {
        }


        public void RequestFile()
        {
            var path = packet.args.First().ToString();
            if (File.Exists(path))
            {
                var fileName = Path.GetFileName(path);
                var totalSize = new FileInfo(path).Length;
                var data = new
                {
                    path,
                    fileValid = true,
                    fileName,
                    size = totalSize
                };
                serializator.Serialize(_client, packet.endpoint, packet.syncKey, data);
                ProcessFile(path, totalSize);
            }
            else
            {
                var data = new
                {
                    path,
                    fileValid = false
                };
                serializator.Serialize(_client, packet.endpoint, packet.syncKey, data);
            }
        }

        public void ProcessFile(string path, long totalSize)
        {
            const int chunkSize = 1000000; // read the file by chunks of 1mb
            using (var file = File.OpenRead(path))
            {
                int bytesRead;
                var buffer = new byte[chunkSize];
                while ((bytesRead = file.Read(buffer, 0, buffer.Length)) > 0)
                {
                    // TODO: Process bytesRead number of bytes from the buffer
                    // not the entire buffer as the size of the buffer is 1KB
                    // whereas the actual number of bytes that are read are 
                    // stored in the bytesRead integer.
                    using (var memory = new MemoryStream())
                    {
                        using (var writer = new BinaryWriter(memory))
                        {
                            writer.Write(buffer, 0, bytesRead);
                            var data = new
                            {
                                path,
                                totalSize,
                                complete = false,
                                fileData = memory.ToArray()
                            };
                            serializator.Serialize(_client, "downloaddata", packet.syncKey, data);
                        }
                    }
                }
                var finalData = new
                {
                    path,
                    totalSize,
                    complete = true
                };
                serializator.Serialize(_client, packet.endpoint, packet.syncKey, finalData);
            }
        }


        public void AddData()
        {
            var sha = packet.args.First().ToString();
            try
            {
                if (FileManager.CurrentState(sha) == FileState.Uploading)
                {
                    var dataArray = JsonConvert.DeserializeObject(packet.args[1].ToString(), typeof (sbyte[]));
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
                            serializator.Serialize(_client, packet.endpoint, packet.syncKey, completeData);
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
                            serializator.Serialize(_client, packet.endpoint, packet.syncKey, issueData);
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
                        serializator.Serialize(_client, packet.endpoint, packet.syncKey, uploadData);
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
                serializator.Serialize(_client, packet.endpoint, packet.syncKey, uploadError);
            }
        }

        public void StoreFile()
        {
            var filePath = packet.args.First().ToString();
            var totalSize = (long) packet.args[1];
            var sha = packet.args[2].ToString();
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
                serializator.Serialize(_client, packet.endpoint, packet.syncKey, storeData);
            }
            else
            {
                var storeData = new
                {
                    stored = false,
                    message = "Unable to store file information"
                };
                serializator.Serialize(_client, packet.endpoint, packet.syncKey, storeData);
            }
        }
    }
}