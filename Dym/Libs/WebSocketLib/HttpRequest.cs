using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using Dym.Libs.WebSocketLib.Net;

namespace Dym.Libs.WebSocketLib
{
    public class HttpRequest : HttpBase
    {
        private CookieCollection _cookies;

        private HttpRequest(string method, string uri, Version version, NameValueCollection headers)
        : base(version, headers)
        {
            HttpMethod = method;
            RequestUri = uri;
        }

        public HttpRequest(string method, string uri)
        : this(method, uri, HttpVersion.Version11, new NameValueCollection())
        {
            Headers["User-Agent"] = nameof(WebSocketLib);
        }

        public AuthenticationResponse AuthenticationResponse
        {
            get
            {
                var res = Headers["Authorization"];
                return !string.IsNullOrEmpty(res)
                       ? AuthenticationResponse.Parse(res)
                       : null;
            }
        }

        public CookieCollection Cookies
        {
            get
            {
                if (_cookies == null)
                    _cookies = Headers.GetCookies(false);

                return _cookies;
            }
        }

        public string HttpMethod { get; }

        public bool IsWebSocketRequest => HttpMethod == "GET"
                                            && ProtocolVersion > HttpVersion.Version10
                                            && Headers.Upgrades("websocket");

        public string RequestUri { get; }

        public static HttpRequest CreateConnectRequest(Uri uri)
        {
            var host = uri.DnsSafeHost;
            var port = uri.Port;
            var authority = $"{host}:{port}";
            var req = new HttpRequest("CONNECT", authority);
            req.Headers["Host"] = port == 80 ? host : authority;

            return req;
        }

        public static HttpRequest CreateWebSocketRequest(Uri uri)
        {
            var req = new HttpRequest("GET", uri.PathAndQuery);
            var headers = req.Headers;

            // Only includes a port number in the Host header value if it's non-default.
            // See: https://tools.ietf.org/html/rfc6455#page-17
            var port = uri.Port;
            var schm = uri.Scheme;
            headers["Host"] = (port == 80 && schm == "ws") || (port == 443 && schm == "wss")
                              ? uri.DnsSafeHost
                              : uri.Authority;

            headers["Upgrade"] = "websocket";
            headers["Connection"] = "Upgrade";

            return req;
        }

        public HttpResponse GetResponse(Stream stream, int millisecondsTimeout)
        {
            var buff = ToByteArray();
            stream.Write(buff, 0, buff.Length);

            return Read<HttpResponse>(stream, HttpResponse.Parse, millisecondsTimeout);
        }

        public static HttpRequest Parse(string[] headerParts)
        {
            var requestLine = headerParts[0].Split(new[] { ' ' }, 3);
            if (requestLine.Length != 3)
                throw new ArgumentException("Invalid request line: " + headerParts[0]);

            var headers = new WebHeaderCollection();
            for (int i = 1; i < headerParts.Length; i++)
                headers.InternalSet(headerParts[i], false);

            return new HttpRequest(
              requestLine[0], requestLine[1], new Version(requestLine[2].Substring(5)), headers);
        }

        public static HttpRequest Read(Stream stream, int millisecondsTimeout)
        {
            return Read<HttpRequest>(stream, Parse, millisecondsTimeout);
        }

        public void SetCookies(CookieCollection cookies)
        {
            if (cookies == null || cookies.Count == 0)
                return;

            var buff = new StringBuilder(64);
            foreach (var cookie in cookies.Sorted)
                if (!cookie.Expired)
                    buff.AppendFormat("{0}; ", cookie);

            var len = buff.Length;
            if (len > 2)
            {
                buff.Length = len - 2;
                Headers["Cookie"] = buff.ToString();
            }
        }

        public override string ToString()
        {
            var output = new StringBuilder(64);
            output.AppendFormat("{0} {1} HTTP/{2}{3}", HttpMethod, RequestUri, ProtocolVersion, CrLf);

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
