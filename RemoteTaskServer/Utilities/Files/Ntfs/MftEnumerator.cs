#region

using System.Collections;
using System.Collections.Generic;
using System.IO;

#endregion

namespace UlteriusServer.Utilities.Files.Ntfs
{
    public class MftEnumerator : IEnumerable<string>
    {
        private readonly string _volume;
        private Dictionary<ulong, FileEntry> _files;
        private Dictionary<ulong, FileEntry> _folders;

        public MftEnumerator(string volume) //volume is "c:" format (no quotes)
        {
            _volume = volume;
        }

        public int Count
        {
            get
            {
                Init();
                return _files.Values.Count;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<string> GetEnumerator()
        {
            Init();
            var path = new List<string>(); //path fragment collector
            foreach (var f in _files.Values)
            {
                path.Clear();
                var p = f;
                var dp = -1; //max path length counter to avoid inifinte loops

                do
                {
                    if (p.ParentFrn != 0)
                    {
                        path.Add(p.Name);
                    }
                    if (_files.ContainsKey(p.ParentFrn))
                    {
                        p = _files[p.ParentFrn];
                    }
                    else if (_folders.ContainsKey(p.ParentFrn))
                    {
                        p = _folders[p.ParentFrn];
                    }
                    else
                    {
                        p = null;
                    }
                } while (p != null && ++dp < 1000);

                if (path.Count != 0)
                {
                    path.Reverse();
                    var file = _volume + '\\' + Path.Combine(path.ToArray());
                    yield return file;
                }
            }
        }

        private void Init()
        {
            if (_files != null)
            {
                return;
            }
            var ntfs = new Volume();
            ntfs.EnumerateVolume(_volume, null, out _files, out _folders);
        }
    }
}