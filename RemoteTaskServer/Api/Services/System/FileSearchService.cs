#region

using System.Collections.Generic;
using System.Threading.Tasks;
using UlteriusFileSearch;

#endregion

namespace UlteriusServer.Api.Services.System
{
    public class FileSearchService
    {
        private readonly string _path;
        private SearchService fileSearch;

        public FileSearchService(string path)
        {
            _path = path;
        }

        public string CurrentScanDrive()
        {
            return fileSearch.CurrentScanDrive();
        }

        public bool IsScanning()
        {
            return fileSearch.IsScanning();
        }

        public List<string> Search(string keyword)
        {
            return fileSearch.Search(keyword);
        }

        public void Start()
        {
            fileSearch = new SearchService(_path);
            Task.Run(() => { fileSearch.Configure(); });
        }
    }
}