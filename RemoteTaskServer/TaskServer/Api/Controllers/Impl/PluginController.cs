#region

using System.Collections.Generic;
using System.Linq;
using UlteriusServer.Plugins;
using UlteriusServer.TaskServer.Api.Serialization;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer.Api.Controllers.Impl
{
    public class PluginController : ApiController
    {
        private readonly WebSocket _client;
        private readonly Packets packet;
        private readonly ApiSerializator serializator = new ApiSerializator();

        public PluginController(WebSocket client, Packets packet)
        {
            _client = client;
            this.packet = packet;
        }

        public void ListPlugins()
        {
            var plugins = (from plugin in PluginHandler._Plugins
                let pluginPerm = PluginHandler._PluginPermissions[plugin.Value.GUID.ToString()]
                select new
                {
                    plugin.Value.CanonicalName, GUID = plugin.Value.GUID.ToString(), plugin.Value.Name, plugin.Value.Author, plugin.Value.Description, plugin.Value.Website, plugin.Value.Icon, plugin.Value.Version, plugin.Value.Javascript, pluginPermissions = pluginPerm
                }).Cast<object>().ToList();
            serializator.Serialize(_client, packet.endpoint, packet.syncKey, plugins);
        }

        public void ListBadPlugins()
        {
            var badPlugins = (from plugin in PluginHandler.GetBadPluginsList()
                select plugin.Split('|')
                into pluginInfo
                let pluginName = pluginInfo[0]
                let pluginError = pluginInfo[1]
                select new
                {
                    pluginName, pluginError
                }).Cast<object>().ToList();
            serializator.Serialize(_client, packet.endpoint, packet.syncKey, badPlugins);
        }

        public void StartPlugin()
        {
            if (PluginHandler.GetTotalPlugins() <= 0)
            {
                var pluginError = new
                {
                    noPluginsLoaded = true
                };
                serializator.Serialize(_client, packet.endpoint, packet.syncKey, pluginError);
                return;
            }
            //GUID should always be the first argument
            var guid = packet.args?.First().ToString();
            if (guid == null)
            {
                var pluginError = new
                {
                    missingGuid = true
                };
                serializator.Serialize(_client, packet.endpoint, packet.syncKey, pluginError);
                return;
            }
            object returnData = null;
            var pluginStarted = false;
            //lets check if we have any other arguments to clean the list
            if (packet.args.Count > 1)
            {
                var cleanedArgs = packet.args;
                //remove guid from the arguments.
                cleanedArgs.RemoveAt(0);
                returnData = PluginHandler.StartPlugin(guid, cleanedArgs);
                pluginStarted = true;
            }
            else
            {
                returnData = PluginHandler.StartPlugin(guid);
                pluginStarted = true;
            }
            var pluginResponse = new
            {
                guid,
                pluginData = returnData,
                pluginStarted
            };
            serializator.Serialize(_client, packet.endpoint, packet.syncKey, pluginResponse);
        }
    }
}