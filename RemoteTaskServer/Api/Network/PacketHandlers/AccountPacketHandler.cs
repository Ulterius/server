#region

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UlteriusServer.Api.Network.Messages;
using UlteriusServer.Utilities;
using UlteriusServer.WebSocketAPI.Authentication;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.Api.Network.PacketHandlers
{
    public class AccountPacketHandler : PacketHandler
    {
        private AuthClient _authClient;
        private MessageBuilder _builder;
        private WebSocket _client;
        private Packet _packet;


        public static string GetUserTilePath(string username)
        {   // username: use null for current user
            var sb = new StringBuilder(1000);
            GetUserTilePath(username, 0x80000000, sb, sb.Capacity);
            return sb.ToString();
        }

        private Image GetUserTile(string username)
        {
            return Image.FromFile(GetUserTilePath(username));
        }

        private string GetWindowsAvatar()
        {
            var avatar = GetUserTile(GetUsername());

            return ImagetoBase64(avatar);
        }


        private static string ImagetoBase64(Image image)
        {
            var ms = new MemoryStream();
            image.Save(ms, ImageFormat.Png);
            var arr = new byte[ms.Length];
            ms.Position = 0;
            ms.Read(arr, 0, (int) ms.Length);
            ms.Close();
            var strBase64 = Convert.ToBase64String(arr);
            return strBase64;
        }

    

        private string GetUserAvatar()
        {
           return  GetWindowsAvatar();
        }

        private string GetUsername() { 
        
            var registryName = AppEnvironment.Setting("LastUsername");
            if (!string.IsNullOrEmpty(registryName))
            {
                return registryName;
            }
            var envName = Environment.UserName;
            //cheap work around
            if (envName.Equals("SYSTEM") && Tools.RunningPlatform() == Tools.Platform.Windows)
            {
                envName = Tools.GetUsernameAsService();
            }
            return envName;
        }

        public void GetAccountData()
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
            _builder = new MessageBuilder(_authClient, _client, _packet.EndPointName, _packet.SyncKey);
            switch (_packet.EndPoint)
            {
                case PacketManager.EndPoints.GetWindowsData:
                    GetAccountData();
                    break;
            }
        }
    }
}