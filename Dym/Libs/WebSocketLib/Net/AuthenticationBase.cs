using System;
using System.Collections.Specialized;
using System.Text;

namespace ModuleFramework.Libs.WebSocketLib.Net
{
    public abstract class AuthenticationBase
    {
        public readonly NameValueCollection Parameters;

        protected AuthenticationBase(AuthenticationSchemes scheme, NameValueCollection parameters)
        {
            Scheme = scheme;
            Parameters = parameters;
        }

        public string Algorithm => Parameters["algorithm"];

        public string Nonce => Parameters["nonce"];

        public string Opaque => Parameters["opaque"];

        public string Qop => Parameters["qop"];

        public string Realm => Parameters["realm"];

        public AuthenticationSchemes Scheme { get; }

        public static string CreateNonceValue()
        {
            var src = new byte[16];
            var rand = new Random();
            rand.NextBytes(src);

            var res = new StringBuilder(32);
            foreach (var b in src)
                res.Append(b.ToString("x2"));

            return res.ToString();
        }

        public static NameValueCollection ParseParameters(string value)
        {
            var res = new NameValueCollection();
            foreach (var param in value.SplitHeaderValue(','))
            {
                var i = param.IndexOf('=');
                var name = i > 0 ? param.Substring(0, i).Trim() : null;
                var val = i < 0
                          ? param.Trim().Trim('"')
                          : i < param.Length - 1
                            ? param.Substring(i + 1).Trim().Trim('"')
                            : string.Empty;

                res.Add(name, val);
            }

            return res;
        }

        public abstract string ToBasicString();

        public abstract string ToDigestString();

        public override string ToString()
        {
            return Scheme == AuthenticationSchemes.Basic
                   ? ToBasicString()
                   : Scheme == AuthenticationSchemes.Digest
                     ? ToDigestString()
                     : string.Empty;
        }
    }
}
