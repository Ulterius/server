#region

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UlteriusServer.TaskServer.Api.Serialization;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer.Api.Controllers.Impl
{
    public class WindowsController : ApiController
    {
        private readonly WebSocket _client;
        private readonly Packets _packet;
        private readonly ApiSerializator _serializator = new ApiSerializator();

        public WindowsController(WebSocket client, Packets packet)
        {
            _client = client;
            _packet = packet;
        }

        
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

        public void GetWindowsInformation()
        {
            var data = new
            {
                avatar = GetUserAvatar(),
                username = GetUsername()
            };
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
        }

        #region

        [DllImport("shell32.dll", EntryPoint = "#261",
            CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void GetUserTilePath(
            string username,
            uint whatever, // 0x80000000
            StringBuilder picpath, int maxLength);

        #endregion
    }
}