#region

using System;
using System.IO;
using System.Net;
using System.Text;
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

    public delegate void WebSocketEventListenerOnPlainTextMessage(WebSocket webSocket, string message);
    public delegate void WebSocketEventListenerOnEncryptedMessage(WebSocket webSocket, byte[] message);

    public delegate void WebSocketEventListenerOnError(WebSocket webSocket, Exception error);

    public class WebSocketEventListener : IDisposable
    {
        private readonly WebSocketListener _listener;

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
        }

        public void Dispose()
        {
            _listener.Dispose();
        }

        public event WebSocketEventListenerOnConnect OnConnect;
        public event WebSocketEventListenerOnDisconnect OnDisconnect;
        public event WebSocketEventListenerOnEncryptedMessage OnEncryptedMessage;
        public event WebSocketEventListenerOnPlainTextMessage OnPlainTextMessage;
        public event WebSocketEventListenerOnError OnError;

        public void Start()
        {
            _listener.Start();
            Task.Run(ListenAsync);
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
                    OnError?.Invoke(null, ex);
                }
            }
        }

        private async Task HandleWebSocketAsync(WebSocket websocket)
        {
            try
            {
                OnConnect?.Invoke(websocket);

                while (websocket.IsConnected)
                {
                    var messageReadStream = await websocket.ReadMessageAsync(CancellationToken.None);
                    if (messageReadStream != null)
                    {
                        switch (messageReadStream.MessageType)
                        {
                            case WebSocketMessageType.Text:
                                using (var sr = new StreamReader(messageReadStream, Encoding.UTF8))
                                {
                                    var stringMessage = await sr.ReadToEndAsync();
                                    if (!string.IsNullOrEmpty(stringMessage))
                                    {
                                        OnPlainTextMessage?.Invoke(websocket, stringMessage);
                                    }
                                }
                                break;
                            case WebSocketMessageType.Binary:
                                using (var ms = new MemoryStream())
                                {
                                    await messageReadStream.CopyToAsync(ms);
                                    var data = ms.ToArray();
                                    if (data.Length > 0)
                                    {
                                        OnEncryptedMessage?.Invoke(websocket, data);
                                    }
                                }
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
                OnDisconnect?.Invoke(websocket);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(websocket, ex);
            }
            finally
            {
                websocket.Dispose();
            }
        }
    }
}