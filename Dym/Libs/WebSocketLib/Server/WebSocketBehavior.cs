using System;
using System.IO;
using Dym.Logging;
using Dym.Libs.WebSocketLib.Net;
using Dym.Libs.WebSocketLib.Net.WebSockets;

namespace Dym.Libs.WebSocketLib.Server
{
    /// <summary>
    /// Exposes a set of methods and properties used to define the behavior of
    /// a WebSocket service provided by the <see cref="WebSocketServer"/> or
    /// <see cref="HttpServer"/>.
    /// </summary>
    /// <remarks>
    /// This class is an abstract class.
    /// </remarks>
    public abstract class WebSocketBehavior : IWebSocketSession
    {
        private bool _emitOnPing;
        private string _protocol;
        private WebSocket _websocket;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketBehavior"/> class.
        /// </summary>
        protected WebSocketBehavior()
        {
            StartTime = DateTime.MaxValue;
        }

        /// <summary>
        /// Gets the logging functions.
        /// </summary>
        /// <value>
        /// A <see cref="Logger"/> that provides the logging functions,
        /// or <see langword="null"/> if the WebSocket connection isn't established.
        /// </value>
        protected Logger Log => _websocket?.Log;

        /// <summary>
        /// Gets the access to the sessions in the WebSocket service.
        /// </summary>
        /// <value>
        /// A <see cref="WebSocketSessionManager"/> that provides the access to the sessions,
        /// or <see langword="null"/> if the WebSocket connection isn't established.
        /// </value>
        protected WebSocketSessionManager Sessions { get; private set; }

        /// <summary>
        /// Gets the information in a handshake request to the WebSocket service.
        /// </summary>
        /// <value>
        /// A <see cref="WebSocketContext"/> instance that provides the access to the handshake request,
        /// or <see langword="null"/> if the WebSocket connection isn't established.
        /// </value>
        public WebSocketContext Context { get; private set; }

