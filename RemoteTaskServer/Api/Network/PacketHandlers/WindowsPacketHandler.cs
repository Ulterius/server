#region

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UlteriusServer.Api.Network.Messages;
using UlteriusServer.WebSocketAPI.Authentication;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.Api.Network.PacketHandlers
{
    public class WindowsPacketHandler : PacketHandler
    {
        private MessageBuilder _builder;
        private AuthClient _authClient;
        private Packet _packet;
        private WebSocket _client;


        private string GetUserTilePath(string username)
        {
            // username: use null for current user
            var sb = new StringBuilder(1000);
            GetUserTilePath(username, 0x80000000, sb, sb.Capacity);
            return sb.ToString();
        }

        private Image GetUserTile(string username)
        {
            return Image.FromFile(GetUserTilePath(username));
        }

        private string GetUserAvatar()
        {
            var avatar = GetUserTile(null);
            var ms = new MemoryStream();
            avatar.Save(ms, ImageFormat.Png);
            var arr = new byte[ms.Length];

            ms.Position = 0;
            ms.Read(arr, 0, (int) ms.Length);
            ms.Close();

            var strBase64 = Convert.ToBase64String(arr);

            return strBase64;
        }

        private string GetUsername()
        {
            return Environment.UserName;
        }

        public void GetWindowsData()
        {
            var data = new
            {
                avatar = GetUserAvatar(),
                username = GetUsername()
            };
            _builder.WriteMessage(data);
        }

        #region

        [DllImport("shell32.dll", EntryPoint = "#261",
            CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void GetUserTilePath(
            string username,
            uint whatever, // 0x80000000
            StringBuilder picpath, int maxLength);

        #endregion

        public override void HandlePacket(Packet packet)
        {
            _client = packet.Client;
            _authClient = packet.AuthClient;
            _packet = packet;
            _builder = new MessageBuilder(_authClient, _client,  _packet.EndPoint, _packet.SyncKey);
            switch (_packet.PacketType)
            {
                case PacketManager.PacketTypes.GetWindowsData:
                    GetWindowsData();
                    break;
            }
        }
    }
}