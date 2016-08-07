#region

using System.Web.Script.Serialization;
using UlteriusPluginBase;

#endregion

namespace UlteriusServer.Plugins
{
    public class PluginHost
    {
        public string Company;
        public string Copyright;
        public string Description;
        public string FileVersion;
        public string Guid;
        public string Icon;

        [ScriptIgnore] public IUlteriusPlugin Instance;

        public string JavaScript;
        public string Product;
        public bool RequiresSetup;
        public string Title;
        public string Trademark;
        public string Website;
    }
}