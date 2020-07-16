using System;

namespace Dym.Libs.WebSocketLib
{
    /// <summary>
    /// Represents the event data for the <see cref="WebSocket.OnClose"/> event.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   That event occurs when the WebSocket connection has been closed.
    ///   </para>
    ///   <para>
    ///   If you would like to get the reason for the close, you should access
    ///   the <see cref="Code"/> or <see cref="Reason"/> property.
    ///   </para>
    /// </remarks>
    public class CloseEventArgs : EventArgs
    {
        public CloseEventArgs()
        {
            PayloadData = PayloadData.Empty;
        }

        public CloseEventArgs(CloseStatusCode code)
          : this((ushort)code)
        {
        }

        public CloseEventArgs(PayloadData payloadData)
        {
            PayloadData = payloadData;
        }

        public CloseEventArgs(ushort code, string reason = null)
        {
            PayloadData = new PayloadData(code, reason);
        }

        public CloseEventArgs(CloseStatusCode code, string reason)
          : this((ushort)code, reason)
        {
        }

        public PayloadData PayloadData { get; }

        /// <summary>
        /// Gets the status code for the close.
        /// </summary>
        /// <value>
        /// A <see cref="ushort"/> that represents the status code for the close if any.
        /// </value>
        public ushort Code => PayloadData.Code;

        /// <summary>
        /// Gets the reason for the close.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the reason for the close if any.
        /// </value>
        public string Reason => PayloadData.Reason ?? string.Empty;

        /// <summary>
        /// Gets a value indicating whether the connection has been closed cleanly.
        /// </summary>
        /// <value>
        /// <c>true</c> if the connection has been closed cleanly; otherwise, <c>false</c>.
        /// </value>
        public bool WasClean { get; set; }
    }
}
