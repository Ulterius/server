#region

using System.Linq;
using UlteriusServer.Authentication;
using UlteriusServer.Plugins;
using UlteriusServer.TaskServer.Network.Messages;

#endregion

namespace UlteriusServer.TaskServer.Network.PacketHandlers
{
    public class PluginPacketHandler : PacketHandler
    {
        private PacketBuilder _builder;
        private AuthClient _client;
        private Packet _packet;


        public void GetPlugins()
        {
            var plugins = (from plugin in PluginHandler.Plugins
                let pluginPerm = PluginHandler.PluginPermissions[plugin.Value.GUID.ToString()]
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
            _builder.WriteMessage(plugins);
        }

        public void ApprovePlugin()
        {
            /*var guid = _packet.Args?.First().ToString();
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
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, pluginApproveResponse);*/
        }

        public void GetPendingPlugins()
        {
            var pendingPlugins = PluginHandler.PendingPlugins.ToList();
            var pendingList = (from pendingPlugin in pendingPlugins
                let plugin = PluginHandler.Plugins[pendingPlugin.Value]
                let Name = plugin.Name
                let Description = plugin.Description
                let Version = plugin.Version
                let Author = plugin.Author
                let Guid = plugin.GUID.ToString()
                let CanonicalName = plugin.CanonicalName
                let Icon = plugin.Icon
                let Website = plugin.Website
                let Permissions = PluginHandler.PluginPermissions[pendingPlugin.Value]
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
            _builder.WriteMessage(pendingList);
        }

        public void GetBadPlugins()
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
            _builder.WriteMessage(badPlugins);
        }

        public void StartPlugin()
        {
            if (PluginHandler.GetTotalPlugins() <= 0)
            {
                var pluginError = new
                {
                    noPluginsLoaded = true
                };
                _builder.WriteMessage(pluginError);
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
                _builder.WriteMessage(pluginError);
                return;
            }
            /*   if (!PluginHandler.ApprovedPlugins.ContainsValue(guid))
            {
                var pluginError = new
                {
                    notApproved = true,
                    guid
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, pluginError);
                return;
            }*/
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
            _builder.WriteMessage(pluginResponse);
        }

        public override void HandlePacket(Packet packet)
        {
            _client = packet.AuthClient;
            _packet = packet;
            _builder = new PacketBuilder(_client, _packet.EndPoint, _packet.SyncKey);
            switch (_packet.PacketType)
            {
                case PacketManager.PacketTypes.Plugin:
                    StartPlugin();
                    break;
                case PacketManager.PacketTypes.ApprovePlugin:
                    ApprovePlugin();
                    break;
                case PacketManager.PacketTypes.GetPendingPlugins:
                    GetPendingPlugins();
                    break;
                case PacketManager.PacketTypes.GetPlugins:
                    GetPlugins();
                    break;
                case PacketManager.PacketTypes.GetBadPlugins:
                    GetBadPlugins();
                    break;
            }
        }
    }
}