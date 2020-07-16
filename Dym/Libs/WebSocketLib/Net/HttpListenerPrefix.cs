using System;

namespace ModuleFramework.Libs.WebSocketLib.Net
{
    public sealed class HttpListenerPrefix
    {
        private string _prefix;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpListenerPrefix"/> class with
        /// the specified <paramref name="uriPrefix"/>.
        /// </summary>
        /// <remarks>
        /// This constructor must be called after calling the CheckPrefix method.
        /// </remarks>
        /// <param name="uriPrefix">
        /// A <see cref="string"/> that represents the URI prefix.
        /// </param>
        public HttpListenerPrefix(string uriPrefix)
        {
            Original = uriPrefix;
            parse(uriPrefix);
        }

        public string Host { get; private set; }

        public bool IsSecure { get; private set; }

        public HttpListener Listener { get; set; }

        public string Original { get; }

        public string Path { get; private set; }

        public string Port { get; private set; }

        private void parse(string uriPrefix)
        {
            if (uriPrefix.StartsWith("https"))
                IsSecure = true;

            var len = uriPrefix.Length;
            var startHost = uriPrefix.IndexOf(':') + 3;
            var root = uriPrefix.IndexOf('/', startHost + 1, len - startHost - 1);

            var colon = uriPrefix.LastIndexOf(':', root - 1, root - startHost - 1);
            if (uriPrefix[root - 1] != ']' && colon > startHost)
            {
                Host = uriPrefix.Substring(startHost, colon - startHost);
                Port = uriPrefix.Substring(colon + 1, root - colon - 1);
            }
            else
            {
                Host = uriPrefix.Substring(startHost, root - startHost);
                Port = IsSecure ? "443" : "80";
            }

            Path = uriPrefix.Substring(root);

            _prefix =
                $"http{(IsSecure ? "s" : "")}://{Host}:{Port}{Path}";
        }

        public static void CheckPrefix(string uriPrefix)
        {
            if (uriPrefix == null)
                throw new ArgumentNullException(nameof(uriPrefix));

            var len = uriPrefix.Length;
            if (len == 0)
                throw new ArgumentException("An empty string.", nameof(uriPrefix));

            if (!(uriPrefix.StartsWith("http://") || uriPrefix.StartsWith("https://")))
                throw new ArgumentException("The scheme isn't 'http' or 'https'.", nameof(uriPrefix));

            var startHost = uriPrefix.IndexOf(':') + 3;
            if (startHost >= len)
                throw new ArgumentException("No host is specified.", nameof(uriPrefix));

            if (uriPrefix[startHost] == ':')
                throw new ArgumentException("No host is specified.", nameof(uriPrefix));

            var root = uriPrefix.IndexOf('/', startHost, len - startHost);
            if (root == startHost)
                throw new ArgumentException("No host is specified.", nameof(uriPrefix));

            if (root == -1 || uriPrefix[len - 1] != '/')
                throw new ArgumentException("Ends without '/'.", nameof(uriPrefix));

            if (uriPrefix[root - 1] == ':')
                throw new ArgumentException("No port is specified.", nameof(uriPrefix));

            if (root == len - 2)
                throw new ArgumentException("No path is specified.", nameof(uriPrefix));
        }

        /// <summary>
        /// Determines whether this instance and the specified <see cref="Object"/> have the same value.
        /// </summary>
        /// <remarks>
        /// This method will be required to detect duplicates in any collection.
        /// </remarks>
        /// <param name="obj">
        /// An <see cref="Object"/> to compare to this instance.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="obj"/> is a <see cref="HttpListenerPrefix"/> and
        /// its value is the same as this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            var pref = obj as HttpListenerPrefix;
            return pref != null && pref._prefix == _prefix;
        }

        /// <summary>
        /// Gets the hash code for this instance.
        /// </summary>
        /// <remarks>
        /// This method will be required to detect duplicates in any collection.
        /// </remarks>
        /// <returns>
        /// An <see cref="int"/> that represents the hash code.
        /// </returns>
        public override int GetHashCode()
        {
            return _prefix.GetHashCode();
        }

        public override string ToString()
        {
            return _prefix;
        }
    }
}
