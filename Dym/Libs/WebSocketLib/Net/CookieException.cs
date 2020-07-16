using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Dym.Libs.WebSocketLib.Net
{
    /// <summary>
    /// The exception that is thrown when a <see cref="Cookie"/> gets an error.
    /// </summary>
    [Serializable]
    public class CookieException : FormatException, ISerializable
    {
        public CookieException(string message)
        : base(message)
        {
        }

        public CookieException(string message, Exception innerException)
          : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CookieException"/> class from
        /// the specified <see cref="SerializationInfo"/> and <see cref="StreamingContext"/>.
        /// </summary>
        /// <param name="serializationInfo">
        /// A <see cref="SerializationInfo"/> that contains the serialized object data.
        /// </param>
        /// <param name="streamingContext">
        /// A <see cref="StreamingContext"/> that specifies the source for the deserialization.
        /// </param>
        protected CookieException(
          SerializationInfo serializationInfo, StreamingContext streamingContext)
          : base(serializationInfo, streamingContext)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CookieException"/> class.
        /// </summary>
        public CookieException() : base()
        {
        }

        /// <summary>
        /// Populates the specified <see cref="SerializationInfo"/> with the data needed to serialize
        /// the current <see cref="CookieException"/>.
        /// </summary>
        /// <param name="serializationInfo">
        /// A <see cref="SerializationInfo"/> that holds the serialized object data.
        /// </param>
        /// <param name="streamingContext">
        /// A <see cref="StreamingContext"/> that specifies the destination for the serialization.
        /// </param>
        [SecurityPermission(
          SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            base.GetObjectData(serializationInfo, streamingContext);
        }

        /// <summary>
        /// Populates the specified <see cref="SerializationInfo"/> with the data needed to serialize
        /// the current <see cref="CookieException"/>.
        /// </summary>
        /// <param name="serializationInfo">
        /// A <see cref="SerializationInfo"/> that holds the serialized object data.
        /// </param>
        /// <param name="streamingContext">
        /// A <see cref="StreamingContext"/> that specifies the destination for the serialization.
        /// </param>
        [SecurityPermission(SecurityAction.LinkDemand,Flags = SecurityPermissionFlag.SerializationFormatter,SerializationFormatter = true)]
        void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            base.GetObjectData(serializationInfo, streamingContext);
        }
    }
}
