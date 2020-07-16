using System.Security.Principal;

namespace Dym.Libs.WebSocketLib.Net
{
    /// <summary>
    /// Holds the username and password from an HTTP Basic authentication attempt.
    /// </summary>
    public class HttpBasicIdentity : GenericIdentity
    {
        public HttpBasicIdentity(string username, string password)
        : base(username, "Basic")
        {
            Password = password;
        }

        /// <summary>
        /// Gets the password from a basic authentication attempt.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the password.
        /// </value>
        public virtual string Password { get; }
    }
}
