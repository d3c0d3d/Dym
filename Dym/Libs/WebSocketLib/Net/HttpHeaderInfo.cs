namespace Dym.Libs.WebSocketLib.Net
{
    public class HttpHeaderInfo
    {
        public HttpHeaderInfo(string name, HttpHeaderType type)
        {
            Name = name;
            Type = type;
        }

        public bool IsMultiValueInRequest => (Type & HttpHeaderType.MultiValueInRequest) == HttpHeaderType.MultiValueInRequest;

        public bool IsMultiValueInResponse => (Type & HttpHeaderType.MultiValueInResponse) == HttpHeaderType.MultiValueInResponse;

        public bool IsRequest => (Type & HttpHeaderType.Request) == HttpHeaderType.Request;

        public bool IsResponse => (Type & HttpHeaderType.Response) == HttpHeaderType.Response;

        public string Name { get; }

        public HttpHeaderType Type { get; }

        public bool IsMultiValue(bool response)
        {
            return (Type & HttpHeaderType.MultiValue) == HttpHeaderType.MultiValue
                   ? (response ? IsResponse : IsRequest)
                   : (response ? IsMultiValueInResponse : IsMultiValueInRequest);
        }

        public bool IsRestricted(bool response)
        {
            return (Type & HttpHeaderType.Restricted) == HttpHeaderType.Restricted && (response ? IsResponse : IsRequest);
        }
    }
}
