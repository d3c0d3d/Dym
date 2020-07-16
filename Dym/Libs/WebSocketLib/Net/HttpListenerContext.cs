using System;
using System.Security.Principal;
using ModuleFramework.Libs.WebSocketLib.Net.WebSockets;

namespace ModuleFramework.Libs.WebSocketLib.Net
{
    /// <summary>
    /// Provides the access to the HTTP request and response objects used by
    /// the <see cref="HttpListener"/>.
    /// </summary>
    /// <remarks>
    /// This class cannot be inherited.
    /// </remarks>
    public sealed class HttpListenerContext
    {
        private HttpListenerWebSocketContext _websocketContext;

        public HttpListenerContext(HttpConnection connection)
        {
            Connection = connection;
            ErrorStatus = 400;
            Request = new HttpListenerRequest(this);
            Response = new HttpListenerResponse(this);
        }

        public HttpConnection Connection { get; }

        public string ErrorMessage { get; set; }

        public int ErrorStatus { get; set; }

        public bool HasError => ErrorMessage != null;

        public HttpListener Listener { get; set; }

        /// <summary>
        /// Gets the HTTP request object that represents a client request.
        /// </summary>
        /// <value>
        /// A <see cref="HttpListenerRequest"/> that represents the client request.
        /// </value>
        public HttpListenerRequest Request { get; }

        /// <summary>
        /// Gets the HTTP response object used to send a response to the client.
        /// </summary>
        /// <value>
        /// A <see cref="HttpListenerResponse"/> that represents a response to the client request.
        /// </value>
        public HttpListenerResponse Response { get; }

        /// <summary>
        /// Gets the client information (identity, authentication, and security roles).
        /// </summary>
        /// <value>
        /// A <see cref="IPrincipal"/> instance that represents the client information.
        /// </value>
        public IPrincipal User { get; private set; }

        public bool Authenticate()
        {
            var schm = Listener.SelectAuthenticationScheme(Request);
            if (schm == AuthenticationSchemes.Anonymous)
                return true;

            if (schm == AuthenticationSchemes.None)
            {
                Response.Close(HttpStatusCode.Forbidden);
                return false;
            }

            var realm = Listener.GetRealm();
            var user = HttpUtility.CreateUser(Request.Headers["Authorization"],schm,realm,Request.HttpMethod,
                Listener.GetUserCredentialsFinder());

            if (user == null || !user.Identity.IsAuthenticated)
            {
                Response.CloseWithAuthChallenge(new AuthenticationChallenge(schm, realm).ToString());
                return false;
            }

            User = user;
            return true;
        }

        public bool Register()
        {
            return Listener.RegisterContext(this);
        }

        public void Unregister()
        {
            Listener.UnregisterContext(this);
        }

        /// <summary>
        /// Accepts a WebSocket handshake request.
        /// </summary>
        /// <returns>
        /// A <see cref="HttpListenerWebSocketContext"/> that represents
        /// the WebSocket handshake request.
        /// </returns>
        /// <param name="protocol">
        /// A <see cref="string"/> that represents the subprotocol supported on
        /// this WebSocket connection.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="protocol"/> is empty.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="protocol"/> contains an invalid character.
        ///   </para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// This method has already been called.
        /// </exception>
        public HttpListenerWebSocketContext AcceptWebSocket(string protocol)
        {
            if (_websocketContext != null)
                throw new InvalidOperationException("The accepting is already in progress.");

            if (protocol != null)
            {
                if (protocol.Length == 0)
                    throw new ArgumentException("An empty string.", nameof(protocol));

                if (!protocol.IsToken())
                    throw new ArgumentException("Contains an invalid character.", nameof(protocol));
            }

            _websocketContext = new HttpListenerWebSocketContext(this, protocol);
            return _websocketContext;
        }
    }
}
