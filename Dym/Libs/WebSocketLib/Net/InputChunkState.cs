namespace Dym.Libs.WebSocketLib.Net
{
    public enum InputChunkState
    {
        None,
        Data,
        DataEnded,
        Trailer,
        End
    }
}
