using System;

namespace Dym.Libs.WebSocketLib
{
    /// <summary>
    /// Represents the event data for the <see cref="WebSocket.OnError"/> event.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   That event occurs when the <see cref="WebSocket"/> gets an error.
    ///   </para>
    ///   <para>
    ///   If you would like to get the error message, you should access
    ///   the <see cref="ErrorEventArgs.Message"/> property.
    ///   </para>
    ///   <para>
    ///   And if the error is due to an exception, you can get it by accessing
    ///   the <see cref="ErrorEventArgs.Exception"/> property.
    ///   </para>
    /// </remarks>
    public class ErrorEventArgs : EventArgs
    {
        public ErrorEventArgs(string message, Exception exception = null)
        {
            Message = message;
            Exception = exception;
        }

        /// <summary>
        /// Gets the exception that caused the error.
        /// </summary>
        /// <value>
        /// An <see cref="System.Exception"/> instance that represents the cause of
        /// the error if it is due to an exception; otherwise, <see langword="null"/>.
        /// </value>
        public Exception Exception { get; }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the error message.
        /// </value>
        public string Message { get; }
    }
}
