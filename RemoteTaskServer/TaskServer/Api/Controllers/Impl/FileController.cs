#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using RemoteTaskServer.WebServer;
using UlteriusServer.TaskServer.Api.Serialization;
using UlteriusServer.TaskServer.Services.Network;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Files;
using vtortola.WebSockets;
using File = System.IO.File;

#endregion

namespace UlteriusServer.TaskServer.Api.Controllers.Impl
{
    public class FileController : ApiController
    {
        private const int EVERYTHING_OK = 0;
        private const int EVERYTHING_ERROR_MEMORY = 1;
        private const int EVERYTHING_ERROR_IPC = 2;
        private const int EVERYTHING_ERROR_REGISTERCLASSEX = 3;
        private const int EVERYTHING_ERROR_CREATEWINDOW = 4;
        private const int EVERYTHING_ERROR_CREATETHREAD = 5;
        private const int EVERYTHING_ERROR_INVALIDINDEX = 6;
        private const int EVERYTHING_ERROR_INVALIDCALL = 7;

        private readonly WebSocket _client;
        private readonly Packets _packet;
        private readonly ApiSerializator _serializator = new ApiSerializator();


        public FileController(WebSocket client, Packets packet)
        {
            _client = client;
            _packet = packet;
            //only do this operation on search request
            if (_packet.PacketType == PacketType.SearchFiles)
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
        }

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
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, tree);
        }


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

        public void SearchFile()
        {
            if (Process.GetProcessesByName("Everything").Length > 0)
            {
                var query = _packet.Args[0].ToString();
                var stopwatch = Stopwatch.StartNew();
                const int bufferSize = 260;
                var buffer = new StringBuilder(bufferSize);
                // set the search
                Everything_SetSearchW(query);
                //execute the query
                Everything_QueryW(true);
                var totalResults = Everything_GetNumResults();
                var searchResults = new List<string>();
                for (var index = 0; index < totalResults; index++)
                {
                    Everything_GetResultFullPathNameW(index, buffer, bufferSize);
                    var filePath = buffer.ToString();
                    searchResults.Add(filePath);
                }
                stopwatch.Stop();
                var searchGenerationTime = stopwatch.ElapsedMilliseconds;
                var data = new
                {
                    success = true,
                    searchGenerationTime,
                    totalResults,
                    searchResults
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
            }
            else
            {
                var error = new
                {
                    success = false,
                    message = "Everything is not running."
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, error);
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
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
            }
        }

        public void RemoveFile()
        {
            var path = _packet.Args[0].ToString();

            //make sure we can only remove tempfiles for now
            if (File.Exists(path) && path.Contains("temp"))
            {
                try
                {
                    File.Delete(path);
                    var deleteData = new
                    {
                        deleted = true,
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
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, approved);
        }


        public void ProcessFile(string path, string password, long totalSize)
        {
            var settings = new Settings();
            var webPath = settings.Read("WebServer", "WebFilePath", HttpServer.defaultPath);

            var file = new FileInfo(path);
            file.Directory?.Create(); // If the directory already exists, this method does nothing.


            var fileName = Path.GetFileName(path);

            var ip = NetworkUtilities.GetIPv4Address();
            var httpPort = HttpServer.GlobalPort;
            var data = File.ReadAllBytes(path);

            var encryptedFile = _serializator.SerializeFile(_client, password, data);
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
            catch (
                Exception e)
            {
                var exceptionData = new
                {
                    error = true,
                    message = e.Message
                };

                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, exceptionData);
            }
        }

        #region everything

        [DllImport("Everything.dll", CharSet = CharSet.Unicode)]
        public static extern int Everything_SetSearchW(string lpSearchString);

        [DllImport("Everything.dll")]
        public static extern void Everything_SetMatchPath(bool bEnable);

        [DllImport("Everything.dll")]
        public static extern void Everything_SetMatchCase(bool bEnable);

        [DllImport("Everything.dll")]
        public static extern void Everything_SetMatchWholeWord(bool bEnable);

        [DllImport("Everything.dll")]
        public static extern void Everything_SetRegex(bool bEnable);

        [DllImport("Everything.dll")]
        public static extern void Everything_SetMax(int dwMax);

        [DllImport("Everything.dll")]
        public static extern void Everything_SetOffset(int dwOffset);

        [DllImport("Everything.dll")]
        public static extern bool Everything_GetMatchPath();

        [DllImport("Everything.dll")]
        public static extern bool Everything_GetMatchCase();

        [DllImport("Everything.dll")]
        public static extern bool Everything_GetMatchWholeWord();

        [DllImport("Everything.dll")]
        public static extern bool Everything_GetRegex();

        [DllImport("Everything32.dll")]
        public static extern uint Everything_GetMax();

        [DllImport("Everything.dll")]
        public static extern uint Everything_GetOffset();

        [DllImport("Everything.dll")]
        public static extern string Everything_GetSearchW();

        [DllImport("Everything.dll")]
        public static extern int Everything_GetLastError();

        [DllImport("Everything.dll")]
        public static extern bool Everything_QueryW(bool bWait);

        [DllImport("Everything.dll")]
        public static extern void Everything_SortResultsByPath();

        [DllImport("Everything.dll")]
        public static extern int Everything_GetNumFileResults();

        [DllImport("Everything.dll")]
        public static extern int Everything_GetNumFolderResults();

        [DllImport("Everything.dll")]
        public static extern int Everything_GetNumResults();

        [DllImport("Everything.dll")]
        public static extern int Everything_GetTotFileResults();

        [DllImport("Everything.dll")]
        public static extern int Everything_GetTotFolderResults();

        [DllImport("Everything.dll")]
        public static extern int Everything_GetTotResults();

        [DllImport("Everything.dll")]
        public static extern bool Everything_IsVolumeResult(int nIndex);

        [DllImport("Everything.dll")]
        public static extern bool Everything_IsFolderResult(int nIndex);

        [DllImport("Everything.dll")]
        public static extern bool Everything_IsFileResult(int nIndex);

        [DllImport("Everything.dll", CharSet = CharSet.Unicode)]
        public static extern void Everything_GetResultFullPathNameW(int nIndex, StringBuilder lpString, int nMaxCount);

        [DllImport("Everything.dll")]
        public static extern void Everything_Reset();

        #endregion
    }
}