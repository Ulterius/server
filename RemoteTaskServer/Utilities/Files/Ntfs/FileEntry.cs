#region

using System;

#endregion

namespace UlteriusServer.Utilities.Files.Ntfs
{
    internal class FileEntry
    {
        public FileEntry(string name, ulong parentFrn)
        {
            if (!string.IsNullOrEmpty(name))
            {
                Name = name;
            }
            else
            {
                throw new ArgumentException("Invalid argument: null or Length = zero", nameof(name));
            }
            ParentFrn = parentFrn;
        }

        public string Name { get; set; }

        public ulong ParentFrn { get; set; }
    }
}