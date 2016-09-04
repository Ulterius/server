#region

using System.Collections.Concurrent;
using System.Threading.Tasks;
using UlteriusServer.Api.Services.ScreenShare;
using UlteriusServer.Api.Win32.WindowsInput;
using UlteriusServer.WebSocketAPI.Authentication;

#endregion

namespace UlteriusServer.Api.Services.LocalSystem
{
    public class ScreenShareService
    {
        public readonly InputSimulator Simulator = new InputSimulator();
        public static ConcurrentDictionary<AuthClient, ScreenModel> Streams { get; set; }
        public ScreenShareService()
        {
            Streams = new ConcurrentDictionary<AuthClient, ScreenModel>();
        }
    }
}