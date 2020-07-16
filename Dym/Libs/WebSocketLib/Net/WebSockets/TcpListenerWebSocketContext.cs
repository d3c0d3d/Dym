using Dym.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Principal;
using System.Text;

namespace Dym.Libs.WebSocketLib.Net.WebSockets
{
    /// <summary>
    /// Provides the access to the information in a WebSocket handshake request to
    /// a <see cref="TcpListener"/> instance.
    /// </summary>
    public class TcpListenerWebSocketContext : WebSocketContext
    {
        private NameValueCollection _queryString;
        private HttpRequest _request;
        private Uri _requestUri;
        private readonly bool _secure;
        private readonly System.Net.EndPoint _serverEndPoint;
        private readonly TcpClient _tcpClient;
        private IPrincipal _user;
        private readonly System.Net.EndPoint _userEndPoint;

        public TcpListenerWebSocketContext(
          TcpClient tcpClient,
          string protocol,
          bool secure,
          ServerSslConfiguration sslConfig,
          Logger log
        )
        {
            _tcpClient = tcpClient;
            _secure = secure;
            Log = log;

            var netStream = tcpClient.GetStream();
            if (secure)
            {
                var sslStream = new SslStream(
                                  netStream,
                                  false,
                                  sslConfig.ClientCertificateValidationCallback
                                );

                sslStream.AuthenticateAsServer(
                  sslConfig.ServerCertificate,
                  sslConfig.ClientCertificateRequired,
                  sslConfig.EnabledSslProtocols,
                  sslConfig.CheckCertificateRevocation
                );

                Stream = sslStream;
            }
            else
            {
                Stream = netStream;
            }

            var sock = tcpClient.Client;
            _serverEndPoint = sock.LocalEndPoint;
            _userEndPoint = sock.RemoteEndPoint;

            _request = HttpRequest.Read(Stream, 90000);
            WebSocket = new WebSocket(this, protocol);
        }

        public Logger Log { get; }

        public Stream Stream { get; }

        /// <summary>
        /// Gets the HTTP cookies included in the handshake request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="Net.CookieCollection"/> that contains
        ///   the cookies.
        ///   </para>
        ///   <para>
        ///   An empty collection if not included.
        ///   </para>
        /// </value>
        public override CookieCollection CookieCollection => _request.Cookies;

        /// <summary>
        /// Gets the HTTP headers included in the handshake request.
        /// </summary>
        /// <value>
        /// A <see cref="NameValueCollection"/> that contains the headers.
        /// </value>
        public override NameValueCollection Headers => _request.Headers;

        /// <summary>
        /// Gets the value of the Host header included in the handshake request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the server host name requested
        ///   by the client.
        ///   </para>
        ///   <para>
        ///   It includes the port number if provided.
        ///   </para>
        /// </value>
        public override string Host => _request.Headers["Host"];

        /// <summary>
        /// Gets a value indicating whether the client is authenticated.
        /// </summary>
        /// <value>
        /// <c>true</c> if the client is authenticated; otherwise, <c>false</c>.
        /// </value>
        public override bool IsAuthenticated => _user != null;

        /// <summary>
        /// Gets a value indicating whether the handshake request is sent from
        /// the local computer.
        /// </summary>
        /// <value>
        /// <c>true</c> if the handshake request is sent from the same computer
        /// as the server; otherwise, <c>false</c>.
        /// </value>
        public override bool IsLocal => UserEndPoint.Address.IsLocal();

        /// <summary>
        /// Gets a value indicating whether a secure connection is used to send
        /// the handshake request.
        /// </summary>
        /// <value>
        /// <c>true</c> if the connection is secure; otherwise, <c>false</c>.
        /// </value>
        public override bool IsSecureConnection => _secure;

        /// <summary>
        /// Gets a value indicating whether the request is a WebSocket handshake
        /// request.
        /// </summary>
        /// <value>
        /// <c>true</c> if the request is a WebSocket handshake request; otherwise,
        /// <c>false</c>.
        /// </value>
        public override bool IsWebSocketRequest => _request.IsWebSocketRequest;

        /// <summary>
        /// Gets the value of the Origin header included in the handshake request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the value of the Origin header.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if the header is not present.
        ///   </para>
        /// </value>
        public override string Origin => _request.Headers["Origin"];

        /// <summary>
        /// Gets the query string included in the handshake request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="NameValueCollection"/> that contains the query
        ///   parameters.
        ///   </para>
        ///   <para>
        ///   An empty collection if not included.
        ///   </para>
        /// </value>
        public override NameValueCollection QueryString
        {
            get
            {
                if (_queryString == null)
                {
                    var uri = RequestUri;
                    _queryString = HttpUtility.InternalParseQueryString(
                                     uri != null ? uri.Query : null,
                                     Encoding.UTF8
                                   );
                }

                return _queryString;
            }
        }

