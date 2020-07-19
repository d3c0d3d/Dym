using System;

namespace Dym
{
    public class TransportMessageBase
    {
        public DateTime Created { get; private set; }
        public TransportType TransportType { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        public string RequestId { get; set; }

        public TransportMessageBase(TransportType transportType, string name, string content, string requestId = null)
        {
            TransportType = transportType;
            Name = name;
            Content = content;
            Created = DateTime.Now;
            RequestId = requestId;
        }

        public override string ToString()
        {
            return $"{Created}, {TransportType}, {Name}, {Content}, {RequestId}";
        }
    }

    public enum TransportType
    {
        Param,
        Send,
        Receipt
    }
}
