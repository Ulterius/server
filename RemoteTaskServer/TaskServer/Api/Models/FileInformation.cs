using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassTransit.Diagnostics.Introspection.Messages;

namespace UlteriusServer.TaskServer.Api.Models
{
    public class FileInformation
    {
        public enum FileState
        {
            Uploading,
            Donwloading,
            Complete
        }

        public string FileName { get; set; }
        public long TotalSize { get; set; }
        public long BytesReceived { get; set; }
        public FileState State { get; set; }
        public byte[] FileData { get; set; }
    }
}
