using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Dym.Libs.WebSocketLib.Net
{
    public sealed class HttpConnection
    {
        private byte[] _buffer;
        private const int _bufferLength = 8192;
        private HttpListenerContext _context;
        private bool _contextRegistered;
        private StringBuilder _currentLine;
        private InputState _inputState;
        private RequestStream _inputStream;
        private HttpListener _lastListener;
        private LineState _lineState;
        private readonly EndPointListener _listener;
        private readonly EndPoint _localEndPoint;
        private ResponseStream _outputStream;
        private int _position;
        private readonly EndPoint _remoteEndPoint;
        private MemoryStream _requestBuffer;
        private Socket _socket;
        private readonly object _sync;
        private int _timeout;
        private readonly Dictionary<int, bool> _timeoutCanceled;
        private Timer _timer;

        public HttpConnection(Socket socket, EndPointListener listener)
        {
            _socket = socket;
            _listener = listener;

            var netStream = new NetworkStream(socket, false);
            if (listener.IsSecure)
            {
                var sslConf = listener.SslConfiguration;
                var sslStream = new SslStream(
                                  netStream,
                                  false,
                                  sslConf.ClientCertificateValidationCallback
                                );

                sslStream.AuthenticateAsServer(
                  sslConf.ServerCertificate,
                  sslConf.ClientCertificateRequired,
                  sslConf.EnabledSslProtocols,
                  sslConf.CheckCertificateRevocation
                );

                IsSecure = true;
                Stream = sslStream;
            }
            else
            {
                Stream = netStream;
            }

            _localEndPoint = socket.LocalEndPoint;
            _remoteEndPoint = socket.RemoteEndPoint;
            _sync = new object();
            _timeout = 90000; // 90k ms for first request, 15k ms from then on.
            _timeoutCanceled = new Dictionary<int, bool>();
            _timer = new Timer(onTimeout, this, Timeout.Infinite, Timeout.Infinite);

            init();
        }

        public bool IsClosed => _socket == null;

        public bool IsLocal => ((IPEndPoint)_remoteEndPoint).Address.IsLocal();

        public bool IsSecure { get; }

        public IPEndPoint LocalEndPoint => (IPEndPoint)_localEndPoint;

        public IPEndPoint RemoteEndPoint => (IPEndPoint)_remoteEndPoint;

        public int Reuses { get; private set; }

        public Stream Stream { get; private set; }

        private void close()
        {
            lock (_sync)
            {
                if (_socket == null)
                    return;

                disposeTimer();
                disposeRequestBuffer();
                disposeStream();
                closeSocket();
            }

            unregisterContext();
            removeConnection();
        }

        private void closeSocket()
        {
            try
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
            catch
            {
            }

            _socket.Close();
            _socket = null;
        }

        private void disposeRequestBuffer()
        {
            if (_requestBuffer == null)
                return;

            _requestBuffer.Dispose();
            _requestBuffer = null;
        }

        private void disposeStream()
        {
            if (Stream == null)
                return;

            _inputStream = null;
            _outputStream = null;

            Stream.Dispose();
            Stream = null;
        }

        private void disposeTimer()
        {
            if (_timer == null)
                return;

            try
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            catch
            {
            }

            _timer.Dispose();
            _timer = null;
        }

        private void init()
        {
            _context = new HttpListenerContext(this);
            _inputState = InputState.RequestLine;
            _inputStream = null;
            _lineState = LineState.None;
            _outputStream = null;
            _position = 0;
            _requestBuffer = new MemoryStream();
        }

        private static void onRead(IAsyncResult asyncResult)
        {
            var conn = (HttpConnection)asyncResult.AsyncState;
            if (conn._socket == null)
                return;

            lock (conn._sync)
            {
                if (conn._socket == null)
                    return;

                var nread = -1;
                var len = 0;
                try
                {
                    var current = conn.Reuses;
                    if (!conn._timeoutCanceled[current])
                    {
                        conn._timer.Change(Timeout.Infinite, Timeout.Infinite);
                        conn._timeoutCanceled[current] = true;
                    }

                    nread = conn.Stream.EndRead(asyncResult);
                    conn._requestBuffer.Write(conn._buffer, 0, nread);
                    len = (int)conn._requestBuffer.Length;
                }
                catch (Exception ex)
                {
                    if (conn._requestBuffer != null && conn._requestBuffer.Length > 0)
                    {
                        conn.SendError(ex.Message, 400);
                        return;
                    }

                    conn.close();
                    return;
                }

                if (nread <= 0)
                {
                    conn.close();
                    return;
                }

                if (conn.processInput(conn._requestBuffer.GetBuffer(), len))
                {
                    if (!conn._context.HasError)
                        conn._context.Request.FinishInitialization();

                    if (conn._context.HasError)
                    {
                        conn.SendError();
                        return;
                    }

                    HttpListener lsnr;
                    if (!conn._listener.TrySearchHttpListener(conn._context.Request.Url, out lsnr))
                    {
                        conn.SendError(null, 404);
                        return;
                    }

                    if (conn._lastListener != lsnr)
                    {
                        conn.removeConnection();
                        if (!lsnr.AddConnection(conn))
                        {
                            conn.close();
                            return;
                        }

                        conn._lastListener = lsnr;
                    }

                    conn._context.Listener = lsnr;
                    if (!conn._context.Authenticate())
                        return;

                    if (conn._context.Register())
                        conn._contextRegistered = true;

                    return;
                }

                conn.Stream.BeginRead(conn._buffer, 0, _bufferLength, onRead, conn);
            }
        }

        private static void onTimeout(object state)
        {
            var conn = (HttpConnection)state;
            var current = conn.Reuses;
            if (conn._socket == null)
                return;

            lock (conn._sync)
            {
                if (conn._socket == null)
                    return;

                if (conn._timeoutCanceled[current])
                    return;

                conn.SendError(null, 408);
            }
        }

        // true -> Done processing.
        // false -> Need more input.
        private bool processInput(byte[] data, int length)
        {
            if (_currentLine == null)
                _currentLine = new StringBuilder(64);

            var nread = 0;
            try
            {
                string line;
                while ((line = readLineFrom(data, _position, length, out nread)) != null)
                {
                    _position += nread;
                    if (line.Length == 0)
                    {
                        if (_inputState == InputState.RequestLine)
                            continue;

                        if (_position > 32768)
                            _context.ErrorMessage = "Headers too long";

                        _currentLine = null;
                        return true;
                    }

                    if (_inputState == InputState.RequestLine)
                    {
                        _context.Request.SetRequestLine(line);
                        _inputState = InputState.Headers;
                    }
                    else
                    {
                        _context.Request.AddHeader(line);
                    }

                    if (_context.HasError)
                        return true;
                }
            }
            catch (Exception ex)
            {
                _context.ErrorMessage = ex.Message;
                return true;
            }

            _position += nread;
            if (_position >= 32768)
            {
                _context.ErrorMessage = "Headers too long";
                return true;
            }

            return false;
        }

        private string readLineFrom(byte[] buffer, int offset, int length, out int read)
        {
            read = 0;

            for (var i = offset; i < length && _lineState != LineState.Lf; i++)
            {
                read++;

                var b = buffer[i];
                if (b == 13)
                    _lineState = LineState.Cr;
                else if (b == 10)
                    _lineState = LineState.Lf;
                else
                    _currentLine.Append((char)b);
            }

            if (_lineState != LineState.Lf)
                return null;

            var line = _currentLine.ToString();

            _currentLine.Length = 0;
            _lineState = LineState.None;

            return line;
        }

        private void removeConnection()
        {
            if (_lastListener != null)
                _lastListener.RemoveConnection(this);
            else
                _listener.RemoveConnection(this);
        }

        private void unregisterContext()
        {
            if (!_contextRegistered)
                return;

            _context.Unregister();
            _contextRegistered = false;
        }

        public void Close(bool force = false)
        {
            if (_socket == null)
                return;

            lock (_sync)
            {
                if (_socket == null)
                    return;

                if (force)
                {
                    if (_outputStream != null)
                        _outputStream.Close(true);

                    close();
                    return;
                }

                GetResponseStream().Close(false);

                if (_context.Response.CloseConnection)
                {
                    close();
                    return;
                }

                if (!_context.Request.FlushInput())
                {
                    close();
                    return;
                }

                disposeRequestBuffer();
                unregisterContext();
                init();

                Reuses++;
                BeginReadRequest();
            }
        }

        public void BeginReadRequest()
        {
            if (_buffer == null)
                _buffer = new byte[_bufferLength];

            if (Reuses == 1)
                _timeout = 15000;

            try
            {
                _timeoutCanceled.Add(Reuses, false);
                _timer.Change(_timeout, Timeout.Infinite);
                Stream.BeginRead(_buffer, 0, _bufferLength, onRead, this);
            }
            catch
            {
                close();
            }
        }

        public RequestStream GetRequestStream(long contentLength, bool chunked)
        {
            lock (_sync)
            {
                if (_socket == null)
                    return null;

                if (_inputStream != null)
                    return _inputStream;

                var buff = _requestBuffer.GetBuffer();
                var len = (int)_requestBuffer.Length;
                var cnt = len - _position;
                disposeRequestBuffer();

                _inputStream = chunked
                               ? new ChunkedRequestStream(
                                   Stream, buff, _position, cnt, _context
                                 )
                               : new RequestStream(
                                   Stream, buff, _position, cnt, contentLength
                                 );

                return _inputStream;
            }
        }

        public ResponseStream GetResponseStream()
        {
            // TODO: Can we get this stream before reading the input?

            lock (_sync)
            {
                if (_socket == null)
                    return null;

                if (_outputStream != null)
                    return _outputStream;

                var lsnr = _context.Listener;
                var ignore = lsnr?.IgnoreWriteExceptions ?? true;
                _outputStream = new ResponseStream(Stream, _context.Response, ignore);

                return _outputStream;
            }
        }

        public void SendError()
        {
            SendError(_context.ErrorMessage, _context.ErrorStatus);
        }

        public void SendError(string message, int status)
        {
            if (_socket == null)
                return;

            lock (_sync)
            {
                if (_socket == null)
                    return;

                try
                {
                    var res = _context.Response;
                    res.StatusCode = status;
                    res.ContentType = "text/html";

                    var content = new StringBuilder(64);
                    content.AppendFormat("<html><body><h1>{0} {1}", status, res.StatusDescription);
                    if (!string.IsNullOrEmpty(message))
                        content.AppendFormat(" ({0})</h1></body></html>", message);
                    else
                        content.Append("</h1></body></html>");

                    var enc = Encoding.UTF8;
                    var entity = enc.GetBytes(content.ToString());
                    res.ContentEncoding = enc;
                    res.ContentLength64 = entity.LongLength;

                    res.Close(entity, true);
                }
                catch
                {
                    Close(true);
                }
            }
        }
    }
}