        /// <summary>
        /// Gets or sets the delegate called to validate the HTTP cookies included in
        /// a handshake request to the WebSocket service.
        /// </summary>
        /// <remarks>
        /// This delegate is called when the <see cref="WebSocket"/> used in a session validates
        /// the handshake request.
        /// </remarks>
        /// <value>
        ///   <para>
        ///   A <c>Func&lt;CookieCollection, CookieCollection, bool&gt;</c> delegate that references
        ///   the method(s) used to validate the cookies.
        ///   </para>
        ///   <para>
        ///   1st <see cref="CookieCollection"/> parameter passed to this delegate contains
        ///   the cookies to validate if any.
        ///   </para>
        ///   <para>
        ///   2nd <see cref="CookieCollection"/> parameter passed to this delegate receives
        ///   the cookies to send to the client.
        ///   </para>
        ///   <para>
        ///   This delegate should return <c>true</c> if the cookies are valid.
        ///   </para>
        ///   <para>
        ///   The default value is <see langword="null"/>, and it does nothing to validate.
        ///   </para>
        /// </value>
        public Func<CookieCollection, CookieCollection, bool> CookiesValidator { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="WebSocket"/> used in a session emits
        /// a <see cref="WebSocket.OnMessage"/> event when receives a Ping.
        /// </summary>
        /// <value>
        /// <c>true</c> if the <see cref="WebSocket"/> emits a <see cref="WebSocket.OnMessage"/> event
        /// when receives a Ping; otherwise, <c>false</c>. The default value is <c>false</c>.
        /// </value>
        public bool EmitOnPing
        {
            get => _websocket != null ? _websocket.EmitOnPing : _emitOnPing;

            set
            {
                if (_websocket != null)
                {
                    _websocket.EmitOnPing = value;
                    return;
                }

                _emitOnPing = value;
            }
        }

        /// <summary>
        /// Gets the unique ID of a session.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the unique ID of the session,
        /// or <see langword="null"/> if the WebSocket connection isn't established.
        /// </value>
        public string ID { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the WebSocket service ignores
        /// the Sec-WebSocket-Extensions header included in a handshake request.
        /// </summary>
        /// <value>
        /// <c>true</c> if the WebSocket service ignores the extensions requested from
        /// a client; otherwise, <c>false</c>. The default value is <c>false</c>.
        /// </value>
        public bool IgnoreExtensions { get; set; }

        /// <summary>
        /// Gets or sets the delegate called to validate the Origin header included in
        /// a handshake request to the WebSocket service.
        /// </summary>
        /// <remarks>
        /// This delegate is called when the <see cref="WebSocket"/> used in a session validates
        /// the handshake request.
        /// </remarks>
        /// <value>
        ///   <para>
        ///   A <c>Func&lt;string, bool&gt;</c> delegate that references the method(s) used to
        ///   validate the origin header.
        ///   </para>
        ///   <para>
        ///   <see cref="string"/> parameter passed to this delegate represents the value of
        ///   the origin header to validate if any.
        ///   </para>
        ///   <para>
        ///   This delegate should return <c>true</c> if the origin header is valid.
        ///   </para>
        ///   <para>
        ///   The default value is <see langword="null"/>, and it does nothing to validate.
        ///   </para>
        /// </value>
        public Func<string, bool> OriginValidator { get; set; }

        /// <summary>
        /// Gets or sets the WebSocket subprotocol used in the WebSocket service.
        /// </summary>
        /// <remarks>
        /// Set operation of this property is available before the WebSocket connection has
        /// been established.
        /// </remarks>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the subprotocol if any.
        ///   The default value is <see cref="String.Empty"/>.
        ///   </para>
        ///   <para>
        ///   The value to set must be a token defined in
        ///   <see href="http://tools.ietf.org/html/rfc2616#section-2.2">RFC 2616</see>.
        ///   </para>
        /// </value>
        public string Protocol
        {
            get => _websocket != null ? _websocket.Protocol : (_protocol ?? string.Empty);

            set
            {
                if (State != WebSocketState.Connecting)
                    return;

                if (value != null && (value.Length == 0 || !value.IsToken()))
                    return;

                _protocol = value;
            }
        }

        /// <summary>
        /// Gets the time that a session has started.
        /// </summary>
        /// <value>
        /// A <see cref="DateTime"/> that represents the time that the session has started,
        /// or <see cref="DateTime.MaxValue"/> if the WebSocket connection isn't established.
        /// </value>
        public DateTime StartTime { get; private set; }

        /// <summary>
        /// Gets the state of the <see cref="WebSocket"/> used in a session.
        /// </summary>
        /// <value>
        /// One of the <see cref="WebSocketState"/> enum values, indicates the state of
        /// the <see cref="WebSocket"/>.
        /// </value>
        public WebSocketState State => _websocket?.ReadyState ?? WebSocketState.Connecting;

        private string checkHandshakeRequest(WebSocketContext context)
        {
            return OriginValidator != null && !OriginValidator(context.Origin)
                   ? "Includes no Origin header, or it has an invalid value."
                   : CookiesValidator != null
                     && !CookiesValidator(context.CookieCollection, context.WebSocket.CookieCollection)
                     ? "Includes no cookie, or an invalid cookie exists."
                     : null;
        }

        private void onClose(object sender, CloseEventArgs e)
        {
            if (ID == null)
                return;

            Sessions.Remove(ID);
            OnClose(e);
        }

        private void onError(object sender, ErrorEventArgs e)
        {
            OnError(e);
        }

        private void onMessage(object sender, MessageEventArgs e)
        {
            OnMessage(e);
        }

        private void onOpen(object sender, EventArgs e)
        {
            ID = Sessions.Add(this);
            if (ID == null)
            {
                _websocket.Close(CloseStatusCode.Away);
                return;
            }

            StartTime = DateTime.Now;
            OnOpen();
        }

        public void Start(WebSocketContext context, WebSocketSessionManager sessions)
        {
            if (_websocket != null)
            {
                _websocket.Log.Error("A session instance cannot be reused.");
                context.WebSocket.Close(HttpStatusCode.ServiceUnavailable);

                return;
            }

            Context = context;
            Sessions = sessions;

            _websocket = context.WebSocket;
            _websocket.CustomHandshakeRequestChecker = checkHandshakeRequest;
            _websocket.EmitOnPing = _emitOnPing;
            _websocket.IgnoreExtensions = IgnoreExtensions;
            _websocket.Protocol = _protocol;

            var waitTime = sessions.WaitTime;
            if (waitTime != _websocket.WaitTime)
                _websocket.WaitTime = waitTime;

            _websocket.OnOpen += onOpen;
            _websocket.OnMessage += onMessage;
            _websocket.OnError += onError;
            _websocket.OnClose += onClose;

            _websocket.InternalAccept();
        }

        /// <summary>
        /// Calls the <see cref="OnError"/> method with the specified message.
        /// </summary>
        /// <param name="message">
        /// A <see cref="string"/> that represents the error message.
        /// </param>
        /// <param name="exception">
        /// An <see cref="Exception"/> instance that represents the cause of
        /// the error if present.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="message"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="message"/> is an empty string.
        /// </exception>
        protected void Error(string message, Exception exception)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message.Length == 0)
                throw new ArgumentException("An empty string.", nameof(message));

            OnError(new ErrorEventArgs(message, exception));
        }

