#region

using System;
using System.Collections.Generic;
using System.Security;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.Plugins
{
    public class PluginHandler
    {
        public static object StartPlugin(WebSocket client, string guid, List<object> args = null)
        {
            try
            {
                var plugin = PluginLoader.Plugins[guid];
                if (args != null)
                {
                    plugin.Instance.Start(client, args);
                }
                else
                {
                    plugin.Instance.Start(client);
                }
                var pluginResponse = new
                {
                    pluginRan = true,
                    withArgs = args != null
                };
                return pluginResponse;
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
            return PluginLoader.Plugins.Count;
        }

        public static List<string> GetBadPluginsList()
        {
            return null;
        }


        public static void LoadPlugins()
        {
            PluginLoader.LoadPlugins();
            var loadedCount = PluginLoader.Plugins.Count;
            var failedCount = PluginLoader.BrokenPlugins.Count;
            Console.WriteLine($"{loadedCount} plugins loaded, {failedCount} plugins did not load.");
            foreach (var plugin in PluginLoader.Plugins)
            {
                var host = plugin.Value;
                if (host.RequiresSetup)
                {
                    host.Instance.Initialize();
                }
            }
        }
    }
}