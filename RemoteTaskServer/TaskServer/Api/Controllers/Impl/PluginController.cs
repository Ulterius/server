#region

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
        private readonly Packets _packet;
        private readonly ApiSerializator _serializator = new ApiSerializator();

        public PluginController(WebSocket client, Packets packet)
        {
            _client = client;
            _packet = packet;
        }

        public void ListPlugins()
        {
            var plugins = (from plugin in PluginHandler._Plugins
                let pluginPerm = PluginHandler._PluginPermissions[plugin.Value.GUID.ToString()]
                select new
                {
                    plugin.Value.CanonicalName,
                    GUID = plugin.Value.GUID.ToString(),
                    plugin.Value.Name,
                    plugin.Value.Author,
                    plugin.Value.Description,
                    plugin.Value.Website,
                    plugin.Value.Icon,
                    plugin.Value.Version,
                    plugin.Value.Javascript,
                    Permissions = pluginPerm
                }).Cast<object>().ToList();
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, plugins);
        }

        public void ApprovePlugin()
        {
            var guid = _packet.Args?.First().ToString();
            if (guid == null)
            {
                var pluginError = new
                {
                    missingGuid = true
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, pluginError);
                return;
            }
            var pluginApproved = PluginPermissions.ApprovePlugin(guid);
            var pluginApproveResponse = new
            {
                pluginApproved,
                guid,
                setupRan = true
            };
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, pluginApproveResponse);
        }

        public void GetPendingPlugins()
        {
            var pendingPlugins = PluginHandler._PendingPlugins.ToList();
            var pendingList = (from pendingPlugin in pendingPlugins
                let plugin = PluginHandler._Plugins[pendingPlugin.Value]
                let Name = plugin.Name
                let Description = plugin.Description
                let Version = plugin.Version
                let Author = plugin.Author
                let Guid = plugin.GUID.ToString()
                let CanonicalName = plugin.CanonicalName
                let Icon = plugin.Icon
                let Website = plugin.Website
                let Permissions = PluginHandler._PluginPermissions[pendingPlugin.Value]
                select new
                {
                    Guid,
                    Name,
                    Description,
                    Version,
                    Author,
                    CanonicalName,
                    Icon,
                    Permissions,
                    Website
                }).Cast<object>().ToList();
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, pendingList);
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
                    pluginName,
                    pluginError
                }).Cast<object>().ToList();
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, badPlugins);
        }

        public void StartPlugin()
        {
            if (PluginHandler.GetTotalPlugins() <= 0)
            {
                var pluginError = new
                {
                    noPluginsLoaded = true
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, pluginError);
                return;
            }
            //GUID should always be the first argument
            var guid = _packet.Args?.First().ToString();
            if (guid == null)
            {
                var pluginError = new
                {
                    missingGuid = true
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, pluginError);
                return;
            }
            if (!PluginHandler._ApprovedPlugins.ContainsValue(guid))
            {
                var pluginError = new
                {
                    notApproved = true,
                    guid
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, pluginError);
                return;
            }
            object returnData = null;
            var pluginStarted = false;
            //lets check if we have any other arguments to clean the list
            if (_packet.Args.Count > 1)
            {
                var cleanedArgs = _packet.Args;
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
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, pluginResponse);
        }
    }
}