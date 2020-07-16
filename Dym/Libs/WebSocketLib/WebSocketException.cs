using System;

namespace Dym.Libs.WebSocketLib
{
    /// <summary>
    /// The exception that is thrown when a fatal error occurs in
    /// the WebSocket communication.
    /// </summary>
    public class WebSocketException : Exception
    {
        public WebSocketException()
        : this(CloseStatusCode.Abnormal, null, null)
        {
        }

        public WebSocketException(Exception innerException)
          : this(CloseStatusCode.Abnormal, null, innerException)
        {
        }

        public WebSocketException(string message)
          : this(CloseStatusCode.Abnormal, message, null)
        {
        }

        public WebSocketException(CloseStatusCode code)
          : this(code, null, null)
        {
        }

        public WebSocketException(string message, Exception innerException)
          : this(CloseStatusCode.Abnormal, message, innerException)
        {
        }

        public WebSocketException(CloseStatusCode code, Exception innerException)
          : this(code, null, innerException)
        {
        }

        public WebSocketException(
          CloseStatusCode code, string message, Exception innerException = null)
          : base(message ?? code.GetMessage(), innerException)
        {
            Code = code;
        }

        /// <summary>
        /// Gets the status code indicating the cause of the exception.
        /// </summary>
        /// <value>
        /// One of the <see cref="CloseStatusCode"/> enum values that represents
        /// the status code indicating the cause of the exception.
        /// </value>
        public CloseStatusCode Code { get; }
    }
}
