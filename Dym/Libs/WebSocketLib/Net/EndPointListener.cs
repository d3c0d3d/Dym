using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Dym.Libs.WebSocketLib.Net
{
    public sealed class EndPointListener
    {
        private List<HttpListenerPrefix> _all; // host == '+'
        private static readonly string _defaultCertFolderPath;
        private readonly IPEndPoint _endpoint;
        private Dictionary<HttpListenerPrefix, HttpListener> _prefixes;
        private readonly Socket _socket;
        private List<HttpListenerPrefix> _unhandled; // host == '*'
        private readonly Dictionary<HttpConnection, HttpConnection> _unregistered;
        private readonly object _unregisteredSync;

        static EndPointListener()
        {
            _defaultCertFolderPath =
              Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

        public EndPointListener(
        IPEndPoint endpoint,
        bool secure,
        string certificateFolderPath,
        ServerSslConfiguration sslConfig,
        bool reuseAddress
      )
        {
            if (secure)
            {
                var cert =
                  getCertificate(endpoint.Port, certificateFolderPath, sslConfig.ServerCertificate);

                if (cert == null)
                    throw new ArgumentException("No server certificate could be found.");

                IsSecure = true;
                SslConfiguration = new ServerSslConfiguration(sslConfig) {ServerCertificate = cert};
            }

            _endpoint = endpoint;
            _prefixes = new Dictionary<HttpListenerPrefix, HttpListener>();
            _unregistered = new Dictionary<HttpConnection, HttpConnection>();
            _unregisteredSync = ((ICollection)_unregistered).SyncRoot;
            _socket =
              new Socket(endpoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            if (reuseAddress)
                _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            _socket.Bind(endpoint);
            _socket.Listen(500);
            _socket.BeginAccept(onAccept, this);
        }

        public IPAddress Address => _endpoint.Address;

        public bool IsSecure { get; }

        public int Port => _endpoint.Port;

        public ServerSslConfiguration SslConfiguration { get; }

        private static void addSpecial(ICollection<HttpListenerPrefix> prefixes, HttpListenerPrefix prefix)
        {
            var path = prefix.Path;
            foreach (var pref in prefixes)
            {
                if (pref.Path == path)
                    throw new HttpListenerException(87, "The prefix is already in use.");
            }

            prefixes.Add(prefix);
        }

        private static RSACryptoServiceProvider createRSAFromFile(string filename)
        {
            byte[] pvk = null;
            using (var fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                pvk = new byte[fs.Length];
                fs.Read(pvk, 0, pvk.Length);
            }

            var rsa = new RSACryptoServiceProvider();
            rsa.ImportCspBlob(pvk);

            return rsa;
        }

        private static X509Certificate2 getCertificate(
          int port, string folderPath, X509Certificate2 defaultCertificate
        )
        {
            if (string.IsNullOrEmpty(folderPath))
                folderPath = _defaultCertFolderPath;

            try
            {
                var cer = Path.Combine(folderPath, $"{port}.cer");
                var key = Path.Combine(folderPath, $"{port}.key");
                if (File.Exists(cer) && File.Exists(key))
                {
                    var cert = new X509Certificate2(cer) {PrivateKey = createRSAFromFile(key)};

                    return cert;
                }
            }
            catch
            {
            }

            return defaultCertificate;
        }

        private void leaveIfNoPrefix()
        {
            if (_prefixes.Count > 0)
                return;

            var prefs = _unhandled;
            if (prefs != null && prefs.Count > 0)
                return;

            prefs = _all;
            if (prefs != null && prefs.Count > 0)
                return;

            EndPointManager.RemoveEndPoint(_endpoint);
        }

        private static void onAccept(IAsyncResult asyncResult)
        {
            var lsnr = (EndPointListener)asyncResult.AsyncState;

            Socket sock = null;
            try
            {
                sock = lsnr._socket.EndAccept(asyncResult);
            }
            catch (SocketException)
            {
                // TODO: Should log the error code when this class has a logging.
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            try
            {
                lsnr._socket.BeginAccept(onAccept, lsnr);
            }
            catch
            {
                sock?.Close();

                return;
            }

            if (sock == null)
                return;

            processAccepted(sock, lsnr);
        }

        private static void processAccepted(Socket socket, EndPointListener listener)
        {
            HttpConnection conn = null;
            try
            {
                conn = new HttpConnection(socket, listener);
                lock (listener._unregisteredSync)
                    listener._unregistered[conn] = conn;

                conn.BeginReadRequest();
            }
            catch
            {
                if (conn != null)
                {
                    conn.Close(true);
                    return;
                }

                socket.Close();
            }
        }

        private static bool removeSpecial(List<HttpListenerPrefix> prefixes, HttpListenerPrefix prefix)
        {
            var path = prefix.Path;
            var cnt = prefixes.Count;
            for (var i = 0; i < cnt; i++)
            {
                if (prefixes[i].Path == path)
                {
                    prefixes.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        private static HttpListener searchHttpListenerFromSpecial(
          string path, List<HttpListenerPrefix> prefixes
        )
        {
            if (prefixes == null)
                return null;

            HttpListener bestMatch = null;

            var bestLen = -1;
            foreach (var pref in prefixes)
            {
                var prefPath = pref.Path;

                var len = prefPath.Length;
                if (len < bestLen)
                    continue;

                if (path.StartsWith(prefPath))
                {
                    bestLen = len;
                    bestMatch = pref.Listener;
                }
            }

            return bestMatch;
        }

        public static bool CertificateExists(int port, string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
                folderPath = _defaultCertFolderPath;

            var cer = Path.Combine(folderPath, $"{port}.cer");
            var key = Path.Combine(folderPath, $"{port}.key");

            return File.Exists(cer) && File.Exists(key);
        }

        public void RemoveConnection(HttpConnection connection)
        {
            lock (_unregisteredSync)
                _unregistered.Remove(connection);
        }

        public bool TrySearchHttpListener(Uri uri, out HttpListener listener)
        {
            listener = null;

            if (uri == null)
                return false;

            var host = uri.Host;
            var dns = Uri.CheckHostName(host) == UriHostNameType.Dns;
            var port = uri.Port.ToString();
            var path = HttpUtility.UrlDecode(uri.AbsolutePath);
            var pathSlash = path[path.Length - 1] != '/' ? path + "/" : path;

            if (host != null && host.Length > 0)
            {
                var bestLen = -1;
                foreach (var pref in _prefixes.Keys)
                {
                    if (dns)
                    {
                        var prefHost = pref.Host;
                        if (Uri.CheckHostName(prefHost) == UriHostNameType.Dns && prefHost != host)
                            continue;
                    }

                    if (pref.Port != port)
                        continue;

                    var prefPath = pref.Path;

                    var len = prefPath.Length;
                    if (len < bestLen)
                        continue;

                    if (path.StartsWith(prefPath) || pathSlash.StartsWith(prefPath))
                    {
                        bestLen = len;
                        listener = _prefixes[pref];
                    }
                }

                if (bestLen != -1)
                    return true;
            }

            var prefs = _unhandled;
            listener = searchHttpListenerFromSpecial(path, prefs);
            if (listener == null && pathSlash != path)
                listener = searchHttpListenerFromSpecial(pathSlash, prefs);

            if (listener != null)
                return true;

            prefs = _all;
            listener = searchHttpListenerFromSpecial(path, prefs);
            if (listener == null && pathSlash != path)
                listener = searchHttpListenerFromSpecial(pathSlash, prefs);

            return listener != null;
        }

        public void AddPrefix(HttpListenerPrefix prefix, HttpListener listener)
        {
            List<HttpListenerPrefix> current, future;
            if (prefix.Host == "*")
            {
                do
                {
                    current = _unhandled;
                    future = current != null
                             ? new List<HttpListenerPrefix>(current)
                             : new List<HttpListenerPrefix>();

                    prefix.Listener = listener;
                    addSpecial(future, prefix);
                }
                while (Interlocked.CompareExchange(ref _unhandled, future, current) != current);

                return;
            }

            if (prefix.Host == "+")
            {
                do
                {
                    current = _all;
                    future = current != null
                             ? new List<HttpListenerPrefix>(current)
                             : new List<HttpListenerPrefix>();

                    prefix.Listener = listener;
                    addSpecial(future, prefix);
                }
                while (Interlocked.CompareExchange(ref _all, future, current) != current);

                return;
            }

            Dictionary<HttpListenerPrefix, HttpListener> prefs, prefs2;
            do
            {
                prefs = _prefixes;
                if (prefs.ContainsKey(prefix))
                {
                    if (prefs[prefix] != listener)
                    {
                        throw new HttpListenerException(
                          87, $"There's another listener for {prefix}."
                        );
                    }

                    return;
                }

                prefs2 = new Dictionary<HttpListenerPrefix, HttpListener>(prefs);
                prefs2[prefix] = listener;
            }
            while (Interlocked.CompareExchange(ref _prefixes, prefs2, prefs) != prefs);
        }

        public void Close()
        {
            _socket.Close();

            HttpConnection[] conns = null;
            lock (_unregisteredSync)
            {
                if (_unregistered.Count == 0)
                    return;

                var keys = _unregistered.Keys;
                conns = new HttpConnection[keys.Count];
                keys.CopyTo(conns, 0);
                _unregistered.Clear();
            }

            for (var i = conns.Length - 1; i >= 0; i--)
                conns[i].Close(true);
        }

        public void RemovePrefix(HttpListenerPrefix prefix, HttpListener listener)
        {
            List<HttpListenerPrefix> current, future;
            if (prefix.Host == "*")
            {
                do
                {
                    current = _unhandled;
                    if (current == null)
                        break;

                    future = new List<HttpListenerPrefix>(current);
                    if (!removeSpecial(future, prefix))
                        break; // The prefix wasn't found.
                }
                while (Interlocked.CompareExchange(ref _unhandled, future, current) != current);

                leaveIfNoPrefix();
                return;
            }

            if (prefix.Host == "+")
            {
                do
                {
                    current = _all;
                    if (current == null)
                        break;

                    future = new List<HttpListenerPrefix>(current);
                    if (!removeSpecial(future, prefix))
                        break; // The prefix wasn't found.
                }
                while (Interlocked.CompareExchange(ref _all, future, current) != current);

                leaveIfNoPrefix();
                return;
            }

            Dictionary<HttpListenerPrefix, HttpListener> prefs, prefs2;
            do
            {
                prefs = _prefixes;
                if (!prefs.ContainsKey(prefix))
                    break;

                prefs2 = new Dictionary<HttpListenerPrefix, HttpListener>(prefs);
                prefs2.Remove(prefix);
            }
            while (Interlocked.CompareExchange(ref _prefixes, prefs2, prefs) != prefs);

            leaveIfNoPrefix();
        }
    }
}
