#region

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using vtortola.WebSockets;
using vtortola.WebSockets.Deflate;
using vtortola.WebSockets.Rfc6455;

#endregion

namespace UlteriusServer.WebSocketAPI
{
    public delegate void WebSocketEventListenerOnConnect(WebSocket webSocket);
    public delegate void WebSocketEventListenerOnDisconnect(WebSocket webSocket);
    public delegate void WebSocketEventListenerOnMessage(WebSocket webSocket, String message);
    public delegate void WebSocketEventListenerOnError(WebSocket webSocket, Exception error);

    public class WebSocketEventListener : IDisposable
    {
        public event WebSocketEventListenerOnConnect OnConnect;
        public event WebSocketEventListenerOnDisconnect OnDisconnect;
        public event WebSocketEventListenerOnMessage OnMessage;
        public event WebSocketEventListenerOnError OnError;

        readonly WebSocketListener _listener;

        public WebSocketEventListener(IPEndPoint endpoint)
            : this(endpoint, new WebSocketListenerOptions())
        {
        }
        public WebSocketEventListener(IPEndPoint endpoint, WebSocketListenerOptions options)
        {
            _listener = new WebSocketListener(endpoint, options);
            var rfc6455 = new WebSocketFactoryRfc6455(_listener);
            rfc6455.MessageExtensions.RegisterExtension(new WebSocketDeflateExtension());
            _listener.Standards.RegisterStandard(rfc6455);
           // _listener.ConnectionExtensions.RegisterExtension(new WebSocketSecureConnectionExtension(ca2));
        }
        public void Start()
        {
            _listener.Start();
            Task.Run((Func<Task>)ListenAsync);
        }
        public void Stop()
        {
            _listener.Stop();
        }
        private async Task ListenAsync()
        {
            while (_listener.IsStarted)
            {
                try
                {
                    var websocket = await _listener.AcceptWebSocketAsync(CancellationToken.None)
                                                   .ConfigureAwait(false);
                    if (websocket != null)
                        Task.Run(() => HandleWebSocketAsync(websocket));
                }
                catch (Exception ex)
                {
                    if (OnError != null)
                        OnError.Invoke(null, ex);
                }
            }
        }
        private async Task HandleWebSocketAsync(WebSocket websocket)
        {
            try
            {
                if (OnConnect != null)
                    OnConnect.Invoke(websocket);

                while (websocket.IsConnected)
                {
                    var message = await websocket.ReadStringAsync(CancellationToken.None)
                                                 .ConfigureAwait(false);
                    if (message != null && OnMessage != null)
                        OnMessage.Invoke(websocket, message);
                }

                if (OnDisconnect != null)
                    OnDisconnect.Invoke(websocket);
            }
            catch (Exception ex)
            {
                if (OnError != null)
                    OnError.Invoke(websocket, ex);
            }
            finally
            {
                websocket.Dispose();
            }
        }
        public void Dispose()
        {
            _listener.Dispose();
        }
    }
}