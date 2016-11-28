#region

using System.Collections.Concurrent;
using System.Threading;

using UlteriusServer.WebSocketAPI.Authentication;

#endregion

namespace UlteriusServer.Api.Services.LocalSystem
{
    public class ScreenShareService
    {


        public ScreenShareService()
        {
            Streams = new ConcurrentDictionary<AuthClient, Thread>();
        }


        public static ConcurrentDictionary<AuthClient, Thread> Streams { get; set; }
    }
}