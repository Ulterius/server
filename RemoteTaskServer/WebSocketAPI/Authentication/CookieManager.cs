#region

using System;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.WebSocketAPI.Authentication
{
    public static class CookieManager
    {
        public static Guid GetConnectionId(WebSocket clientSocket)
        {
            Guid connectionId;
            var cookie = clientSocket.HttpRequest.Cookies["ConnectionId"] ??
                         clientSocket.HttpResponse.Cookies["ConnectionId"];
            if (cookie == null || !Guid.TryParse(cookie.Value, out connectionId))
                return Guid.Empty;
            return connectionId;
        }
    }
}