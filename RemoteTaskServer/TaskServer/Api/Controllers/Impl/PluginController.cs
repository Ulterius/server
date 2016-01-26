#region

using System;
using System.Linq;
using System.Management;
using UlteriusServer.Plugins;
using UlteriusServer.TaskServer.Api.Models;
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
            this._client = client;
            this.packet = packet;
        }

        public void ListPlugins()
        {
            serializator.Serialize(_client, packet.endpoint, packet.syncKey, PluginManager._Plugins);
        }
        public void ListBadPlugins()
        {
            serializator.Serialize(_client, packet.endpoint, packet.syncKey, PluginManager.GetBadPluginsList());
        }

        public void StartPlugin()
        {
            if (PluginManager.GetTotalPlugins() <= 0)
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
                returnData = PluginManager.StartPlugin(guid, cleanedArgs);
                pluginStarted = true;
            }
            else
            {
                returnData = PluginManager.StartPlugin(guid);
                pluginStarted = true;
            }
            var pluginResponse = new
            {
                pluginData = returnData,
                pluginStarted,
            };
            serializator.Serialize(_client, packet.endpoint, packet.syncKey, pluginResponse);
        }
    }
}