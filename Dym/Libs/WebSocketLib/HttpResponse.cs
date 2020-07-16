using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using ModuleFramework.Libs.WebSocketLib.Net;

namespace ModuleFramework.Libs.WebSocketLib
{
    public class HttpResponse : HttpBase
    {
        private HttpResponse(string code, string reason, Version version, NameValueCollection headers)
        : base(version, headers)
        {
            StatusCode = code;
            Reason = reason;
        }

        public HttpResponse(HttpStatusCode code)
        : this(code, code.GetDescription())
        {
        }

        public HttpResponse(HttpStatusCode code, string reason)
          : this(((int)code).ToString(), reason, HttpVersion.Version11, new NameValueCollection())
        {
            Headers["Server"] = "0xCC";
        }

        public CookieCollection Cookies => Headers.GetCookies(true);

        public bool HasConnectionClose
        {
            get
            {
                var comparison = StringComparison.OrdinalIgnoreCase;
                return Headers.Contains("Connection", "close", comparison);
            }
        }

        public bool IsProxyAuthenticationRequired => StatusCode == "407";

        public bool IsRedirect => StatusCode == "301" || StatusCode == "302";

        public bool IsUnauthorized => StatusCode == "401";

        public bool IsWebSocketResponse => ProtocolVersion > HttpVersion.Version10
                                             && StatusCode == "101"
                                             && Headers.Upgrades("websocket");

        public string Reason { get; }

        public string StatusCode { get; }

        public static HttpResponse CreateCloseResponse(HttpStatusCode code)
        {
            var res = new HttpResponse(code);
            res.Headers["Connection"] = "close";

            return res;
        }

        public static HttpResponse CreateUnauthorizedResponse(string challenge)
        {
            var res = new HttpResponse(HttpStatusCode.Unauthorized);
            res.Headers["WWW-Authenticate"] = challenge;

            return res;
        }

        public static HttpResponse CreateWebSocketResponse()
        {
            var res = new HttpResponse(HttpStatusCode.SwitchingProtocols);

            var headers = res.Headers;
            headers["Upgrade"] = "websocket";
            headers["Connection"] = "Upgrade";

            return res;
        }

        public static HttpResponse Parse(string[] headerParts)
        {
            var statusLine = headerParts[0].Split(new[] { ' ' }, 3);
            if (statusLine.Length != 3)
                throw new ArgumentException("Invalid status line: " + headerParts[0]);

            var headers = new WebHeaderCollection();
            for (int i = 1; i < headerParts.Length; i++)
                headers.InternalSet(headerParts[i], true);

            return new HttpResponse(
              statusLine[1], statusLine[2], new Version(statusLine[0].Substring(5)), headers);
        }

        public static HttpResponse Read(Stream stream, int millisecondsTimeout)
        {
            return Read<HttpResponse>(stream, Parse, millisecondsTimeout);
        }

        public void SetCookies(CookieCollection cookies)
        {
            if (cookies == null || cookies.Count == 0)
                return;

            var headers = Headers;
            foreach (var cookie in cookies.Sorted)
                headers.Add("Set-Cookie", cookie.ToResponseString());
        }

        public override string ToString()
        {
            var output = new StringBuilder(64);
            output.AppendFormat("HTTP/{0} {1} {2}{3}", ProtocolVersion, StatusCode, Reason, CrLf);

            var headers = Headers;
            foreach (var key in headers.AllKeys)
                output.AppendFormat("{0}: {1}{2}", key, headers[key], CrLf);

            output.Append(CrLf);

            var entity = EntityBody;
            if (entity.Length > 0)
                output.Append(entity);

            return output.ToString();
        }
    }
}