        /// <summary>
        /// Called when the WebSocket connection for a session has been closed.
        /// </summary>
        /// <param name="e">
        /// A <see cref="CloseEventArgs"/> that represents the event data passed
        /// from a <see cref="WebSocket.OnClose"/> event.
        /// </param>
        protected virtual void OnClose(CloseEventArgs e)
        {
        }

        /// <summary>
        /// Called when the WebSocket instance for a session gets an error.
        /// </summary>
        /// <param name="e">
        /// A <see cref="ErrorEventArgs"/> that represents the event data passed
        /// from a <see cref="WebSocket.OnError"/> event.
        /// </param>
        protected virtual void OnError(ErrorEventArgs e)
        {
        }

        /// <summary>
        /// Called when the WebSocket instance for a session receives a message.
        /// </summary>
        /// <param name="e">
        /// A <see cref="MessageEventArgs"/> that represents the event data passed
        /// from a <see cref="WebSocket.OnMessage"/> event.
        /// </param>
        protected virtual void OnMessage(MessageEventArgs e)
        {
        }

        /// <summary>
        /// Called when the WebSocket connection for a session has been established.
        /// </summary>
        protected virtual void OnOpen()
        {
        }

        /// <summary>
        /// Sends the specified data to a client using the WebSocket connection.
        /// </summary>
        /// <param name="data">
        /// An array of <see cref="byte"/> that represents the binary data to send.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The current state of the connection is not Open.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        protected void Send(byte[] data)
        {
            if (_websocket == null)
            {
                var msg = "The current state of the connection is not Open.";
                throw new InvalidOperationException(msg);
            }

            _websocket.Send(data);
        }

        /// <summary>
        /// Sends the specified file to a client using the WebSocket connection.
        /// </summary>
        /// <param name="fileInfo">
        ///   <para>
        ///   A <see cref="FileInfo"/> that specifies the file to send.
        ///   </para>
        ///   <para>
        ///   The file is sent as the binary data.
        ///   </para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The current state of the connection is not Open.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="fileInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   The file does not exist.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The file could not be opened.
        ///   </para>
        /// </exception>
        protected void Send(FileInfo fileInfo)
        {
            if (_websocket == null)
            {
                var msg = "The current state of the connection is not Open.";
                throw new InvalidOperationException(msg);
            }

            _websocket.Send(fileInfo);
        }

        /// <summary>
        /// Sends the specified data to a client using the WebSocket connection.
        /// </summary>
        /// <param name="data">
        /// A <see cref="string"/> that represents the text data to send.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The current state of the connection is not Open.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="data"/> could not be UTF-8-encoded.
        /// </exception>
        protected void Send(string data)
        {
            if (_websocket == null)
            {
                var msg = "The current state of the connection is not Open.";
                throw new InvalidOperationException(msg);
            }

            _websocket.Send(data);
        }

        /// <summary>
        /// Sends the data from the specified stream to a client using
        /// the WebSocket connection.
        /// </summary>
        /// <param name="stream">
        ///   <para>
        ///   A <see cref="Stream"/> instance from which to read the data to send.
        ///   </para>
        ///   <para>
        ///   The data is sent as the binary data.
        ///   </para>
        /// </param>
        /// <param name="length">
        /// An <see cref="int"/> that specifies the number of bytes to send.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The current state of the connection is not Open.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="stream"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="stream"/> cannot be read.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="length"/> is less than 1.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   No data could be read from <paramref name="stream"/>.
        ///   </para>
        /// </exception>
        protected void Send(Stream stream, int length)
        {
            if (_websocket == null)
            {
                var msg = "The current state of the connection is not Open.";
                throw new InvalidOperationException(msg);
            }

            _websocket.Send(stream, length);
        }

