using System.Collections.Specialized;
using System.Text;

namespace Dym.Libs.WebSocketLib.Net
{
    public class AuthenticationChallenge : AuthenticationBase
    {
        private AuthenticationChallenge(AuthenticationSchemes scheme, NameValueCollection parameters)
          : base(scheme, parameters)
        {
        }

        public AuthenticationChallenge(AuthenticationSchemes scheme, string realm)
          : base(scheme, new NameValueCollection())
        {
            Parameters["realm"] = realm;
            if (scheme == AuthenticationSchemes.Digest)
            {
                Parameters["nonce"] = CreateNonceValue();
                Parameters["algorithm"] = "MD5";
                Parameters["qop"] = "auth";
            }
        }

        public string Domain => Parameters["domain"];

        public string Stale => Parameters["stale"];

        public static AuthenticationChallenge CreateBasicChallenge(string realm)
        {
            return new AuthenticationChallenge(AuthenticationSchemes.Basic, realm);
        }

        public static AuthenticationChallenge CreateDigestChallenge(string realm)
        {
            return new AuthenticationChallenge(AuthenticationSchemes.Digest, realm);
        }

        public static AuthenticationChallenge Parse(string value)
        {
            var chal = value.Split(new[] { ' ' }, 2);
            if (chal.Length != 2)
                return null;

            var schm = chal[0].ToLower();
            return schm == "basic"
                   ? new AuthenticationChallenge(
                       AuthenticationSchemes.Basic, ParseParameters(chal[1]))
                   : schm == "digest"
                     ? new AuthenticationChallenge(
                         AuthenticationSchemes.Digest, ParseParameters(chal[1]))
                     : null;
        }

        public override string ToBasicString()
        {
            return $"Basic realm=\"{Parameters["realm"]}\"";
        }

        public override string ToDigestString()
        {
            var output = new StringBuilder(128);

            var domain = Parameters["domain"];
            if (domain != null)
                output.AppendFormat(
                  "Digest realm=\"{0}\", domain=\"{1}\", nonce=\"{2}\"",
                  Parameters["realm"],
                  domain,
                  Parameters["nonce"]);
            else
                output.AppendFormat(
                  "Digest realm=\"{0}\", nonce=\"{1}\"", Parameters["realm"], Parameters["nonce"]);

            var opaque = Parameters["opaque"];
            if (opaque != null)
                output.AppendFormat(", opaque=\"{0}\"", opaque);

            var stale = Parameters["stale"];
            if (stale != null)
                output.AppendFormat(", stale={0}", stale);

            var algo = Parameters["algorithm"];
            if (algo != null)
                output.AppendFormat(", algorithm={0}", algo);

            var qop = Parameters["qop"];
            if (qop != null)
                output.AppendFormat(", qop=\"{0}\"", qop);

            return output.ToString();
        }
    }
}
