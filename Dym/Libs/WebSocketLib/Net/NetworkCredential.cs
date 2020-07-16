using System;

namespace ModuleFramework.Libs.WebSocketLib.Net
{
    /// <summary>
    /// Provides the credentials for the password-based authentication.
    /// </summary>
    public class NetworkCredential
    {
        private string _domain;
        private static readonly string[] _noRoles;
        private string _password;
        private string[] _roles;

        static NetworkCredential()
        {
            _noRoles = new string[0];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkCredential"/> class with
        /// the specified <paramref name="username"/> and <paramref name="password"/>.
        /// </summary>
        /// <param name="username">
        /// A <see cref="string"/> that represents the username associated with
        /// the credentials.
        /// </param>
        /// <param name="password">
        /// A <see cref="string"/> that represents the password for the username
        /// associated with the credentials.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="username"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="username"/> is empty.
        /// </exception>
        public NetworkCredential(string username, string password)
          : this(username, password, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkCredential"/> class with
        /// the specified <paramref name="username"/>, <paramref name="password"/>,
        /// <paramref name="domain"/> and <paramref name="roles"/>.
        /// </summary>
        /// <param name="username">
        /// A <see cref="string"/> that represents the username associated with
        /// the credentials.
        /// </param>
        /// <param name="password">
        /// A <see cref="string"/> that represents the password for the username
        /// associated with the credentials.
        /// </param>
        /// <param name="domain">
        /// A <see cref="string"/> that represents the domain associated with
        /// the credentials.
        /// </param>
        /// <param name="roles">
        /// An array of <see cref="string"/> that represents the roles
        /// associated with the credentials if any.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="username"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="username"/> is empty.
        /// </exception>
        public NetworkCredential(
          string username, string password, string domain, params string[] roles
        )
        {
            if (username == null)
                throw new ArgumentNullException(nameof(username));

            if (username.Length == 0)
                throw new ArgumentException("An empty string.", nameof(username));

            Username = username;
            _password = password;
            _domain = domain;
            _roles = roles;
        }

        /// <summary>
        /// Gets the domain associated with the credentials.
        /// </summary>
        /// <remarks>
        /// This property returns an empty string if the domain was
        /// initialized with <see langword="null"/>.
        /// </remarks>
        /// <value>
        /// A <see cref="string"/> that represents the domain name
        /// to which the username belongs.
        /// </value>
        public string Domain => _domain ?? string.Empty;

        /// <summary>
        /// Gets the password for the username associated with the credentials.
        /// </summary>
        /// <remarks>
        /// This property returns an empty string if the password was
        /// initialized with <see langword="null"/>.
        /// </remarks>
        /// <value>
        /// A <see cref="string"/> that represents the password.
        /// </value>
        public string Password => _password ?? string.Empty;

        /// <summary>
        /// Gets the roles associated with the credentials.
        /// </summary>
        /// <remarks>
        /// This property returns an empty array if the roles were
        /// initialized with <see langword="null"/>.
        /// </remarks>
        /// <value>
        /// An array of <see cref="string"/> that represents the role names
        /// to which the username belongs.
        /// </value>
        public string[] Roles => _roles ?? _noRoles;

        /// <summary>
        /// Gets the username associated with the credentials.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the username.
        /// </value>
        public string Username { get; set; }
    }
}
