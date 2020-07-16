using Dym.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Security.Principal;

namespace Dym.Libs.WebSocketLib.Net.WebSockets
{
    /// <summary>
    /// Provides the access to the information in a WebSocket handshake request to
    /// a <see cref="HttpListener"/> instance.
    /// </summary>
    public class HttpListenerWebSocketContext : WebSocketContext
    {
        private readonly HttpListenerContext _context;

        public HttpListenerWebSocketContext(
          HttpListenerContext context, string protocol
        )
        {
            _context = context;
            WebSocket = new WebSocket(this, protocol);
        }

        public Logger Log => _context.Listener.Log;

        public Stream Stream => _context.Connection.Stream;

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
        public override CookieCollection CookieCollection => _context.Request.Cookies;

        /// <summary>
        /// Gets the HTTP headers included in the handshake request.
        /// </summary>
        /// <value>
        /// A <see cref="NameValueCollection"/> that contains the headers.
        /// </value>
        public override NameValueCollection Headers => _context.Request.Headers;

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
        public override string Host => _context.Request.UserHostName;

        /// <summary>
        /// Gets a value indicating whether the client is authenticated.
        /// </summary>
        /// <value>
        /// <c>true</c> if the client is authenticated; otherwise, <c>false</c>.
        /// </value>
        public override bool IsAuthenticated => _context.Request.IsAuthenticated;

        /// <summary>
        /// Gets a value indicating whether the handshake request is sent from
        /// the local computer.
        /// </summary>
        /// <value>
        /// <c>true</c> if the handshake request is sent from the same computer
        /// as the server; otherwise, <c>false</c>.
        /// </value>
        public override bool IsLocal => _context.Request.IsLocal;

        /// <summary>
        /// Gets a value indicating whether a secure connection is used to send
        /// the handshake request.
        /// </summary>
        /// <value>
        /// <c>true</c> if the connection is secure; otherwise, <c>false</c>.
        /// </value>
        public override bool IsSecureConnection => _context.Request.IsSecureConnection;

        /// <summary>
        /// Gets a value indicating whether the request is a WebSocket handshake
        /// request.
        /// </summary>
        /// <value>
        /// <c>true</c> if the request is a WebSocket handshake request; otherwise,
        /// <c>false</c>.
        /// </value>
        public override bool IsWebSocketRequest => _context.Request.IsWebSocketRequest;

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
        public override string Origin => _context.Request.Headers["Origin"];

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
        public override NameValueCollection QueryString => _context.Request.QueryString;

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
        public override Uri RequestUri => _context.Request.Url;

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
        public override string SecWebSocketKey => _context.Request.Headers["Sec-WebSocket-Key"];

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
                var val = _context.Request.Headers["Sec-WebSocket-Protocol"];
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
        public override string SecWebSocketVersion => _context.Request.Headers["Sec-WebSocket-Version"];

        /// <summary>
        /// Gets the endpoint to which the handshake request is sent.
        /// </summary>
        /// <value>
        /// A <see cref="System.Net.IPEndPoint"/> that represents the server IP
        /// address and port number.
        /// </value>
        public override System.Net.IPEndPoint ServerEndPoint => _context.Request.LocalEndPoint;

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
        public override IPrincipal User => _context.User;

        /// <summary>
        /// Gets the endpoint from which the handshake request is sent.
        /// </summary>
        /// <value>
        /// A <see cref="System.Net.IPEndPoint"/> that represents the client IP
        /// address and port number.
        /// </value>
        public override System.Net.IPEndPoint UserEndPoint => _context.Request.RemoteEndPoint;

        /// <summary>
        /// Gets the WebSocket instance used for two-way communication between
        /// the client and server.
        /// </summary>
        /// <value>
        /// A <see cref="HttpSocket.WebSocket"/>.
        /// </value>
        public override WebSocket WebSocket { get; }

        public void Close()
        {
            _context.Connection.Close(true);
        }

        public void Close(HttpStatusCode code)
        {
            _context.Response.Close(code);
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
            return _context.Request.ToString();
        }
    }
}
