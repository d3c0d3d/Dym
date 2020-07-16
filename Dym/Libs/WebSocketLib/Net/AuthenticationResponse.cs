using System;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace Dym.Libs.WebSocketLib.Net
{
    public class AuthenticationResponse : AuthenticationBase
    {
        private uint _nonceCount;

        private AuthenticationResponse(AuthenticationSchemes scheme, NameValueCollection parameters)
          : base(scheme, parameters)
        {
        }

        public AuthenticationResponse(NetworkCredential credentials)
          : this(AuthenticationSchemes.Basic, new NameValueCollection(), credentials, 0)
        {
        }

        public AuthenticationResponse(
          AuthenticationChallenge challenge, NetworkCredential credentials, uint nonceCount)
          : this(challenge.Scheme, challenge.Parameters, credentials, nonceCount)
        {
        }

        public AuthenticationResponse(
          AuthenticationSchemes scheme,
          NameValueCollection parameters,
          NetworkCredential credentials,
          uint nonceCount)
          : base(scheme, parameters)
        {
            Parameters["username"] = credentials.Username;
            Parameters["password"] = credentials.Password;
            Parameters["uri"] = credentials.Domain;
            _nonceCount = nonceCount;
            if (scheme == AuthenticationSchemes.Digest)
                initAsDigest();
        }

        public uint NonceCount => _nonceCount < uint.MaxValue
            ? _nonceCount
            : 0;

        public string Cnonce => Parameters["cnonce"];

        public string Nc => Parameters["nc"];

        public string Password => Parameters["password"];

        public string Response => Parameters["response"];

        public string Uri => Parameters["uri"];

        public string UserName => Parameters["username"];

        private static string createA1(string username, string password, string realm)
        {
            return $"{username}:{realm}:{password}";
        }

        private static string createA1(
          string username, string password, string realm, string nonce, string cnonce)
        {
            return $"{hash(createA1(username, password, realm))}:{nonce}:{cnonce}";
        }

        private static string createA2(string method, string uri)
        {
            return $"{method}:{uri}";
        }

        private static string createA2(string method, string uri, string entity)
        {
            return $"{method}:{uri}:{hash(entity)}";
        }

        private static string hash(string value)
        {
            var src = Encoding.UTF8.GetBytes(value);
            var md5 = MD5.Create();
            var hashed = md5.ComputeHash(src);

            var res = new StringBuilder(64);
            foreach (var b in hashed)
                res.Append(b.ToString("x2"));

            return res.ToString();
        }

        private void initAsDigest()
        {
            var qops = Parameters["qop"];
            if (qops != null)
            {
                if (qops.Split(',').Contains(qop => qop.Trim().ToLower() == "auth"))
                {
                    Parameters["qop"] = "auth";
                    Parameters["cnonce"] = CreateNonceValue();
                    Parameters["nc"] = $"{++_nonceCount:x8}";
                }
                else
                {
                    Parameters["qop"] = null;
                }
            }

            Parameters["method"] = "GET";
            Parameters["response"] = CreateRequestDigest(Parameters);
        }

        public static string CreateRequestDigest(NameValueCollection parameters)
        {
            var user = parameters["username"];
            var pass = parameters["password"];
            var realm = parameters["realm"];
            var nonce = parameters["nonce"];
            var uri = parameters["uri"];
            var algo = parameters["algorithm"];
            var qop = parameters["qop"];
            var cnonce = parameters["cnonce"];
            var nc = parameters["nc"];
            var method = parameters["method"];

            var a1 = algo != null && algo.ToLower() == "md5-sess"
                     ? createA1(user, pass, realm, nonce, cnonce)
                     : createA1(user, pass, realm);

            var a2 = qop != null && qop.ToLower() == "auth-int"
                     ? createA2(method, uri, parameters["entity"])
                     : createA2(method, uri);

            var secret = hash(a1);
            var data = qop != null
                       ? $"{nonce}:{nc}:{cnonce}:{qop}:{hash(a2)}"
                : $"{nonce}:{hash(a2)}";

            return hash($"{secret}:{data}");
        }

        public static AuthenticationResponse Parse(string value)
        {
            try
            {
                var cred = value.Split(new[] { ' ' }, 2);
                if (cred.Length != 2)
                    return null;

                var schm = cred[0].ToLower();
                return schm == "basic"
                       ? new AuthenticationResponse(
                           AuthenticationSchemes.Basic, ParseBasicCredentials(cred[1]))
                       : schm == "digest"
                         ? new AuthenticationResponse(
                             AuthenticationSchemes.Digest, ParseParameters(cred[1]))
                         : null;
            }
            catch
            {
            }

            return null;
        }

        public static NameValueCollection ParseBasicCredentials(string value)
        {
            // Decode the basic-credentials (a Base64 encoded string).
            var userPass = Encoding.Default.GetString(Convert.FromBase64String(value));

            // The format is [<domain>\]<username>:<password>.
            var i = userPass.IndexOf(':');
            var user = userPass.Substring(0, i);
            var pass = i < userPass.Length - 1 ? userPass.Substring(i + 1) : string.Empty;

            // Check if 'domain' exists.
            i = user.IndexOf('\\');
            if (i > -1)
                user = user.Substring(i + 1);

            var res = new NameValueCollection();
            res["username"] = user;
            res["password"] = pass;

            return res;
        }

        public override string ToBasicString()
        {
            var userPass = $"{Parameters["username"]}:{Parameters["password"]}";
            var cred = Convert.ToBase64String(Encoding.UTF8.GetBytes(userPass));

            return "Basic " + cred;
        }

        public override string ToDigestString()
        {
            var output = new StringBuilder(256);
            output.AppendFormat(
              "Digest username=\"{0}\", realm=\"{1}\", nonce=\"{2}\", uri=\"{3}\", response=\"{4}\"",
              Parameters["username"],
              Parameters["realm"],
              Parameters["nonce"],
              Parameters["uri"],
              Parameters["response"]);

            var opaque = Parameters["opaque"];
            if (opaque != null)
                output.AppendFormat(", opaque=\"{0}\"", opaque);

            var algo = Parameters["algorithm"];
            if (algo != null)
                output.AppendFormat(", algorithm={0}", algo);

            var qop = Parameters["qop"];
            if (qop != null)
                output.AppendFormat(
                  ", qop={0}, cnonce=\"{1}\", nc={2}", qop, Parameters["cnonce"], Parameters["nc"]);

            return output.ToString();
        }

        public IIdentity ToIdentity()
        {
            var schm = Scheme;
            return schm == AuthenticationSchemes.Basic
                   ? new HttpBasicIdentity(Parameters["username"], Parameters["password"]) as IIdentity
                   : schm == AuthenticationSchemes.Digest
                     ? new HttpDigestIdentity(Parameters)
                     : null;
        }
    }
}
