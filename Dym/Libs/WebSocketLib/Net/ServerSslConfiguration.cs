using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Dym.Libs.WebSocketLib.Net
{
    /// <summary>
    /// Stores the parameters for the <see cref="SslStream"/> used by servers.
    /// </summary>
    public class ServerSslConfiguration
    {
        private RemoteCertificateValidationCallback _clientCertValidationCallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerSslConfiguration"/> class.
        /// </summary>
        public ServerSslConfiguration()
        {
            EnabledSslProtocols = SslProtocols.Default;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerSslConfiguration"/> class
        /// with the specified <paramref name="serverCertificate"/>.
        /// </summary>
        /// <param name="serverCertificate">
        /// A <see cref="X509Certificate2"/> that represents the certificate used to
        /// authenticate the server.
        /// </param>
        public ServerSslConfiguration(X509Certificate2 serverCertificate)
        {
            ServerCertificate = serverCertificate;
            EnabledSslProtocols = SslProtocols.Default;
        }

        /// <summary>
        /// Copies the parameters from the specified <paramref name="configuration"/> to
        /// a new instance of the <see cref="ServerSslConfiguration"/> class.
        /// </summary>
        /// <param name="configuration">
        /// A <see cref="ServerSslConfiguration"/> from which to copy.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="configuration"/> is <see langword="null"/>.
        /// </exception>
        public ServerSslConfiguration(ServerSslConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            CheckCertificateRevocation = configuration.CheckCertificateRevocation;
            ClientCertificateRequired = configuration.ClientCertificateRequired;
            _clientCertValidationCallback = configuration._clientCertValidationCallback;
            EnabledSslProtocols = configuration.EnabledSslProtocols;
            ServerCertificate = configuration.ServerCertificate;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the certificate revocation
        /// list is checked during authentication.
        /// </summary>
        /// <value>
        ///   <para>
        ///   <c>true</c> if the certificate revocation list is checked during
        ///   authentication; otherwise, <c>false</c>.
        ///   </para>
        ///   <para>
        ///   The default value is <c>false</c>.
        ///   </para>
        /// </value>
        public bool CheckCertificateRevocation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the client is asked for
        /// a certificate for authentication.
        /// </summary>
        /// <value>
        ///   <para>
        ///   <c>true</c> if the client is asked for a certificate for
        ///   authentication; otherwise, <c>false</c>.
        ///   </para>
        ///   <para>
        ///   The default value is <c>false</c>.
        ///   </para>
        /// </value>
        public bool ClientCertificateRequired { get; set; }

        /// <summary>
        /// Gets or sets the callback used to validate the certificate
        /// supplied by the client.
        /// </summary>
        /// <remarks>
        /// The certificate is valid if the callback returns <c>true</c>.
        /// </remarks>
        /// <value>
        ///   <para>
        ///   A <see cref="RemoteCertificateValidationCallback"/> delegate that
        ///   invokes the method called for validating the certificate.
        ///   </para>
        ///   <para>
        ///   The default value is a delegate that invokes a method that
        ///   only returns <c>true</c>.
        ///   </para>
        /// </value>
        public RemoteCertificateValidationCallback ClientCertificateValidationCallback => _clientCertValidationCallback ?? (_clientCertValidationCallback =
                                                                                                defaultValidateClientCertificate);

        /// <summary>
        /// Gets or sets the protocols used for authentication.
        /// </summary>
        /// <value>
        ///   <para>
        ///   The <see cref="SslProtocols"/> enum values that represent
        ///   the protocols used for authentication.
        ///   </para>
        ///   <para>
        ///   The default value is <see cref="SslProtocols.Default"/>.
        ///   </para>
        /// </value>
        public SslProtocols EnabledSslProtocols { get; set; }

        /// <summary>
        /// Gets or sets the certificate used to authenticate the server.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="X509Certificate2"/> or <see langword="null"/>
        ///   if not specified.
        ///   </para>
        ///   <para>
        ///   That instance represents an X.509 certificate.
        ///   </para>
        /// </value>
        public X509Certificate2 ServerCertificate { get; set; }

        private static bool defaultValidateClientCertificate(
        object sender,
        X509Certificate certificate,
        X509Chain chain,
        SslPolicyErrors sslPolicyErrors
      )
        {
            return true;
        }
    }
}
