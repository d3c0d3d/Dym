using System;

namespace Dym.Libs.WebSocketLib.Net
{
    [Flags]
    public enum HttpHeaderType
    {
        Unspecified = 0,
        Request = 1,
        Response = 1 << 1,
        Restricted = 1 << 2,
        MultiValue = 1 << 3,
        MultiValueInRequest = 1 << 4,
        MultiValueInResponse = 1 << 5
    }
}
