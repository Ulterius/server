using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using UlteriusPlugins;

namespace UlteriusServer.Plugins
{
    public class PluginManager
    {
        public static Dictionary<string, IPlugin> _Plugins;
        private static List<string> BadPlugins = new List<string>();

        public static object StartPlugin(string guid, List<object> args = null)
        {
            var plugin = _Plugins[guid];
            return args != null ? plugin.Start(args) : plugin.Start();
        }

        public static int GetTotalPlugins()
        {
            return _Plugins.Count;
        }

        public static List<string> GetBadPluginsList()
        {
            return BadPlugins;
        }


        public static void LoadPlugins()
        {
            _Plugins = new Dictionary<string, IPlugin>();
            var plugins = PluginLoader<IPlugin>.LoadPlugins();
            if (plugins != null)
            {
                BadPlugins.AddRange(PluginLoader<IPlugin>.brokenPlugins);
                foreach (var plugin in plugins)
                {
                    _Plugins.Add(plugin.GUID.ToString(), plugin);
                }
            }
           
        }
    }
}