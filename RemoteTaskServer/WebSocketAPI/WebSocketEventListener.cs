#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Security;
using vtortola.WebSockets;
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
        private readonly List<WebSocketListener> _listeners = new List<WebSocketListener>();

        public WebSocketEventListener(List<IPEndPoint> endpoints)
            : this(endpoints, new WebSocketListenerOptions())
        {
        }

        public WebSocketEventListener(List<IPEndPoint> endpoints, WebSocketListenerOptions options)
        {
            foreach (var endpoint in endpoints)
            {
                var listener = new WebSocketListener(endpoint, options);
                var rfc6455 = new WebSocketFactoryRfc6455(listener);
                listener.Standards.RegisterStandard(rfc6455);
                _listeners.Add(listener);
            }
        }

        public void Dispose()
        {
            foreach (var listener in _listeners)
            {
                listener.Dispose();
            }
        }

        public event WebSocketEventListenerOnConnect OnConnect;
        public event WebSocketEventListenerOnDisconnect OnDisconnect;
        public event WebSocketEventListenerOnEncryptedMessage OnEncryptedMessage;
        public event WebSocketEventListenerOnPlainTextMessage OnPlainTextMessage;
        public event WebSocketEventListenerOnError OnError;

        public void Start()
        {
            foreach (var listener in _listeners)
            {
                listener.Start();
            }
            ListenAsync();
        }

        public void Stop()
        {
            foreach (var listener in _listeners)
            {
                listener.Stop();
            }
        }

        private async Task HandleListners(WebSocketListener listener)
        {
            while (listener.IsStarted)
            {
                try
                {
                    var websocket = await listener.AcceptWebSocketAsync(CancellationToken.None)
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

        private void ListenAsync()
        {
            foreach (var listener in _listeners)
            {
                Task.Run(() => HandleListners(listener));
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