        /// <summary>
        /// Sends the specified data to a client asynchronously using
        /// the WebSocket connection.
        /// </summary>
        /// <remarks>
        /// This method does not wait for the send to be complete.
        /// </remarks>
        /// <param name="data">
        /// An array of <see cref="byte"/> that represents the binary data to send.
        /// </param>
        /// <param name="completed">
        ///   <para>
        ///   An <c>Action&lt;bool&gt;</c> delegate or <see langword="null"/>
        ///   if not needed.
        ///   </para>
        ///   <para>
        ///   The delegate invokes the method called when the send is complete.
        ///   </para>
        ///   <para>
        ///   <c>true</c> is passed to the method if the send has done with
        ///   no error; otherwise, <c>false</c>.
        ///   </para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The current state of the connection is not Open.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        protected void SendAsync(byte[] data, Action<bool> completed)
        {
            if (_websocket == null)
            {
                var msg = "The current state of the connection is not Open.";
                throw new InvalidOperationException(msg);
            }

            _websocket.SendAsync(data, completed);
        }

        /// <summary>
        /// Sends the specified file to a client asynchronously using
        /// the WebSocket connection.
        /// </summary>
        /// <remarks>
        /// This method does not wait for the send to be complete.
        /// </remarks>
        /// <param name="fileInfo">
        ///   <para>
        ///   A <see cref="FileInfo"/> that specifies the file to send.
        ///   </para>
        ///   <para>
        ///   The file is sent as the binary data.
        ///   </para>
        /// </param>
        /// <param name="completed">
        ///   <para>
        ///   An <c>Action&lt;bool&gt;</c> delegate or <see langword="null"/>
        ///   if not needed.
        ///   </para>
        ///   <para>
        ///   The delegate invokes the method called when the send is complete.
        ///   </para>
        ///   <para>
        ///   <c>true</c> is passed to the method if the send has done with
        ///   no error; otherwise, <c>false</c>.
        ///   </para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The current state of the connection is not Open.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="fileInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   The file does not exist.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The file could not be opened.
        ///   </para>
        /// </exception>
        protected void SendAsync(FileInfo fileInfo, Action<bool> completed)
        {
            if (_websocket == null)
            {
                var msg = "The current state of the connection is not Open.";
                throw new InvalidOperationException(msg);
            }

            _websocket.SendAsync(fileInfo, completed);
        }

        /// <summary>
        /// Sends the specified data to a client asynchronously using
        /// the WebSocket connection.
        /// </summary>
        /// <remarks>
        /// This method does not wait for the send to be complete.
        /// </remarks>
        /// <param name="data">
        /// A <see cref="string"/> that represents the text data to send.
        /// </param>
        /// <param name="completed">
        ///   <para>
        ///   An <c>Action&lt;bool&gt;</c> delegate or <see langword="null"/>
        ///   if not needed.
        ///   </para>
        ///   <para>
        ///   The delegate invokes the method called when the send is complete.
        ///   </para>
        ///   <para>
        ///   <c>true</c> is passed to the method if the send has done with
        ///   no error; otherwise, <c>false</c>.
        ///   </para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The current state of the connection is not Open.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="data"/> could not be UTF-8-encoded.
        /// </exception>
        protected void SendAsync(string data, Action<bool> completed)
        {
            if (_websocket == null)
            {
                var msg = "The current state of the connection is not Open.";
                throw new InvalidOperationException(msg);
            }

            _websocket.SendAsync(data, completed);
        }

        /// <summary>
        /// Sends the data from the specified stream to a client asynchronously
        /// using the WebSocket connection.
        /// </summary>
        /// <remarks>
        /// This method does not wait for the send to be complete.
        /// </remarks>
        /// <param name="stream">
        ///   <para>
        ///   A <see cref="Stream"/> instance from which to read the data to send.
        ///   </para>
        ///   <para>
        ///   The data is sent as the binary data.
        ///   </para>
        /// </param>
        /// <param name="length">
        /// An <see cref="int"/> that specifies the number of bytes to send.
        /// </param>
        /// <param name="completed">
        ///   <para>
        ///   An <c>Action&lt;bool&gt;</c> delegate or <see langword="null"/>
        ///   if not needed.
        ///   </para>
        ///   <para>
        ///   The delegate invokes the method called when the send is complete.
        ///   </para>
        ///   <para>
        ///   <c>true</c> is passed to the method if the send has done with
        ///   no error; otherwise, <c>false</c>.
        ///   </para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The current state of the connection is not Open.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="stream"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="stream"/> cannot be read.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="length"/> is less than 1.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   No data could be read from <paramref name="stream"/>.
        ///   </para>
        /// </exception>
        protected void SendAsync(Stream stream, int length, Action<bool> completed)
        {
            if (_websocket == null)
            {
                var msg = "The current state of the connection is not Open.";
                throw new InvalidOperationException(msg);
            }

            _websocket.SendAsync(stream, length, completed);
        }
    }
}
