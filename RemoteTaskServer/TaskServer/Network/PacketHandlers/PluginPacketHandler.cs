#region

using System.Linq;
using UlteriusServer.Plugins;
using UlteriusServer.TaskServer.Network.Messages;
using UlteriusServer.WebSocketAPI.Authentication;

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
            var plugins = PluginLoader.Plugins.Select(plugin => plugin.Value).ToList();
            _builder.WriteMessage(plugins);
        }


        public void GetBadPlugins()
        {
            _builder.WriteMessage(PluginLoader.BrokenPlugins);
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
            object returnData = null;
            var pluginStarted = false;
            //lets check if we have any other arguments to clean the list
            if (_packet.Args.Count > 1)
            {
                var cleanedArgs = _packet.Args;
                //remove guid from the arguments.
                cleanedArgs.RemoveAt(0);
                returnData = PluginHandler.StartPlugin(_client.Client, guid, cleanedArgs);
                pluginStarted = true;
            }
            else
            {
                returnData = PluginHandler.StartPlugin(_client.Client, guid);
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
                    //  ApprovePlugin();
                    break;
                case PacketManager.PacketTypes.GetPendingPlugins:
                    // GetPendingPlugins();
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