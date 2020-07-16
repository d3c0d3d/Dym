using System;
using Dym.Logging;
using Dym.Libs.WebSocketLib.Net.WebSockets;

namespace Dym.Libs.WebSocketLib.Server
{
    /// <summary>
    /// Exposes the methods and properties used to access the information in
    /// a WebSocket service provided by the <see cref="WebSocketServer"/> or
    /// <see cref="HttpServer"/>.
    /// </summary>
    /// <remarks>
    /// This class is an abstract class.
    /// </remarks>
    public abstract class WebSocketServiceHost
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketServiceHost"/> class
        /// with the specified <paramref name="path"/> and <paramref name="log"/>.
        /// </summary>
        /// <param name="path">
        /// A <see cref="string"/> that represents the absolute path to the service.
        /// </param>
        /// <param name="log">
        /// A <see cref="Logger"/> that represents the logging function for the service.
        /// </param>
        protected WebSocketServiceHost(string path, Logger log)
        {
            Path = path;
            Log = log;

            Sessions = new WebSocketSessionManager(log);
        }

        public ServerState State => Sessions.State;

        /// <summary>
        /// Gets the logging function for the service.
        /// </summary>
        /// <value>
        /// A <see cref="Logger"/> that provides the logging function.
        /// </value>
        protected Logger Log { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the service cleans up
        /// the inactive sessions periodically.
        /// </summary>
        /// <remarks>
        /// The set operation does nothing if the service has already started or
        /// it is shutting down.
        /// </remarks>
        /// <value>
        /// <c>true</c> if the service cleans up the inactive sessions every
        /// 60 seconds; otherwise, <c>false</c>.
        /// </value>
        public bool KeepClean
        {
            set => Sessions.KeepClean = value;
        }

        /// <summary>
        /// Gets the path to the service.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the absolute path to
        /// the service.
        /// </value>
        public string Path { get; }

        /// <summary>
        /// Gets the management function for the sessions in the service.
        /// </summary>
        /// <value>
        /// A <see cref="WebSocketSessionManager"/> that manages the sessions in
        /// the service.
        /// </value>
        public WebSocketSessionManager Sessions { get; }

        /// <summary>
        /// Gets the <see cref="Type"/> of the behavior of the service.
        /// </summary>
        /// <value>
        /// A <see cref="Type"/> that represents the type of the behavior of
        /// the service.
        /// </value>
        public abstract Type BehaviorType { get; }

        /// <summary>
        /// Gets or sets the time to wait for the response to the WebSocket Ping or
        /// Close.
        /// </summary>
        /// <remarks>
        /// The set operation does nothing if the service has already started or
        /// it is shutting down.
        /// </remarks>
        /// <value>
        /// A <see cref="TimeSpan"/> to wait for the response.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value specified for a set operation is zero or less.
        /// </exception>
        public TimeSpan WaitTime
        {
            get => Sessions.WaitTime;

            set => Sessions.WaitTime = value;
        }

        public void Start()
        {
            Sessions.Start();
        }

        public void StartSession(WebSocketContext context)
        {
            CreateSession().Start(context, Sessions);
        }

        public void Stop(ushort code, string reason)
        {
            Sessions.Stop(code, reason);
        }

        /// <summary>
        /// Creates a new session for the service.
        /// </summary>
        /// <returns>
        /// A <see cref="WebSocketBehavior"/> instance that represents
        /// the new session.
        /// </returns>
        protected abstract WebSocketBehavior CreateSession();
    }
}