        /// <summary>
        /// Gets the URI requested by the client.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="Uri"/> that represents the URI parsed from the request.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if the URI cannot be parsed.
        ///   </para>
        /// </value>
        public override Uri RequestUri => _requestUri ?? (_requestUri = HttpUtility.CreateRequestUrl(
                                              _request.RequestUri,
                                              _request.Headers["Host"],
                                              _request.IsWebSocketRequest,
                                              _secure
                                          ));

        /// <summary>
        /// Gets the value of the Sec-WebSocket-Key header included in
        /// the handshake request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the value of
        ///   the Sec-WebSocket-Key header.
        ///   </para>
        ///   <para>
        ///   The value is used to prove that the server received
        ///   a valid WebSocket handshake request.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if the header is not present.
        ///   </para>
        /// </value>
        public override string SecWebSocketKey => _request.Headers["Sec-WebSocket-Key"];

        /// <summary>
        /// Gets the names of the subprotocols from the Sec-WebSocket-Protocol
        /// header included in the handshake request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   An <see cref="T:System.Collections.Generic.IEnumerable{string}"/>
        ///   instance.
        ///   </para>
        ///   <para>
        ///   It provides an enumerator which supports the iteration over
        ///   the collection of the names of the subprotocols.
        ///   </para>
        /// </value>
        public override IEnumerable<string> SecWebSocketProtocols
        {
            get
            {
                var val = _request.Headers["Sec-WebSocket-Protocol"];
                if (string.IsNullOrEmpty(val))
                    yield break;

                foreach (var elm in val.Split(','))
                {
                    var protocol = elm.Trim();
                    if (protocol.Length == 0)
                        continue;

                    yield return protocol;
                }
            }
        }

        /// <summary>
        /// Gets the value of the Sec-WebSocket-Version header included in
        /// the handshake request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the WebSocket protocol
        ///   version specified by the client.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if the header is not present.
        ///   </para>
        /// </value>
        public override string SecWebSocketVersion => _request.Headers["Sec-WebSocket-Version"];

        /// <summary>
        /// Gets the endpoint to which the handshake request is sent.
        /// </summary>
        /// <value>
        /// A <see cref="System.Net.IPEndPoint"/> that represents the server IP
        /// address and port number.
        /// </value>
        public override System.Net.IPEndPoint ServerEndPoint => (System.Net.IPEndPoint)_serverEndPoint;

        /// <summary>
        /// Gets the client information.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="IPrincipal"/> instance that represents identity,
        ///   authentication, and security roles for the client.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if the client is not authenticated.
        ///   </para>
        /// </value>
        public override IPrincipal User => _user;

        /// <summary>
        /// Gets the endpoint from which the handshake request is sent.
        /// </summary>
        /// <value>
        /// A <see cref="System.Net.IPEndPoint"/> that represents the client IP
        /// address and port number.
        /// </value>
        public override System.Net.IPEndPoint UserEndPoint => (System.Net.IPEndPoint)_userEndPoint;

        /// <summary>
        /// Gets the WebSocket instance used for two-way communication between
        /// the client and server.
        /// </summary>
        /// <value>
        /// A <see cref="HttpSocket.WebSocket"/>.
        /// </value>
        public override WebSocket WebSocket { get; }

        private HttpRequest sendAuthenticationChallenge(string challenge)
        {
            var res = HttpResponse.CreateUnauthorizedResponse(challenge);
            var bytes = res.ToByteArray();
            Stream.Write(bytes, 0, bytes.Length);

            return HttpRequest.Read(Stream, 15000);
        }

        public bool Authenticate(AuthenticationSchemes scheme,string realm,Func<IIdentity, NetworkCredential> credentialsFinder)
        {
            var chal = new AuthenticationChallenge(scheme, realm).ToString();

            var retry = -1;

            bool Auth()
            {
                retry++;
                if (retry > 99)
                    return false;

                var user = HttpUtility.CreateUser(_request.Headers["Authorization"], scheme, realm, _request.HttpMethod, credentialsFinder);

                if (user != null && user.Identity.IsAuthenticated)
                {
                    _user = user;
                    return true;
                }

                _request = sendAuthenticationChallenge(chal);
                return Auth();
            }

            return Auth();
        }

        public void Close()
        {
            Stream.Close();
            _tcpClient.Close();
        }

        public void Close(HttpStatusCode code)
        {
            var res = HttpResponse.CreateCloseResponse(code);
            var bytes = res.ToByteArray();
            Stream.Write(bytes, 0, bytes.Length);

            Stream.Close();
            _tcpClient.Close();
        }

        /// <summary>
        /// Returns a string that represents the current instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that contains the request line and headers
        /// included in the handshake request.
        /// </returns>
        public override string ToString()
        {
            return _request.ToString();
        }
    }
}
