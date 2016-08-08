#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UlteriusServer.Api.Network.Messages;
using UlteriusServer.Api.Services.Network;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Files;
using UlteriusServer.WebSocketAPI.Authentication;
using static UlteriusServer.Api.Network.PacketManager;
using File = System.IO.File;

#endregion

namespace UlteriusServer.Api.Network.PacketHandlers
{
    public class FilePacketHandler : PacketHandler
    {
        private const int EVERYTHING_OK = 0;
        private const int EVERYTHING_ERROR_MEMORY = 1;
        private const int EVERYTHING_ERROR_IPC = 2;
        private const int EVERYTHING_ERROR_REGISTERCLASSEX = 3;
        private const int EVERYTHING_ERROR_CREATEWINDOW = 4;
        private const int EVERYTHING_ERROR_CREATETHREAD = 5;
        private const int EVERYTHING_ERROR_INVALIDINDEX = 6;
        private const int EVERYTHING_ERROR_INVALIDCALL = 7;

        private MessageBuilder _builder;
        private AuthClient _client;
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

        /*   public IEnumerable<string> Search(string keyWord)
        {
            return Search(keyWord, 0, int.MaxValue);
        }*/

        public long DirSize(DirectoryInfo d)
        {
            // Add file sizes.
            var fis = d.GetFiles();
            var size = fis.Sum(fi => fi.Length);
            // Add subdirectory sizes.
            var dis = d.GetDirectories();
            size += dis.Sum(di => DirSize(di));
            return size;
        }

        /*  public void EverythingReset()
        {
            Everything_Reset();
        }

        public IEnumerable<string> Search(string keyWord, int offset, int maxCount)
        {
            if (string.IsNullOrEmpty(keyWord))
                throw new ArgumentNullException("keyWord");

            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset");

            if (maxCount < 0)
                throw new ArgumentOutOfRangeException("maxCount");

            Everything_SetSearch(keyWord);
            Everything_SetOffset(offset);
            Everything_SetMax(maxCount);
            if (!Everything_Query())
            {
                switch (Everything_GetLastError())
                {
                    case StateCode.CreateThreadError:
                        throw new CreateThreadException();
                    case StateCode.CreateWindowError:
                        throw new CreateWindowException();
                    case StateCode.InvalidCallError:
                        throw new InvalidCallException();
                    case StateCode.InvalidIndexError:
                        throw new InvalidIndexException();
                    case StateCode.IPCError:
                        throw new IPCErrorException();
                    case StateCode.MemoryError:
                        throw new MemoryErrorException();
                    case StateCode.RegisterClassExError:
                        throw new RegisterClassExException();
                }
                yield break;
            }

            const int bufferSize = 256;
            var buffer = new StringBuilder(bufferSize);
            for (var idx = 0; idx < Everything_GetNumResults(); ++idx)
            {
                Everything_GetResultFullPathName(idx, buffer, bufferSize);
                yield return buffer.ToString();
            }
        }*/

        private List<string> Search(string keyword)
        {
            Console.WriteLine(keyword);
            return UlteriusApiServer.FileSearchService.Search(keyword);
        }

        public void SearchFile()
        {
            /*try
            {
                ConfigureSearch();
            }
            catch (Exception)
            {
                var error = new
                {
                    success = false,
                    message = "Everything failed to configure."
                };
                _builder.WriteMessage(error);
                return;
            }*/
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

        private void ConfigureSearch()
        {
            var startupDirEndingWithSlash = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) +
                                            "\\";
            var resolvedDomainTimeFileName = startupDirEndingWithSlash + "Everything.dll";
            if (!File.Exists(resolvedDomainTimeFileName))
            {
                if (IntPtr.Size == 8)
                {
                    Console.WriteLine(@"x64 Everything Loaded");
                    if (File.Exists(startupDirEndingWithSlash + "Everything64.dll"))

                        File.Copy(startupDirEndingWithSlash + "Everything64.dll", resolvedDomainTimeFileName);
                }
                else
                {
                    Console.WriteLine(@"x86 Everything Loaded");
                    if (File.Exists(startupDirEndingWithSlash + "Everything32.dll"))
                        File.Copy(startupDirEndingWithSlash + "Everything32.dll", resolvedDomainTimeFileName);
                }
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
            var password = _packet.Args[2].ToString();
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

            var ip = NetworkService.GetIPv4Address();
            var port = (int) Settings.Get("WebServer").WebServerPort;
            var data = File.ReadAllBytes(path);

            var encryptedFile = _builder.PackFile(password, data);
            try
            {
                if (encryptedFile != null)
                {
                    var tempPath = Path.Combine(tempFolderPath, fileName);

                    File.WriteAllBytes(tempPath, encryptedFile);
                    var tempWebPath = $"http://{ip}:{port}/temp/{fileName}";
                    var downloadData = new
                    {
                        tempWebPath,
                        totalSize
                    };
                    _builder.WriteMessage(downloadData);
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
            catch (
                Exception e)
            {
                var exceptionData = new
                {
                    error = true,
                    message = e.Message
                };

                _builder.WriteMessage(exceptionData);
            }
        }

        public override void HandlePacket(Packet packet)
        {
            _client = packet.AuthClient;
            _packet = packet;
            _builder = new MessageBuilder(_client, _packet.EndPoint, _packet.SyncKey);
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

        /// <summary>
        ///     ///
        /// </summary>
        public class MemoryErrorException : ApplicationException
        {
        }

        /// <summary>
        ///     ///
        /// </summary>
        public class IPCErrorException : ApplicationException
        {
        }

        /// <summary>
        ///     ///
        /// </summary>
        public class RegisterClassExException : ApplicationException
        {
        }

        /// <summary>
        ///     ///
        /// </summary>
        public class CreateWindowException : ApplicationException
        {
        }

        /// <summary>
        ///     ///
        /// </summary>
        public class CreateThreadException : ApplicationException
        {
        }

        /// <summary>
        ///     ///
        /// </summary>
        public class InvalidIndexException : ApplicationException
        {
        }

        /// <summary>
        ///     ///
        /// </summary>
        public class InvalidCallException : ApplicationException
        {
        }

        private enum StateCode
        {
            OK,
            MemoryError,
            IPCError,
            RegisterClassExError,
            CreateWindowError,
            CreateThreadError,
            InvalidIndexError,
            InvalidCallError
        }
    }
}