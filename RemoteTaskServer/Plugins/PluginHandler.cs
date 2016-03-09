using System;
using System.Collections.Generic;
using System.Security;
using UlteriusPluginBase;
using UlteriusServer.Forms.Utilities;
using UlteriusServer.Utilities;

namespace UlteriusServer.Plugins
{
    public class PluginHandler
    {
        public static Dictionary<string, PluginBase> _Plugins;
        public static Dictionary<string, List<string>> _PluginPermissions;
        private static readonly List<string> BadPlugins = new List<string>();

        public static object StartPlugin(string guid, List<object> args = null)
        {
            try
            {
                var plugin = _Plugins[guid];
                return args != null ? plugin.Start(args) : plugin.Start();
            }
            catch (SecurityException e)
            {
                var input = e.Message;
                var start = input.IndexOf("'", StringComparison.Ordinal) + 1;
                var end = input.IndexOf(",", start, StringComparison.Ordinal);
                var result = input.Substring(start, end - start);
                var message = $"Plugin attempted to invoke code that it lacks permissions for: {result}";
                var pluginResponse = new
                {
                    permissionError = true,
                    message
                };
                return pluginResponse;
            }
            catch (Exception ex)
            {
                var pluginResponse = new
                {
                    pluginError = true,
                    message = ex.Message
                };
                return pluginResponse;
            }
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
            var settings = new Settings();
            var loadPlugins = settings.Read("Plugins", "LoadPlugins", true);
            if (loadPlugins)
            {
                _Plugins = new Dictionary<string, PluginBase>();
                _PluginPermissions = new Dictionary<string, List<string>>();
                var plugins = PluginLoader.LoadPlugins();
                if (plugins != null)
                {
                    BadPlugins.AddRange(PluginLoader.BrokenPlugins);
                    foreach (var plugin in plugins)
                    {
                        //probably a better way to expose objects
                        plugin.NotificationIcon = UlteriusTray.NotificationIcon;
                        if (plugin.RequiresSetup)
                        {
                            plugin.Setup();
                        }
                        _Plugins.Add(plugin.GUID.ToString(), plugin);
                    }
                }
            }
            
        }
    }
}