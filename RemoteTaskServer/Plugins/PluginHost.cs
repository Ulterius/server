using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using UlteriusPluginBase;

namespace UlteriusServer.Plugins
{
    public class PluginHost
    {
        public string Guid;
        public string Title;
        public string Description;
        public string Company;
        public string Product;
        public string Copyright;
        public string Trademark;
        public string FileVersion;
        public string Website;
        public string Icon;
        public string JavaScript;
        public bool RequiresSetup;
        [ScriptIgnore]
        public IUlteriusPlugin Instance;
    }
}
