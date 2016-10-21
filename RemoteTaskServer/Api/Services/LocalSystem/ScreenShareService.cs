#region

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UlteriusServer.Api.Services.ScreenShare;
using UlteriusServer.Api.Win32;
using UlteriusServer.Api.Win32.WindowsInput;
using UlteriusServer.Utilities;
using UlteriusServer.WebSocketAPI.Authentication;

#endregion

namespace UlteriusServer.Api.Services.LocalSystem
{
    public class ScreenShareService
    {
        public static string ClipboardText = string.Empty;
        public readonly InputSimulator Simulator = new InputSimulator();
    
        public static ConcurrentDictionary<AuthClient, Thread> Streams { get; set; }
        public ScreenShareService()
        {
            Streams = new ConcurrentDictionary<AuthClient, Thread>();
            if (Tools.RunningPlatform() == Tools.Platform.Windows)
            {
                var win8Version = new Version(6, 2, 9200, 0);

                if (Environment.OSVersion.Platform == PlatformID.Win32NT &&
                    Environment.OSVersion.Version >= win8Version)
                {
                    
                }
                ClipboardNotifications.ClipboardUpdate += HandleClipboard;
            }
        }

        private void HandleClipboard(object sender, EventArgs e)
        {
            try
            {
                var clipboard = WinApi.GetText();
                if (clipboard == null)
                {
                    clipboard = string.Empty;
                    return;
                }
                if (clipboard.Length > 5242880)
                {
                    clipboard = string.Empty;
                    return;
                }
                if (ClipboardText.Equals(clipboard))
                {
                    return;
                }
                ClipboardText = clipboard;
            }
            catch (Exception)
            {
                //Nothing to be done
            }
        }
    }
}