// .NET

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace RemoteTaskServer.WebSocketAPI
{
    // ------------------------------------------------------------------------------------------------------------------- Constants
    internal static class Constants
    {
        internal const int MAJOR_VERSION = 1;
        internal const int MINOR_VERSION = 0;
        internal const int REVISION = 16;
        internal const string PRODUCT_NAME = ".NET WebSocket {0}TaskServer";
        internal const string MANUFACTURER = "";
        public const string WS_CONNECTION = "WS_CONNECTION";
        public const string WS_ENDPOINT = "WS_ENDPOINT";
        public const string WS_HOST = "WS_HOST";
        public const string WS_LOCATION = "WS_LOCATION";
        public const string WS_ORIGIN = "WS_ORIGIN";
        public const string WS_PROTOCOL = "WS_PROTOCOL";
        public const string WS_UPGRADE = "WS_UPGRADE";
        public const string WSS = "WSS";
        public const string WSS_KEYSIZE = "WSS_KEYSIZE";
        public const string WSS_SECRETKEYSIZE = "WSS_SECRETKEYSIZE";
        public const string WSS_SERVER_ISSUER = "WSS_SERVER_ISSUER";
        public const string WSS_SERVER_SUBJECT = "WSS_SERVER_SUBJECT";
        public const string ALL_RAW = "ALL_RAW";
        public const string HTTP_COOKIE = "HTTP_COOKIE";
        public const string QUERYSTRING = "QUERYSTRING";
        public const string CERT_KEYSIZE = "CERT_KEYSIZE";
        public const string CERT_SECRETKEYSIZE = "CERT_SECRETKEYSIZE";
        public const string CERT_SERVER_ISSUER = "CERT_SERVER_ISSUER";
        public const string CERT_SERVER_SUBJECT = "CERT_SERVER_SUBJECT";
        public const string LOCAL_ADDR = "LOCAL_ADDR";
        public const string REMOTE_ADDR = "REMOTE_ADDR";
        public const string REMOTE_HOST = "REMOTE_HOST";
        public const string REMOTE_PORT = "REMOTE_PORT";
        public const string SERVER_NAME = "SERVER_NAME";
        public const string SERVER_PORT = "SERVER_PORT";
        public const string SERVER_PORT_SECURE = "SERVER_PORT_SECURE";
        public const string SERVER_SOFTWARE = "SERVER_SOFTWARE";
        public const string URL = "URL";
        public const int WS_PORT = 80;
        public const int WS_SSL_PORT = 443;
    }

    // ------------------------------------------------------------------------------------------------------------- WebSocketStream
    internal class WebSocketStream : NetworkStream
    {
        public WebSocketStream(Socket aSocket) : base(aSocket, true)
        {
        }

        public new Socket Socket
        {
            get { return base.Socket; }
        }
    }

    // ---------------------------------------------------------------------------------------------------------- WebSocketSslStream
    internal class WebSocketSslStream : SslStream
    {
        public WebSocketSslStream(WebSocketStream aStream) : base(aStream, true)
        {
        }

        public Socket Socket
        {
            get { return ((WebSocketStream) InnerStream).Socket; }
        }
    }

    // ------------------------------------------------------------------------------------------------ WebSocketNameValueCollection
    internal class WebSocketNameValueCollection : NameValueCollection
    {
        public WebSocketNameValueCollection()
        {
            SetReadOnly(false);
        }

        public void Sort()
        {
            var temp = new WebSocketNameValueCollection();

            var keys = AllKeys;
            Array.Sort(keys);
            foreach (var key in keys)
            {
                temp.Add(key, this[key]);
            }
            Clear();
            Add(temp);
        }

        internal void SetReadOnly(bool aIsReadOnly)
        {
            IsReadOnly = aIsReadOnly;
        }
    }

    // ------------------------------------------------------------------------------------------------------- WebSocketException(s)
    public class WebSocketException : Exception
    {
        public WebSocketException()
        {
        }

        public WebSocketException(string aMessage) : base(aMessage)
        {
        }
    }

    public class WebSocketParseException : WebSocketException
    {
        public WebSocketParseException()
        {
        }

        public WebSocketParseException(string aMessage) : base(aMessage)
        {
        }
    }

    public class WebSocketMalformedHeaderException : WebSocketParseException
    {
        public WebSocketMalformedHeaderException(string aMessage) : base(aMessage)
        {
        }
    }

    public class WebSocketMalformedBodyException : WebSocketParseException
    {
        public WebSocketMalformedBodyException(string aMessage) : base(aMessage)
        {
        }
    }

    // --------------------------------------------------------------------------------------------------------- WebSocketParameters
    internal enum WebSocketProtocol
    {
        V75,
        V76
    }

    public sealed class WebSocketParameters : IDisposable
    {
        // Internal ...
        internal int HttpMajorVersion;
        internal int HttpMinorVersion;
        private bool IsDisposed;

        internal WebSocketParameters()
        {
            IsDisposed = false;

            ServerVariables = new WebSocketNameValueCollection();
            QueryString = new WebSocketNameValueCollection();
            Cookies = new CookieCollection();
            Session = new WebSocketSessionBase();
            Protocol = WebSocketProtocol.V75;
            SecureKey1 = SecureKey2 = 0;
            SecureKey3 = null;
        }

        public NameValueCollection ServerVariables { get; private set; }
        public NameValueCollection QueryString { get; private set; }
        public CookieCollection Cookies { get; private set; }
        public WebSocketSessionBase Session { get; private set; }
        internal WebSocketProtocol Protocol { get; set; }
        internal uint SecureKey1 { get; set; }
        internal uint SecureKey2 { get; set; }
        internal byte[] SecureKey3 { get; set; }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                SecureKey3 = null;

                if (Session != null)
                {
                    Session.Clear();
                    Session = null;
                }

                if (Cookies != null)
                {
                    Cookies = null;
                }

                if (QueryString != null)
                {
                    QueryString.Clear();
                    QueryString = null;
                }

                if (ServerVariables != null)
                {
                    ServerVariables.Clear();
                    ServerVariables = null;
                }

                IsDisposed = true;
            }
        }

        ~WebSocketParameters()
        {
            Dispose();
        }
    }

    // ----------------------------------------------------------------------------------------------------------- FrameReceivedArgs
    public sealed class FrameReceivedArgs : EventArgs
    {
        public FrameReceivedArgs(int aSize, string aData, bool aIsBinary)
        {
            Size = aSize;
            Data = aData;
            IsBinary = aIsBinary;
        }

        public int Size { get; private set; }
        public string Data { get; private set; }
        public bool IsBinary { get; set; }
    }

    // ------------------------------------------------------------------------------------------------------------- WebSocketClient
    public delegate void FrameReceivedHandler(WebSocketClient aSender, FrameReceivedArgs aEventArgs);

    public delegate void DisconnectedEventHandler(WebSocketClient aSender, EventArgs aEventArgs);

    public class WebSocketClient
    {
        private byte[] FrameBuffer;
        private StringBuilder FrameData;
        private long FrameLength;
        private string ResolvedHostName;
        private string ResolvedServerHostName;
        private WebSocketState State;

        public WebSocketClient(Stream aStream, int aBufferSize, WebSocketParameters aParams)
        {
            Stream = aStream;
            FrameBuffer = new byte[aBufferSize];
            FrameData = new StringBuilder();
            ID = Guid.NewGuid();
            Params = aParams;

            AddServerVariables(Params.ServerVariables);

            var collection = Params.ServerVariables as WebSocketNameValueCollection;
            if (collection != null)
            {
                collection.Sort();
                collection.SetReadOnly(true);
            }

            collection = Params.QueryString as WebSocketNameValueCollection;
            if (collection != null)
            {
                collection.Sort();
                collection.SetReadOnly(true);
            }
        }

        public bool IsSecure
        {
            get { return (Stream is WebSocketSslStream); }
        }

        public Socket Socket
        {
            get
            {
                if (IsSecure)
                {
                    return ((WebSocketSslStream) Stream).Socket;
                }
                return ((WebSocketStream) Stream).Socket;
            }
        }

        public IPAddress Address
        {
            get { return ((IPEndPoint) Socket.RemoteEndPoint).Address; }
        }

        public int Port
        {
            get { return ((IPEndPoint) Socket.RemoteEndPoint).Port; }
        }

        public IPAddress ServerAddress
        {
            get { return ((IPEndPoint) Socket.LocalEndPoint).Address; }
        }

        public int ServerPort
        {
            get { return ((IPEndPoint) Socket.LocalEndPoint).Port; }
        }

        public string HostName
        {
            get
            {
                if (ResolvedHostName == null)
                {
                    ResolvedHostName = Utility.ResolveHost(Address);
                }
                return ResolvedHostName;
            }
        }

        public string ServerHostName
        {
            get
            {
                if (ResolvedServerHostName == null)
                {
                    ResolvedServerHostName = Utility.ResolveHost(ServerAddress);
                }
                return ResolvedServerHostName;
            }
        }

        public string EndPoint
        {
            get { return Params.ServerVariables[Constants.WS_ENDPOINT]; }
        }

        public string Protocol
        {
            get { return Params.ServerVariables[Constants.WS_PROTOCOL]; }
        }

        public Stream Stream { get; private set; }
        public Guid ID { get; private set; }
        public WebSocketParameters Params { get; private set; }
        public event FrameReceivedHandler FrameReceived;
        public event DisconnectedEventHandler Disconnected;

        private void AddServerVariables(NameValueCollection aCollection)
        {
            aCollection.Add(Constants.WSS, IsSecure ? "ON" : "OFF");
            aCollection.Add(Constants.REMOTE_ADDR, Address.ToString());
            if (HostName != string.Empty)
            {
                aCollection.Add(Constants.REMOTE_HOST, HostName);
            }
            aCollection.Add(Constants.REMOTE_PORT, Port.ToString());

            aCollection.Add(Constants.LOCAL_ADDR, ServerAddress.ToString());
            aCollection.Add(Constants.SERVER_NAME, ServerHostName);
            aCollection.Add(Constants.SERVER_PORT, ServerPort.ToString());
            aCollection.Add(Constants.SERVER_PORT_SECURE, IsSecure ? "1" : "0");

            if (IsSecure)
            {
                var ssl = Stream as SslStream;
                if (ssl != null)
                {
                    aCollection.Add(Constants.WSS_KEYSIZE, ssl.CipherStrength.ToString());
                    aCollection.Add(Constants.WSS_SECRETKEYSIZE, ssl.KeyExchangeStrength.ToString());
                    aCollection.Add(Constants.WSS_SERVER_ISSUER, ssl.LocalCertificate.Issuer);
                    aCollection.Add(Constants.WSS_SERVER_SUBJECT, ssl.LocalCertificate.Subject);

                    aCollection.Add(Constants.CERT_KEYSIZE, ssl.CipherStrength.ToString());
                    aCollection.Add(Constants.CERT_SECRETKEYSIZE, ssl.KeyExchangeStrength.ToString());
                    aCollection.Add(Constants.CERT_SERVER_ISSUER, ssl.LocalCertificate.Issuer);
                    aCollection.Add(Constants.CERT_SERVER_SUBJECT, ssl.LocalCertificate.Subject);
                }
            }
        }

        ~WebSocketClient()
        {
            Shutdown(false);
        }

        protected virtual void OnFrameReceived(FrameReceivedArgs aEventArgs)
        {
            if (FrameReceived != null)
            {
                FrameReceived(this, aEventArgs);
            }
        }

        protected virtual void OnDisconnected()
        {
            if (Disconnected != null)
            {
                Disconnected(this, EventArgs.Empty);
            }
        }

        private void FireOnFrameReceived(FrameReceivedArgs aEventArgs)
        {
            try
            {
                OnFrameReceived(aEventArgs);
            }
            catch
            {
                // DO NOTHING
            }
        }

        private void FireOnDisconnected()
        {
            try
            {
                OnDisconnected();
            }
            catch
            {
                // DO NOTHING
            }
        }

        internal void Listen()
        {
            State = WebSocketState.IDLE;
            FrameLength = 0;
            WaitForData();
        }

        private void WaitForData()
        {
            Stream.BeginRead(FrameBuffer, 0, FrameBuffer.Length, ReadData, null);
        }

        private void ReadData(IAsyncResult aResult)
        {
            var bytesRead = Stream.EndRead(aResult);
            if (bytesRead > 0)
            {
                int i = 0, j;

                if (State == WebSocketState.IDLE)
                {
                    // Frame start ...
                    for (i = 0; i < bytesRead - 1; i++)
                    {
                        var highBitSet = (FrameBuffer[i] & 0x80) > 0;
                        if (!highBitSet && FrameBuffer[i] == (byte) WrapperBytes.Start)
                        {
                            State = WebSocketState.GET_FRAME;
                        }

                        if (highBitSet && FrameBuffer[i] == (byte) WrapperBytes.End)
                        {
                            State = WebSocketState.GET_FRAMELENGTH;
                        }

                        if (State != WebSocketState.IDLE)
                        {
                            FrameLength = 0;
                            i++;
                            break;
                        }
                    }
                }

                if (State == WebSocketState.GET_FRAMELENGTH)
                {
                    // Frame length ...
                    for (j = i; j < bytesRead; j++)
                    {
                        if (FrameBuffer[j] == (byte) WrapperBytes.Start)
                        {
                            if (FrameLength == 0)
                            {
                                State = WebSocketState.CLIENTSIDE_CLOSE;
                            }
                            else
                            {
                                if (FrameLength > int.MaxValue)
                                {
                                    // Attempt to overflow the buffer .. abort the connection ...
                                    Shutdown(true);
                                    return;
                                }
                                State = WebSocketState.GET_BINARYFRAME;
                            }
                            break;
                        }
                        if ((FrameBuffer[j] & 0x80) == 0x80)
                        {
                            // Add to frame length ...
                            FrameLength = (FrameLength*0x80) + (FrameBuffer[j] & 0x7f);
                        }
                        else
                        {
                            // Bogus frame ... abort the connection
                            Shutdown(true);
                            return;
                        }

                        if (State != WebSocketState.GET_FRAMELENGTH)
                        {
                            i = j + 1;
                            break;
                        }
                    }
                }

                if (State == WebSocketState.GET_FRAME)
                {
                    // Text frame ...
                    var EOF = false;
                    for (j = i; j < bytesRead; j++)
                    {
                        if (FrameBuffer[j] == (byte) WrapperBytes.End)
                        {
                            EOF = true;
                            break;
                        }
                    }

                    FrameData.Append(Encoding.UTF8.GetString(FrameBuffer, i, j - i));
                    if (EOF)
                    {
                        InternalFrameReceived(Encoding.UTF8, false);
                        State = WebSocketState.IDLE;
                    }
                }

                if (State == WebSocketState.GET_BINARYFRAME)
                {
                    // Binary frame ...
                    j = Utility.Min(bytesRead - i, (int) FrameLength);
                    FrameData.Append(Utility.CP437.GetString(FrameBuffer, i, j));
                    FrameLength -= j;

                    if (FrameLength == 0)
                    {
                        InternalFrameReceived(Utility.CP437, true);
                        State = WebSocketState.IDLE;
                    }
                }

                // Client asked for close ... so acknowledge and close connection
                if (State == WebSocketState.CLIENTSIDE_CLOSE)
                {
                    Close(false);
                    State = WebSocketState.CLOSED;
                }

                // TaskServer asked for close ... verify client acknowlegde
                if (State == WebSocketState.SERVERSIDE_CLOSE)
                {
                    if (i < bytesRead)
                    {
                        if (FrameBuffer[i] == (byte) WrapperBytes.End)
                        {
                            State = WebSocketState.CLOSING;
                            i++;
                        }
                        else
                        {
                            // Bogus frame ... abort the connection
                            Shutdown(true);
                            return;
                        }
                    }
                }

                // Did the client acknowledge the close?
                if (State == WebSocketState.CLOSING)
                {
                    if (i < bytesRead)
                    {
                        if (FrameBuffer[i] == (byte) WrapperBytes.Start)
                        {
                            State = WebSocketState.CLOSED;
                        }
                        else
                        {
                            // Bogus frame ... abort the connection
                            Shutdown(true);
                            return;
                        }
                    }
                }

                // Final close so shutdown ...
                if (State == WebSocketState.CLOSED)
                {
                    Shutdown(true);
                    return;
                }

                WaitForData();
            }
            else
            {
                FireOnDisconnected();
            }
        }

        private void InternalFrameReceived(Encoding aEncoding, bool aIsBinary)
        {
            var frame = FrameData.ToString();
            var frameLen = aEncoding.GetBytes(frame.ToCharArray()).Length;

            FireOnFrameReceived(new FrameReceivedArgs(frameLen, frame, aIsBinary));

            FrameData = null;
            FrameData = new StringBuilder();
        }

        public void Write(string aData)
        {
            if (Socket.Connected)
            {
                try
                {
                    var dataBytes = Encoding.UTF8.GetBytes(aData);

                    Stream.WriteByte((byte) WrapperBytes.Start);
                    Stream.Write(dataBytes, 0, dataBytes.Length);
                    Stream.WriteByte((byte) WrapperBytes.End);
                    Stream.Flush();
                }
                catch
                {
                    FireOnDisconnected();
                }
            }
        }

        public void WriteBinary(byte[] aData)
        {
            if (Socket.Connected)
            {
                try
                {
                    Stream.WriteByte((byte) WrapperBytes.End);

                    var dataLen = aData.Length;
                    do
                    {
                        Stream.WriteByte((byte) ((dataLen%128) | 0x80));
                        dataLen /= 128;
                    } while (dataLen > 0);

                    Stream.WriteByte((byte) WrapperBytes.Start);
                    Stream.Write(aData, 0, dataLen);
                    Stream.Flush();
                }
                catch
                {
                    FireOnDisconnected();
                }
            }
        }

        public void WriteBinary(string aData)
        {
            WriteBinary(Utility.CP437.GetBytes(aData));
        }

        public void Close()
        {
            Close(true);
        }

        private void Close(bool aServerSide)
        {
            if (!Socket.Connected)
            {
                Shutdown(false);
                return;
            }

            switch (Params.Protocol)
            {
                case WebSocketProtocol.V76:
                    try
                    {
                        Stream.WriteByte((byte) WrapperBytes.End);
                        Stream.WriteByte((byte) WrapperBytes.Start);
                        Stream.Flush();
                    }
                    catch
                    {
                        Shutdown(Socket.Connected);
                        return;
                    }
                    break;

                default:
                    Shutdown(true);
                    return;
            }

            if (aServerSide)
            {
                State = WebSocketState.SERVERSIDE_CLOSE;
            }
        }

        private void Shutdown(bool aGracefully)
        {
            if (Stream != null)
            {
                try
                {
                    if (Socket != null && Socket.Connected && aGracefully)
                    {
                        FireOnDisconnected();
                    }
                    Stream.Dispose();
                }
                catch
                {
                    // DO NOTHING
                }
                Stream = null;
            }

            FrameBuffer = null;
            FrameData = null;

            if (Params != null)
            {
                Params.Dispose();
                Params = null;
            }
        }

        private enum WrapperBytes : byte
        {
            Start = 0x00,
            End = 0xff
        };

        private enum WebSocketState
        {
            IDLE,
            GET_FRAMELENGTH,
            GET_FRAME,
            GET_BINARYFRAME,
            CLIENTSIDE_CLOSE,
            SERVERSIDE_CLOSE,
            CLOSING,
            CLOSED
        }
    }

    // ---------------------------------------------------------------------------------------------------- ConnectionErrorEventArgs
    public sealed class ConnectionErrorEventArgs : EventArgs
    {
        public ConnectionErrorEventArgs(Exception aException)
        {
            Exception = aException;
        }

        public Exception Exception { get; private set; }
    }

    // ----------------------------------------------------------------------------------------------------- ValidateOriginEventArgs
    public sealed class ValidateOriginEventArgs : EventArgs
    {
        public ValidateOriginEventArgs(string aOrigin)
        {
            Origin = aOrigin;
        }

        public string Origin { get; private set; }
    }

    // ------------------------------------------------------------------------------------------------------------- WebSocketServer
    public delegate void ClientConnectedEventHandler(WebSocketClient aSender, EventArgs aEventArgs);

    public delegate void ClientDisconnectedEventHandler(WebSocketClient aSender, EventArgs aEventArgs);

    public delegate void ClientConnectionErrorEventHandler(WebSocketServer aSender, ConnectionErrorEventArgs aEventArgs);

    public delegate bool ValidateOriginEventHandler(WebSocketServer aSender, ValidateOriginEventArgs aEventArgs);

    public class WebSocketServer
    {
        // Regular expressions 
        private const RegexOptions DefaultOptions =
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ECMAScript;

        private static readonly Regex HttpGetRe = new Regex(@"^GET (.*) HTTP\/(\d)\.(\d)\s*$", DefaultOptions);
        private static readonly Regex HttpHeaderRe = new Regex(@"^(.*): (.*)\s*$", DefaultOptions);
        private static readonly Regex UpgradeRe = new Regex(@"^Upgrade: (.*)\s*$", DefaultOptions);
        private static readonly Regex ConnectionRe = new Regex(@"^Connection: (.*)\s*$", DefaultOptions);
        private static readonly Regex HostRe = new Regex(@"^Host: (.*)\s*$", DefaultOptions);
        private static readonly Regex OriginRe = new Regex(@"^Origin: (.*)\s*$", DefaultOptions);
        private static readonly Regex ProtocolRe = new Regex(@"^(Sec-)*WebSocket-Protocol: (.*)\s*$", DefaultOptions);
        private static readonly Regex KeyRe = new Regex(@"^Sec-WebSocket-key(\d): (.*)\s*$", DefaultOptions);
        private static readonly Regex CookieRe = new Regex(@"^Cookie: (.*)\s*$", DefaultOptions);

        public WebSocketServer() :
            this(IPAddress.Loopback, Constants.WS_PORT)
        {
        }

        public WebSocketServer(string aListenOn) :
            this(aListenOn, Constants.WS_PORT)
        {
        }

        public WebSocketServer(string aListenOn, int aPort)
        {
            IPAddress ip;

            if (!IPAddress.TryParse(aListenOn, out ip))
            {
                var hostEntry = Dns.GetHostEntry(aListenOn);
                ip = hostEntry.AddressList[0];
            }
            Initialize(ip, aPort);
        }

        public WebSocketServer(IPAddress aListenOn, int aPort)
        {
            Initialize(aListenOn, aPort);
        }

        public int ClientCount
        {
            get
            {
                var result = 0;
                lock (Clients)
                {
                    result = Clients.Count;
                }
                return result;
            }
        }

        public Socket Socket { get; private set; }
        public int Port { get; set; }
        public IPAddress ListenOn { get; set; }
        public List<string> ValidOrigins { get; private set; }
        public bool IsRunning { get; private set; }
        protected List<WebSocketClient> Clients { get; private set; }

        protected virtual bool IsSecure
        {
            get { return false; }
        }

        public virtual int MajorVersion
        {
            get { return Constants.MAJOR_VERSION; }
        }

        public virtual int MinorVersion
        {
            get { return Constants.MINOR_VERSION; }
        }

        public virtual int Revision
        {
            get { return Constants.REVISION; }
        }

        public virtual string ProductName
        {
            get { return string.Format(Constants.PRODUCT_NAME, IsSecure ? "SSL " : ""); }
        }

        public virtual string FullVersion
        {
            get { return string.Format("{0}.{1}.{2}", MajorVersion, MinorVersion, Revision); }
        }

        public virtual string Manufacturer
        {
            get { return Constants.MANUFACTURER; }
        }

        public event ClientConnectedEventHandler ClientConnected;
        public event ClientDisconnectedEventHandler ClientDisconnected;
        public event ClientConnectionErrorEventHandler ClientConnectionError;
        public event ValidateOriginEventHandler ValidateOrigin;

        protected virtual void Initialize(IPAddress aListenOn, int aPort)
        {
            IsRunning = false;
            Socket = null;
            ListenOn = aListenOn ?? IPAddress.Loopback;
            Port = aPort;
            Clients = new List<WebSocketClient>();
            ValidOrigins = new List<string>();
        }

        ~WebSocketServer()
        {
            Stop();

            if (ValidOrigins != null)
            {
                ValidOrigins.Clear();
                ValidOrigins = null;
            }

            Clients = null;
            ListenOn = null;
        }

        public void Start()
        {
            if (IsRunning)
            {
                return;
            }

            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            var endPoint = new IPEndPoint(ListenOn, Port);
            Socket.Bind(endPoint);
            Socket.Listen(100);
            Listen();

            IsRunning = true;
        }

        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            if (Socket != null)
            {
                CloseAll();

                try
                {
                    Socket.Shutdown(SocketShutdown.Both);
                }
                catch
                {
                    // DO NOTHING
                }

                try
                {
                    Socket.Close(); // Dispose(); TODO: .NET 4
                }
                catch
                {
                    // DO NOTHING
                }

                Socket = null;
            }

            lock (Clients)
            {
                Clients.Clear();
            }

            IsRunning = false;
        }

        private void Listen()
        {
            Socket.BeginAccept(AcceptClient, null);
        }

        private void AcceptClient(IAsyncResult aResult)
        {
            var socket = Socket.EndAccept(aResult);
            try
            {
                var stream = CreateStream(socket);
                try
                {
                    var parameters = Handshake(stream);

                    var client = CreateClient(stream, parameters);
                    client.Disconnected += InternalClientDisconnected;

                    AddClient(client);

                    client.Listen();
                }
                catch (Exception ex)
                {
                    FireOnClientConnectionError(ex);
                    try
                    {
                        stream.Dispose();
                    }
                    catch
                    {
                        // DO NOTHING
                    }
                    stream = null;
                    socket = null;
                }
            }
            catch (Exception ex)
            {
                FireOnClientConnectionError(ex);
                try
                {
                    socket.Close(); //Dispose(); TODO: .NET 4
                }
                catch
                {
                    // DO NOTHING
                }
                socket = null;
            }
            Listen();
        }

        private void AddClient(WebSocketClient aClient)
        {
            lock (Clients)
            {
                Clients.Add(aClient);
                FireOnClientConnected(aClient);
            }
        }

        protected virtual void OnClientConnected(WebSocketClient aClient)
        {
            if (ClientConnected != null)
            {
                ClientConnected(aClient, EventArgs.Empty);
            }
        }

        protected virtual void OnClientDisconnected(WebSocketClient aClient)
        {
            if (ClientDisconnected != null)
            {
                ClientDisconnected(aClient, EventArgs.Empty);
            }
        }

        protected virtual void OnClientConnectionError(Exception aException)
        {
            if (ClientConnectionError != null)
            {
                ClientConnectionError(this, new ConnectionErrorEventArgs(aException));
            }
        }

        protected virtual bool OnValidateOrigin(string aOrigin)
        {
            if (ValidateOrigin != null)
            {
                return ValidateOrigin(this, new ValidateOriginEventArgs(aOrigin));
            }
            return false;
        }

        private void FireOnClientConnected(WebSocketClient aClient)
        {
            try
            {
                OnClientConnected(aClient);
            }
            catch
            {
                // DO NOTHING
            }
        }

        private void FireOnClientDisconnected(WebSocketClient aClient)
        {
            try
            {
                OnClientDisconnected(aClient);
            }
            catch
            {
                // DO NOTHING
            }
        }

        private void FireOnClientConnectionError(Exception aException)
        {
            try
            {
                OnClientConnectionError(aException);
            }
            catch
            {
                // DO NOTHING
            }
        }

        private bool FireOnValidateOrigin(string aOrigin)
        {
            try
            {
                return OnValidateOrigin(aOrigin);
            }
            catch
            {
                // DO NOTHING
            }
            return false;
        }

        private bool IsValidOrigin(string aOrigin)

        {
            
            return (FireOnValidateOrigin(aOrigin) || (ValidOrigins.IndexOf(aOrigin) >= 0));
        }

        private void InternalClientDisconnected(WebSocketClient aSender, EventArgs aEventArgs)
        {
            lock (Clients)
            {
                if (Clients.IndexOf(aSender) >= 0)
                {
                    aSender.Disconnected -= InternalClientDisconnected;

                    Clients.Remove(aSender);
                    FireOnClientDisconnected(aSender);
                }
            }
        }

        public WebSocketClient GetClient(Guid aID)
        {
            WebSocketClient result = null;

            lock (Clients)
            {
                foreach (var client in Clients)
                {
                    if (client.ID == aID)
                    {
                        result = client;
                        break;
                    }
                }
            }

            if (result == null)
            {
                throw new ArgumentOutOfRangeException();
            }

            return result;
        }

        public void WriteToAll(string aData)
        {
            lock (Clients)
            {
                Clients.ForEach(client => client.Write(aData));
            }
        }

        public void WriteToAll(byte[] aData)
        {
            lock (Clients)
            {
                Clients.ForEach(client => client.WriteBinary(aData));
            }
        }

        public void WriteToAllExceptOne(string aData, WebSocketClient aSkip)
        {
            lock (Clients)
            {
                foreach (var client in Clients)
                {
                    if (client != aSkip)
                    {
                        client.Write(aData);
                    }
                }
            }
        }

        public void WriteToAllExceptOne(string aData, Guid aSkip)
        {
            try
            {
                WriteToAllExceptOne(aData, GetClient(aSkip));
            }
            catch
            {
                // DO NOTHING
            }
        }

        public void WriteToAllExceptOne(byte[] aData, WebSocketClient aSkip)
        {
            lock (Clients)
            {
                foreach (var client in Clients)
                {
                    if (client != aSkip)
                    {
                        client.WriteBinary(aData);
                    }
                }
            }
        }

        public void WriteToAllExceptOne(byte[] aData, Guid aSkip)
        {
            try
            {
                WriteToAllExceptOne(aData, GetClient(aSkip));
            }
            catch
            {
                // DO NOTHING
            }
        }

        public void CloseAll()
        {
            lock (Clients)
            {
                try
                {
                    Clients.ForEach(client => client.Close());
                }
                catch
                {
                    // DO NOTHING
                }
            }
        }

        protected virtual Stream CreateStream(Socket aSocket)
        {
            return new WebSocketStream(aSocket);
        }

        protected virtual WebSocketClient CreateClient(Stream aStream, WebSocketParameters aParams)
        {
            return new WebSocketClient(aStream, 4096, aParams);
        }

        internal WebSocketParameters Handshake(Stream aStream)
        {
            WebSocketParameters result = null;

            // Read the request header & optional body ...
            var reader = new StreamReader(aStream, Utility.CP437);
            try
            {
                var request = new StringBuilder();
                try
                {
                    while (true)
                    {
                        var requestBuffer = reader.ReadLine();
                        if (requestBuffer != null)
                        {
                            requestBuffer = requestBuffer.TrimEnd();
                        }
                        if (!string.IsNullOrEmpty(requestBuffer))
                        {
                            request.AppendLine(requestBuffer);
                        }
                        else
                        {
                            break;
                        }
                    }

                    result = ParseHeaders(request.ToString());

                    switch (result.Protocol)
                    {
                        case WebSocketProtocol.V76:
                            try
                            {
                                var requestBuffer = new char[8];
                                if (reader.Read(requestBuffer, 0, 8) == 8)
                                {
                                    result.SecureKey3 = Utility.CP437.GetBytes(requestBuffer, 0, 8);
                                }
                                else
                                {
                                    throw new IOException();
                                }
                            }
                            catch
                            {
                                throw new WebSocketMalformedBodyException("\"Body\" missing or invalid!");
                            }
                            break;

                        default:
                            break;
                    }
                }
                finally
                {
                    request = null;
                }
            }
            finally
            {
                reader = null;
            }

            CompleteHandshake(aStream, result);

            return result;
        }

        private void CompleteHandshake(Stream aStream, WebSocketParameters aParams)
        {
            if (aParams == null)
            {
                throw new WebSocketParseException();
            }

            if (aParams.ServerVariables.Count == 0)
            {
                throw new WebSocketParseException("HTTP headers missing or invalid!");
            }

            var headers = aParams.ServerVariables;
            if (headers[Constants.WS_UPGRADE].Trim() != "WebSocket")
            {
                throw new WebSocketMalformedHeaderException("\"Upgrade\" header missing or invalid!");
            }

            if (headers[Constants.WS_CONNECTION].Trim() != "Upgrade")
            {
                throw new WebSocketMalformedHeaderException("\"Connection\" header missing or invalid!");
            }
            string origin = headers[Constants.WS_ORIGIN] ?? "?";
            origin = origin.Trim();
            if (!IsValidOrigin(origin))
            {
                throw new WebSocketMalformedHeaderException("\"Origin\" header missing or invalid!");
            }

            var response = new StringBuilder();
            try
            {
                switch (aParams.Protocol)
                {
                    case WebSocketProtocol.V76:
                        response.AppendFormat("HTTP/{0}.{1} 101 WebSocket Protocol Handshake\r\n",
                            aParams.HttpMajorVersion, aParams.HttpMinorVersion);
                        break;

                    default:
                        response.AppendFormat("HTTP/{0}.{1} 101 Web Socket Protocol Handshake\r\n",
                            aParams.HttpMajorVersion, aParams.HttpMinorVersion);
                        break;
                }

                response.Append("Upgrade: WebSocket\r\n");
                response.Append("Connection: Upgrade\r\n");

                switch (aParams.Protocol)
                {
                    case WebSocketProtocol.V76:
                        response.AppendFormat("Sec-WebSocket-Origin: {0}\r\n", headers[Constants.WS_ORIGIN]);
                        response.AppendFormat("Sec-WebSocket-Location: {0}\r\n", headers[Constants.WS_LOCATION]);

                        if (headers[Constants.WS_PROTOCOL] != null)
                        {
                            response.AppendFormat("Sec-WebSocket-Protocol: {0}\r\n", headers[Constants.WS_PROTOCOL]);
                        }

                        break;

                    default:
                        response.AppendFormat("WebSocket-Origin: {0}\r\n", headers[Constants.WS_ORIGIN]);
                        response.AppendFormat("WebSocket-Location: {0}\r\n", headers[Constants.WS_LOCATION]);

                        if (headers[Constants.WS_PROTOCOL] != null)
                        {
                            response.AppendFormat("WebSocket-Protocol: {0}\r\n", headers[Constants.WS_PROTOCOL]);
                        }

                        break;
                }

                response.Append("\r\n");

                var responseData = Utility.CP437.GetBytes(response.ToString());
                aStream.Write(responseData, 0, responseData.Length);

                switch (aParams.Protocol)
                {
                    case WebSocketProtocol.V76:
                        using (var stream = new MemoryStream())
                        {
                            WriteKeyData(stream, aParams.SecureKey1);
                            WriteKeyData(stream, aParams.SecureKey2);
                            WriteKeyData(stream, aParams.SecureKey3);

                            var hash = Utility.MD5(stream.ToArray());
                            aStream.Write(hash, 0, hash.Length);
                        }
                        break;

                    default:
                        break;
                }

                aStream.Flush();
            }
            finally
            {
                response = null;
            }
        }

        private void WriteKeyData(Stream aStream, byte[] aKeyData)
        {
            aStream.Write(aKeyData, 0, aKeyData.Length);
        }

        private void WriteKeyData(Stream aStream, uint aKeyData)
        {
            WriteKeyData(aStream, BitConverter.GetBytes(aKeyData));
        }

        private WebSocketParameters ParseHeaders(string aHeaders)
        {
            var result = new WebSocketParameters();

            ParseHeader(HttpGetRe, aHeaders, delegate(List<string> aValues)
            {
                var v = aValues[0];

                if (v != string.Empty)
                {
                    result.ServerVariables.Add(Constants.URL, v);
                    var i = v.IndexOf("?");

                    var endPoint = i > 0 ? v.Substring(0, i) : v;
                    if (endPoint.EndsWith("/") && endPoint != "/")
                    {
                        endPoint = endPoint.Substring(0, endPoint.Length - 1);
                    }
                    result.ServerVariables.Add(Constants.WS_ENDPOINT, endPoint);

                    if (i >= 0)
                    {
                        v = v.Substring(i + 1);
                        if (v != string.Empty)
                        {
                            result.ServerVariables.Add(Constants.QUERYSTRING, v);
                            result.QueryString.Add(HttpUtility.ParseQueryString(v));
                        }
                    }
                }

                result.HttpMajorVersion = Convert.ToInt32(aValues[1]);
                result.HttpMinorVersion = Convert.ToInt32(aValues[2]);
            });

            ParseHeader(UpgradeRe, aHeaders, delegate(List<string> aValues)
            {
                var v = aValues[0];

                if (v != string.Empty)
                {
                    result.ServerVariables.Add(Constants.WS_UPGRADE, v);
                }
            });

            ParseHeader(ConnectionRe, aHeaders, delegate(List<string> aValues)
            {
                var v = aValues[0];

                if (v != string.Empty)
                {
                    result.ServerVariables.Add(Constants.WS_CONNECTION, v);
                }
            });

            ParseHeader(HostRe, aHeaders, delegate(List<string> aValues)
            {
                var v = aValues[0];

                if (v != string.Empty)
                {
                    result.ServerVariables.Add(Constants.WS_HOST, v.Split(':')[0]);
                }
            });

            ParseHeader(OriginRe, aHeaders, delegate(List<string> aValues)
            {
                var v = aValues[0];

                if (v != string.Empty)
                {
                    result.ServerVariables.Add(Constants.WS_ORIGIN, v);
                }
            });

            ParseHeader(ProtocolRe, aHeaders, delegate(List<string> aValues)
            {
                var v = aValues[1];

                if (v != string.Empty)
                {
                    result.ServerVariables.Add(Constants.WS_PROTOCOL, v);
                }
            });

            ParseHeader(CookieRe, aHeaders, delegate(List<string> aValues)
            {
                var v = aValues[0];

                if (v != string.Empty)
                {
                    result.ServerVariables.Add(Constants.HTTP_COOKIE, v.Trim());

                    var cookies = v.Split(';');
                    foreach (var cookie in cookies)
                    {
                        try
                        {
                            var i = cookie.IndexOf('=');
                            if (i > 0)
                            {
                                var cookieName = cookie.Substring(0, i).Trim();
                                var cookieValue = HttpUtility.UrlDecode(cookie.Substring(i + 1).Trim());

                                result.Cookies.Add(new Cookie(cookieName, cookieValue));
                            }
                        }
                        catch
                        {
                            // DO NOTHING
                        }
                    }
                }
            });

            ParseHeader(KeyRe, aHeaders, delegate(List<string> aValues)
            {
                var i = int.Parse(aValues[0]);
                var v = aValues[1];

                if (v != string.Empty)
                {
                    if (i == 1)
                    {
                        result.SecureKey1 = ParseKey(v);
                    }
                    if (i == 2)
                    {
                        result.SecureKey2 = ParseKey(v);
                    }
                }
            });

            if (result.SecureKey1 != 0 && result.SecureKey2 != 0)
            {
                result.Protocol = WebSocketProtocol.V76;
            }

            if (result.ServerVariables.Count > 0)
            {
                // Add HEADER_ ...
                ParseHeader(HttpHeaderRe, aHeaders, delegate(List<string> aValues)
                {
                    if (aValues.Count != 2)
                    {
                        return;
                    }

                    var k = aValues[0].Trim();
                    if (k == string.Empty)
                    {
                        return;
                    }

                    var v = aValues[1].Trim();
                    if (v == string.Empty)
                    {
                        return;
                    }

                    result.ServerVariables.Add(string.Format("HEADER_{0}", k), v);
                });

                // Add ALL_RAW ...
                result.ServerVariables.Add(Constants.ALL_RAW, aHeaders);

                // Add SERVER_SOFTWARE ...
                result.ServerVariables.Add(Constants.SERVER_SOFTWARE,
                    string.Format("{0} V{1}, {2}", ProductName, FullVersion, Manufacturer));

                // Add WS_LOCATION ...
                result.ServerVariables.Add(Constants.WS_LOCATION,
                    string.Format("{0}://{1}{2}{3}",
                        IsSecure ? "wss" : "ws", result.ServerVariables[Constants.WS_HOST],
                        (Port != (IsSecure ? Constants.WS_SSL_PORT : Constants.WS_PORT) ? ":" + Port : ""),
                        result.ServerVariables[Constants.URL]));
            }

            return result;
        }

        private uint ParseKey(string aKey)
        {
            uint c = 0;
            var v = string.Empty;

            for (int i = 0, l = aKey.Length; i < l; i++)
            {
                if (char.IsDigit(aKey[i]))
                {
                    v += aKey[i];
                }
                else if (char.IsWhiteSpace(aKey[i]))
                {
                    c += 1;
                }
            }

            return Utility.ToBigEndian((uint) (Convert.ToUInt64(v)/c));
        }

        private void ParseHeader(Regex aPatternRe, string aData, ParseHeaderCallback aCallback)
        {
            lock (aPatternRe)
            {
                var matches = aPatternRe.Matches(aData);
                foreach (Match match in matches)
                {
                    if (match.Success && aCallback != null)
                    {
                        var values = new List<string>();

                        for (var i = 1; i < match.Groups.Count; i++)
                        {
                            values.Add(match.Groups[i].Value.TrimEnd());
                        }

                        aCallback(values);
                    }
                }
            }
        }

        private delegate void ParseHeaderCallback(List<string> aValues);
    }

    // ---------------------------------------------------------------------------------------------------------- WebSocketSslServer
    public delegate X509Certificate GetCertificateEventHandler(WebSocketSslServer aSender, EventArgs aEventArgs);

    public class WebSocketSslServer : WebSocketServer
    {
        public WebSocketSslServer() :
            this(IPAddress.Loopback, Constants.WS_SSL_PORT)
        {
        }

        public WebSocketSslServer(string aListenOn) :
            this(aListenOn, Constants.WS_SSL_PORT)
        {
        }

        public WebSocketSslServer(string aListenOn, int aPort) :
            base(aListenOn, aPort)
        {
        }

        public WebSocketSslServer(IPAddress aListenOn, int aPort) :
            base(aListenOn, aPort)
        {
        }

        public X509Certificate ServerCertificate { get; set; }

        protected override bool IsSecure
        {
            get { return true; }
        }

        public event GetCertificateEventHandler GetServerCertificate;

        protected override void Initialize(IPAddress aListenOn, int aPort)
        {
            base.Initialize(aListenOn, aPort);
            ServerCertificate = null;
        }

        protected virtual void OnGetServerCertificate()
        {
            if (GetServerCertificate != null)
            {
                ServerCertificate = GetServerCertificate(this, EventArgs.Empty);
            }
        }

        private void FireOnGetServerCertificate()
        {
            if (ServerCertificate != null)
            {
                return;
            }

            try
            {
                OnGetServerCertificate();
            }
            catch
            {
                // DO NOTHING
            }
        }

        protected override Stream CreateStream(Socket aSocket)
        {
            FireOnGetServerCertificate();
            if (ServerCertificate == null)
            {
                throw new InvalidOperationException("TaskServer certificate missing!");
            }

            var sslStream = new WebSocketSslStream((WebSocketStream) base.CreateStream(aSocket));
            sslStream.AuthenticateAsServer(ServerCertificate);

            return sslStream;
        }
    }

    // ------------------------------------------------------------------------------------------------------------ WebSocketRequest
    public sealed class WebSocketRequest : IDisposable
    {
        private bool IsDisposed;

        internal WebSocketRequest(WebSocketContext aContext)
        {
            Context = aContext;
            ContentEncoding = null;
            ContentType = null;
            ContentLength = 0;

            Params = new WebSocketNameValueCollection();
            Params.Add(QueryString);
            foreach (Cookie cookie in Cookies)
            {
                Params.Add(cookie.Name, cookie.Value);
            }
            Params.Add(ServerVariables);
            ((WebSocketNameValueCollection) Params).SetReadOnly(true);

            Url = new Uri(ServerVariables[Constants.WS_LOCATION]);

            InputStream = null;

            IsDisposed = false;
        }

        internal WebSocketContext Context { get; set; }

        public string this[string aKey]
        {
            get { return Params[aKey]; }
        }

        public NameValueCollection ServerVariables
        {
            get { return Context.Client.Params.ServerVariables; }
        }

        public NameValueCollection QueryString
        {
            get { return Context.Client.Params.QueryString; }
        }

        public NameValueCollection Params { get; private set; }

        public CookieCollection Cookies
        {
            get { return Context.Client.Params.Cookies; }
        }

        public int ContentLength { get; private set; }
        public string ContentType { get; private set; }
        public Encoding ContentEncoding { get; private set; }
        public Stream InputStream { get; private set; }

        public long TotalBytes
        {
            get { return InputStream != null ? InputStream.Length : 0; }
        }

        public string UserHostAddress
        {
            get { return Context.Client.Address.ToString(); }
        }

        public string UserHostName
        {
            get { return Context.Client.HostName; }
        }

        public bool IsLocal
        {
            get
            {
                return (UserHostAddress == IPAddress.Loopback.ToString() ||
                        UserHostAddress == Context.Client.ServerAddress.ToString());
            }
        }

        public bool IsSecureConnection
        {
            get { return Context.Client.IsSecure; }
        }

        public string Protocol
        {
            get { return Context.Client.Protocol ?? ""; }
        }

        public Uri Url { get; private set; }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                if (InputStream != null)
                {
                    InputStream.Dispose();
                    InputStream = null;
                }

                Url = null;

                if (Params != null)
                {
                    Params.Clear();
                    Params = null;
                }

                ContentType = null;
                ContentEncoding = null;
                Context = null;

                IsDisposed = true;
            }
        }

        ~WebSocketRequest()
        {
            Dispose();
        }

        internal void SetContent(string aContent, bool aIsBinary)
        {
            ContentType = aIsBinary ? "application/octetstream" : "text/plain";
            ContentEncoding = aIsBinary ? Utility.CP437 : Encoding.UTF8;
            ContentLength = aContent != null ? aContent.Length : 0;

            var content = (aContent != null ? ContentEncoding.GetBytes(aContent) : new byte[0]);
            InputStream = new MemoryStream(content, 0, content.Length, false);
        }
    }

    // --------------------------------------------------------------------------------------------- WebSocketSessionClosedException
    public class WebSocketSessionClosedException : WebSocketException
    {
        public WebSocketSessionClosedException(string aMessage) : base(aMessage)
        {
        }
    }

    // ----------------------------------------------------------------------------------------------------------- WebSocketResponse
    public sealed class WebSocketResponse : IDisposable
    {
        private bool IsDisposed;

        internal WebSocketResponse(WebSocketContext aContext)
        {
            Context = aContext;
            IsDisposed = false;
        }

        internal WebSocketContext Context { get; set; }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                Context = null;

                IsDisposed = true;
            }
        }

        ~WebSocketResponse()
        {
            Dispose();
        }

        private void CheckState()
        {
            if (IsDisposed || Context == null || Context.Session == null || Context.Session.IsClosed)
            {
                throw new WebSocketSessionClosedException("Session closed! Unable to complete the write request!");
            }
        }

        public void Write(string aData)
        {
            CheckState();
            Context.Client.Write(aData);
        }

        public void Write(string aFormat, params object[] aData)
        {
            Write(string.Format(aFormat, aData));
        }

        public void WriteLine(string aData)
        {
            Write("{0}\r\n", aData);
        }

        public void WriteLine(string aFormat, params object[] aData)
        {
            WriteLine(string.Format(aFormat, aData));
        }

        public void WriteBinary(string aData)
        {
            CheckState();
            Context.Client.WriteBinary(aData);
        }

        public void WriteBinary(byte[] aData)
        {
            CheckState();
            Context.Client.WriteBinary(aData);
        }

        public void WriteBinary(bool aData)
        {
            WriteBinary(BitConverter.GetBytes(aData));
        }

        public void WriteBinary(char aData)
        {
            WriteBinary(BitConverter.GetBytes(aData));
        }

        public void WriteBinary(byte aData)
        {
            WriteBinary(BitConverter.GetBytes(aData));
        }

        public void WriteBinary(short aData)
        {
            WriteBinary(BitConverter.GetBytes(aData));
        }

        public void WriteBinary(ushort aData)
        {
            WriteBinary(BitConverter.GetBytes(aData));
        }

        public void WriteBinary(int aData)
        {
            WriteBinary(BitConverter.GetBytes(aData));
        }

        public void WriteBinary(uint aData)
        {
            WriteBinary(BitConverter.GetBytes(aData));
        }

        public void WriteBinary(long aData)
        {
            WriteBinary(BitConverter.GetBytes(aData));
        }

        public void WriteBinary(ulong aData)
        {
            WriteBinary(BitConverter.GetBytes(aData));
        }

        public void WriteBinary(float aData)
        {
            WriteBinary(BitConverter.GetBytes(aData));
        }

        public void WriteBinary(double aData)
        {
            WriteBinary(BitConverter.GetBytes(aData));
        }
    }

    // -------------------------------------------------------------------------------------------------------- WebSocketSessionBase
    public class WebSocketSessionBase : NameObjectCollectionBase
    {
        private int UpdateCount;

        public WebSocketSessionBase() : this(true)
        {
        }

        public WebSocketSessionBase(bool aIsSorted)
        {
            UpdateCount = 0;
            IsReadOnly = false;
            IsSorted = aIsSorted;
            IsDirty = false;
        }

        public virtual Guid SessionID
        {
            get { return Guid.Empty; }
        }

        public bool IsSorted { get; private set; }
        public bool IsDirty { get; protected set; }

        public object this[int aIndex]
        {
            get { return BaseGet(aIndex); }
            set
            {
                if (!Equals(BaseGet(aIndex), value))
                {
                    BaseSet(BaseGetKey(aIndex), value);
                    SetDirty();
                }
            }
        }

        public object this[string aKey]
        {
            get { return BaseGet(aKey); }
            set
            {
                if (!Equals(BaseGet(aKey), value))
                {
                    BaseSet(aKey, value);
                    SetDirty();
                }
            }
        }

        public string[] AllKeys
        {
            get { return BaseGetAllKeys(); }
        }

        public object[] AllValues
        {
            get { return BaseGetAllValues(); }
        }

        public bool HasKeys
        {
            get { return BaseHasKeys(); }
        }

        private void BeginUpdate()
        {
            lock (this)
            {
                UpdateCount++;
            }
        }

        private void EndUpdate()
        {
            if (UpdateCount == 0) return;
            lock (this)
            {
                UpdateCount--;
                if (UpdateCount == 0 && Count > 1) Sort();
            }
        }

        public void Add(string aKey, object aValue)
        {
            BeginUpdate();
            try
            {
                BaseAdd(aKey, aValue);
                SetDirty();
            }
            finally
            {
                EndUpdate();
            }
        }

        public void Add(WebSocketSessionBase aCollection)
        {
            BeginUpdate();
            try
            {
                foreach (var key in aCollection.AllKeys)
                {
                    Add(key, aCollection[key]);
                }
            }
            finally
            {
                EndUpdate();
            }
        }

        public void Remove(string aKey)
        {
            BeginUpdate();
            try
            {
                BaseRemove(aKey);
                SetDirty();
            }
            finally
            {
                EndUpdate();
            }
        }

        public void RemoveAt(int aIndex)
        {
            BeginUpdate();
            try
            {
                BaseRemoveAt(aIndex);
                SetDirty();
            }
            finally
            {
                EndUpdate();
            }
        }

        public void Clear()
        {
            BeginUpdate();
            try
            {
                BaseClear();
                SetDirty();
            }
            finally
            {
                EndUpdate();
            }
        }

        public void Sort()
        {
            if (!IsSorted)
            {
                return;
            }

            lock (this)
            {
                var temp = new WebSocketSessionBase(false);

                var keys = AllKeys;
                Array.Sort(keys);
                foreach (var key in keys)
                {
                    temp.Add(key, this[key]);
                }

                var wasDirty = IsDirty;
                try
                {
                    BeginUpdate();
                    try
                    {
                        Clear();
                        Add(temp);
                    }
                    finally
                    {
                        EndUpdate();
                    }
                }
                finally
                {
                    if (wasDirty)
                    {
                        SetDirty();
                    }
                    else
                    {
                        ResetDirty();
                    }
                }
            }
        }

        internal void CopyFrom(WebSocketSessionBase aCollection)
        {
            lock (this)
            {
                BeginUpdate();
                try
                {
                    Clear();
                    if (aCollection.Count > 0) Add(aCollection);
                }
                finally
                {
                    EndUpdate();
                    ResetDirty();
                }
            }
        }

        public void SetDirty()
        {
            if (!IsDirty) IsDirty = true;
        }

        public void ResetDirty()
        {
            IsDirty = false;
        }
    }

    // ------------------------------------------------------------------------------------------------------- WebSocketSessionState
    public sealed class WebSocketSessionState : WebSocketSessionBase, IDisposable
    {
        private bool IsDisposed;

        internal WebSocketSessionState(WebSocketServer aServer, WebSocketClient aClient)
        {
            Server = aServer;
            Client = aClient;
            IsClosed = false;
            IsDisposed = false;
            Restore();
        }

        internal WebSocketClient Client { get; set; }
        public bool IsClosed { get; internal set; }
        public WebSocketServer Server { get; private set; }

        public override Guid SessionID
        {
            get { return (Client != null ? Client.ID : base.SessionID); }
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                Save();
                Clear();

                Client = null;
                Server = null;

                IsDisposed = true;
            }
        }

        ~WebSocketSessionState()
        {
            Dispose();
        }

        private void Restore()
        {
            if (Client != null)
            {
                CopyFrom(Client.Params.Session);
            }
        }

        private void Save()
        {
            if (IsDirty)
            {
                if (Client != null)
                {
                    Client.Params.Session.CopyFrom(this);
                }
                ResetDirty();
            }
        }
    }

    // ------------------------------------------------------------------------------------------------------------ WebSocketContext
    public sealed class WebSocketContext : IDisposable
    {
        private bool IsDisposed;

        internal WebSocketContext(WebSocketServer aServer, WebSocketClient aClient)
        {
            Client = aClient;
            Request = new WebSocketRequest(this);
            Response = new WebSocketResponse(this);
            Session = new WebSocketSessionState(aServer, aClient);

            IsDisposed = false;
        }

        internal WebSocketContext(WebSocketServer aServer, WebSocketClient aClient, string aContent, bool aIsBinary) :
            this(aServer, aClient)
        {
            Request.SetContent(aContent, aIsBinary);
        }

        internal WebSocketClient Client { get; set; }
        public WebSocketRequest Request { get; private set; }
        public WebSocketResponse Response { get; private set; }
        public WebSocketSessionState Session { get; private set; }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                if (Session != null)
                {
                    Session.Dispose();
                    Session = null;
                }

                if (Response != null)
                {
                    Response.Dispose();
                    Response = null;
                }

                if (Request != null)
                {
                    Request.Dispose();
                    Request = null;
                }

                Client = null;

                IsDisposed = true;
            }
        }

        ~WebSocketContext()
        {
            Dispose();
        }
    }

    // ----------------------------------------------------------------------------------------------------------- IWebSocketService
    public interface IWebSocketService
    {
        void SessionOpen(WebSocketContext aContext);
        void SessionClose(WebSocketContext aContext);
        void ProcessRequest(WebSocketContext aContext);
    }

    // -------------------------------------------------------------------------------------------------------- WebSocketServiceHost
    public class WebSocketServiceHost : IDisposable
    {
        private static readonly Regex EndPointRe = new Regex(@"^(.*):\/\/(.*)$",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private bool HandlersActive;
        private bool IsDisposed;

        public WebSocketServiceHost(WebSocketServer aServer)
        {
            if (aServer == null)
            {
                throw new ArgumentNullException("WebSocketServer instance is null!");
            }

            Server = aServer;
            Services = new Dictionary<string, IWebSocketService>();

            IsDisposed = false;
            HandlersActive = false;
        }

        public WebSocketServer Server { get; private set; }

        public virtual string DefaultProtocolName
        {
            get { return "default"; }
        }

        protected Dictionary<string, IWebSocketService> Services { get; private set; }

        public virtual void Dispose()
        {
            if (!IsDisposed)
            {
                if (Server != null)
                {
                    Close();
                    Server = null;
                }

                if (Services != null)
                {
                    RemoveAllServices();
                    Services = null;
                }

                IsDisposed = true;
            }
        }

        ~WebSocketServiceHost()
        {
            Dispose();
        }

        protected void CheckDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(
                    "Illegal operation. This instance of WebSocketServiceHost has been disposed!");
            }
        }

        private void AttachHandlers()
        {
            if (!HandlersActive)
            {
                Server.ClientConnected += SessionOpen;
                Server.ClientDisconnected += SessionClose;

                HandlersActive = true;
            }
        }

        private void DetachHandlers()
        {
            if (HandlersActive)
            {
                Server.ClientDisconnected -= SessionClose;
                Server.ClientConnected -= SessionOpen;

                HandlersActive = false;
            }
        }

        public virtual void Open()
        {
            CheckDisposed();
            AttachHandlers();
            Server.Start();
        }

        public virtual void Close()
        {
            CheckDisposed();
            Server.Stop();
            DetachHandlers();
        }

        private string NormalizeEndPoint(string aEndPoint)
        {
            var result = (aEndPoint ?? "").Trim();
            if (result == string.Empty)
            {
                result = "/";
            }
            return result;
        }

        private string RemoveLeadingSlash(string aEndPoint)
        {
            if (aEndPoint.StartsWith("/"))
            {
                return aEndPoint.Substring(1);
            }
            return aEndPoint;
        }

        private string CreateEndPoint(string aEndPoint)
        {
            return CreateEndPoint(aEndPoint, null);
        }

        private string CreateEndPoint(string aEndPoint, string aProtocol)
        {
            return string.Format("{0}://{1}",
                string.IsNullOrEmpty(aProtocol) ? DefaultProtocolName : aProtocol,
                aEndPoint == null ? "" : RemoveLeadingSlash(aEndPoint.Trim()));
        }

        private void ParseEndPoint(string aEndPoint, ParseEndPointCallback aCallback)
        {
            lock (EndPointRe)
            {
                var matches = EndPointRe.Matches(aEndPoint);
                foreach (Match match in matches)
                {
                    if (match.Success && aCallback != null)
                    {
                        var values = new List<string>();

                        for (var i = 1; i < match.Groups.Count; i++)
                        {
                            values.Add(match.Groups[i].Value.Trim());
                        }

                        aCallback(values);
                    }
                }
            }
        }

        private string ParseAndCreateEndPoint(string aEndPoint)
        {
            string endPoint = NormalizeEndPoint(aEndPoint), protocol = string.Empty;

            ParseEndPoint(endPoint, delegate(List<string> aValues)
            {
                if (aValues.Count > 0)
                {
                    protocol = aValues[0];
                }

                if (aValues.Count > 1)
                {
                    endPoint = aValues[1];
                }

                if (string.IsNullOrEmpty(protocol))
                {
                    protocol = DefaultProtocolName;
                }

                if (string.IsNullOrEmpty(endPoint))
                {
                    endPoint = "/";
                }
            });

            return CreateEndPoint(endPoint, protocol);
        }

        public void AddService(string aEndPoint, IWebSocketService aService)
        {
            CheckDisposed();

            if (aService == null || string.IsNullOrEmpty(aEndPoint))
            {
                throw new ArgumentNullException();
            }

            var endPoint = ParseAndCreateEndPoint(aEndPoint);
            lock (Services)
            {
                if (!Services.ContainsKey(endPoint))
                {
                    Services.Add(aEndPoint, aService);
                }
            }
        }

        public void AddService(string aEndPoint, Type aType)
        {
            CheckDisposed();

            if (aType == null)
            {
                throw new ArgumentNullException();
            }

            if (aType.GetInterface(typeof (IWebSocketService).ToString()) == null)
            {
                throw new NotSupportedException();
            }

            var service = Activator.CreateInstance(aType) as IWebSocketService;
            if (service != null)
            {
                try
                {
                    AddService(aEndPoint, service);
                }
                catch
                {
                    service = null;
                    throw;
                }
            }
        }

        public void RemoveService(string aEndPoint)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(aEndPoint))
            {
                throw new ArgumentNullException();
            }

            var endPoint = ParseAndCreateEndPoint(aEndPoint);
            lock (Services)
            {
                if (Services.ContainsKey(endPoint))
                {
                    Services[endPoint] = null;
                    Services.Remove(endPoint);
                }
            }
        }

        public void RemoveAllServices()
        {
            lock (Services)
            {
                List<string> endPoints = new List<string>();
                foreach (string endPoint in Services.Keys)
                {
                    //Services[endPoint] = null;
                    endPoints.Add(endPoint);
                }
                foreach (string endPoint in endPoints)
                {
                    Services[endPoint] = null;
                }

                Services.Clear();
            }
        }

        public IWebSocketService GetService(string aEndPoint)
        {
            CheckDisposed();

            return GetService(aEndPoint, null);
        }

        public IWebSocketService GetService(string aEndPoint, string aProtocol)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(aEndPoint))
            {
                throw new ArgumentNullException();
            }

            IWebSocketService result = null;
            aEndPoint = NormalizeEndPoint(aEndPoint);

            lock (Services)
            {
                // First try endpoint/protocol ...
                var endPoint = CreateEndPoint(aEndPoint, aProtocol);

                if (!Services.ContainsKey(endPoint) && !string.IsNullOrEmpty(aProtocol))
                {
                    // Not found? try default endpoint ...
                    endPoint = CreateEndPoint(aEndPoint);
                }

                if (Services.ContainsKey(endPoint))
                {
                    result = Services[endPoint];
                }
            }

            if (result == null)
            {
                throw new ArgumentOutOfRangeException();
            }

            return result;
        }

        private void SessionOpen(WebSocketClient aSender, EventArgs aArgs)
        {
            // attach handler
            aSender.FrameReceived += SessionDispatcher;

            try
            {
                var service = GetService(aSender.EndPoint, aSender.Protocol);
                using (var context = new WebSocketContext(Server, aSender))
                {
                    try
                    {
                        service.SessionOpen(context);
                    }
                    catch
                    {
                        // DO NOTHING
                    }
                }
            }
            catch
            {
                // DO NOTHING
            }
        }

        private void SessionClose(WebSocketClient aSender, EventArgs aArgs)
        {
            try
            {
                var service = GetService(aSender.EndPoint, aSender.Protocol);
                using (var context = new WebSocketContext(Server, aSender))
                {
                    context.Session.IsClosed = true;
                    try
                    {
                        service.SessionClose(context);
                    }
                    catch
                    {
                        // DO NOTHING
                    }
                }
            }
            catch
            {
                // DO NOTHING
            }

            // detach handler
            aSender.FrameReceived -= SessionDispatcher;
        }

        private void SessionDispatcher(WebSocketClient aSender, FrameReceivedArgs aArgs)
        {
            try
            {
                var service = GetService(aSender.EndPoint, aSender.Protocol);
                using (var context = new WebSocketContext(Server, aSender, aArgs.Data, aArgs.IsBinary))
                {
                    try
                    {
                        service.ProcessRequest(context);
                    }
                    catch
                    {
                        // DO NOTHING
                    }
                }
            }
            catch
            {
                // DO NOTHING
            }
        }

        private delegate void ParseEndPointCallback(List<string> aValues);
    }

    // --------------------------------------------------------------------------------------------------------------------- Utility
    internal static class Utility
    {


        private static readonly MD5CryptoServiceProvider Hasher = new MD5CryptoServiceProvider();
        internal static Encoding CP437 = Encoding.GetEncoding(437);

        internal static uint ToBigEndian(uint aValue)
        {
            if (BitConverter.IsLittleEndian)
            {
                return aValue >> 24 | ((aValue << 8) & 0x00ff0000) | ((aValue >> 8) & 0x0000ff00) | (aValue << 24);
            }
            return aValue;
        }

        internal static byte[] MD5(byte[] aBytes)
        {
            return Hasher.ComputeHash(aBytes);
        }

        internal static int Min(int aLeft, int aRight)
        {
            return aLeft > aRight ? aRight : aLeft;
        }

        internal static string ResolveHost(IPAddress aAddress)
        {
            var result = string.Empty;
            try
            {
                var hostEntry = Dns.GetHostEntry(aAddress);
                result = hostEntry.HostName;
            }
            catch
            {
                // DO NOTHING
            }
            return result;
        }
    }
}