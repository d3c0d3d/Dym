using Dym.Libs.WebSocketLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dym.Libs.WebSocketLib
{
    public class WebSocketFrame : IEnumerable<byte>
    {
        /// <summary>
        /// Represents the ping frame without the payload data as an array of <see cref="byte"/>.
        /// </summary>
        /// <remarks>
        /// The value of this field is created from a non masked frame, so it can only be used to
        /// send a ping from a server.
        /// </remarks>
        public static readonly byte[] EmptyPingBytes;

        static WebSocketFrame()
        {
            EmptyPingBytes = CreatePingFrame(false).ToArray();
        }

        private WebSocketFrame()
        {
        }

        public WebSocketFrame(Opcode opcode, PayloadData payloadData, bool mask)
        : this(Fin.Final, opcode, payloadData, false, mask)
        {
        }

        public WebSocketFrame(Fin fin, Opcode opcode, byte[] data, bool compressed, bool mask)
          : this(fin, opcode, new PayloadData(data), compressed, mask)
        {
        }

        public WebSocketFrame(
          Fin fin, Opcode opcode, PayloadData payloadData, bool compressed, bool mask)
        {
            Fin = fin;
            Rsv1 = opcode.IsData() && compressed ? Rsv.On : Rsv.Off;
            Rsv2 = Rsv.Off;
            Rsv3 = Rsv.Off;
            Opcode = opcode;

            var len = payloadData.Length;
            if (len < 126)
            {
                PayloadLength = (byte)len;
                ExtendedPayloadLength = WebSocket.EmptyBytes;
            }
            else if (len < 0x010000)
            {
                PayloadLength = 126;
                ExtendedPayloadLength = ((ushort)len).InternalToByteArray(ByteOrder.Big);
            }
            else
            {
                PayloadLength = 127;
                ExtendedPayloadLength = len.InternalToByteArray(ByteOrder.Big);
            }

            if (mask)
            {
                Mask = Mask.On;
                MaskingKey = createMaskingKey();
                payloadData.Mask(MaskingKey);
            }
            else
            {
                Mask = Mask.Off;
                MaskingKey = WebSocket.EmptyBytes;
            }

            PayloadData = payloadData;
        }

        public int ExtendedPayloadLengthCount => PayloadLength < 126 ? 0 : (PayloadLength == 126 ? 2 : 8);

        public ulong FullPayloadLength => PayloadLength < 126
            ? PayloadLength
            : PayloadLength == 126
                ? ExtendedPayloadLength.ToUInt16(ByteOrder.Big)
                : ExtendedPayloadLength.ToUInt64(ByteOrder.Big);

        public byte[] ExtendedPayloadLength { get; private set; }

        public Fin Fin { get; private set; }

        public bool IsBinary => Opcode == Opcode.Binary;

        public bool IsClose => Opcode == Opcode.Close;

        public bool IsCompressed => Rsv1 == Rsv.On;

        public bool IsContinuation => Opcode == Opcode.Cont;

        public bool IsControl => Opcode >= Opcode.Close;

        public bool IsData => Opcode == Opcode.Text || Opcode == Opcode.Binary;

        public bool IsFinal => Fin == Fin.Final;

        public bool IsFragment => Fin == Fin.More || Opcode == Opcode.Cont;

        public bool IsMasked => Mask == Mask.On;

        public bool IsPing => Opcode == Opcode.Ping;

        public bool IsPong => Opcode == Opcode.Pong;

        public bool IsText => Opcode == Opcode.Text;

        public ulong Length => 2 + (ulong)(ExtendedPayloadLength.Length + MaskingKey.Length) + PayloadData.Length;

        public Mask Mask { get; private set; }

        public byte[] MaskingKey { get; private set; }

        public Opcode Opcode { get; private set; }

        public PayloadData PayloadData { get; private set; }

        public byte PayloadLength { get; private set; }

        public Rsv Rsv1 { get; private set; }

        public Rsv Rsv2 { get; private set; }

        public Rsv Rsv3 { get; private set; }

        private static byte[] createMaskingKey()
        {
            var key = new byte[4];
            WebSocket.RandomNumber.GetBytes(key);

            return key;
        }

        private static string dump(WebSocketFrame frame)
        {
            var len = frame.Length;
            var cnt = (long)(len / 4);
            var rem = (int)(len % 4);

            int cntDigit;
            string cntFmt;
            if (cnt < 10000)
            {
                cntDigit = 4;
                cntFmt = "{0,4}";
            }
            else if (cnt < 0x010000)
            {
                cntDigit = 4;
                cntFmt = "{0,4:X}";
            }
            else if (cnt < 0x0100000000)
            {
                cntDigit = 8;
                cntFmt = "{0,8:X}";
            }
            else
            {
                cntDigit = 16;
                cntFmt = "{0,16:X}";
            }

            var spFmt = $"{{0,{cntDigit}}}";
            var headerFmt = string.Format(@"
{0} 01234567 89ABCDEF 01234567 89ABCDEF
{0}+--------+--------+--------+--------+\n", spFmt);
            var lineFmt = $"{cntFmt}|{{1,8}} {{2,8}} {{3,8}} {{4,8}}|\n";
            var footerFmt = $"{spFmt}+--------+--------+--------+--------+";

            var output = new StringBuilder(64);
            Func<Action<string, string, string, string>> linePrinter = () =>
            {
                long lineCnt = 0;
                return (arg1, arg2, arg3, arg4) =>
                  output.AppendFormat(lineFmt, ++lineCnt, arg1, arg2, arg3, arg4);
            };
            var printLine = linePrinter();

            output.AppendFormat(headerFmt, string.Empty);

            var bytes = frame.ToArray();
            for (long i = 0; i <= cnt; i++)
            {
                var j = i * 4;
                if (i < cnt)
                {
                    printLine(
                      Convert.ToString(bytes[j], 2).PadLeft(8, '0'),
                      Convert.ToString(bytes[j + 1], 2).PadLeft(8, '0'),
                      Convert.ToString(bytes[j + 2], 2).PadLeft(8, '0'),
                      Convert.ToString(bytes[j + 3], 2).PadLeft(8, '0'));

                    continue;
                }

                if (rem > 0)
                    printLine(
                      Convert.ToString(bytes[j], 2).PadLeft(8, '0'),
                      rem >= 2 ? Convert.ToString(bytes[j + 1], 2).PadLeft(8, '0') : string.Empty,
                      rem == 3 ? Convert.ToString(bytes[j + 2], 2).PadLeft(8, '0') : string.Empty,
                      string.Empty);
            }

            output.AppendFormat(footerFmt, string.Empty);
            return output.ToString();
        }

        private static string print(WebSocketFrame frame)
        {
            // Payload Length
            var payloadLen = frame.PayloadLength;

            // Extended Payload Length
            var extPayloadLen = payloadLen > 125 ? frame.FullPayloadLength.ToString() : string.Empty;

            // Masking Key
            var maskingKey = BitConverter.ToString(frame.MaskingKey);

            // Payload Data
            var payload = payloadLen == 0
                          ? string.Empty
                          : payloadLen > 125
                            ? "---"
                            : frame.IsText && !(frame.IsFragment || frame.IsMasked || frame.IsCompressed)
                              ? frame.PayloadData.ApplicationData.UTF8Decode()
                              : frame.PayloadData.ToString();

            var fmt = @"
                    FIN: {0}
                   RSV1: {1}
                   RSV2: {2}
                   RSV3: {3}
                 Opcode: {4}
                   MASK: {5}
         Payload Length: {6}
Extended Payload Length: {7}
            Masking Key: {8}
           Payload Data: {9}";

            return string.Format(
              fmt,
              frame.Fin,
              frame.Rsv1,
              frame.Rsv2,
              frame.Rsv3,
              frame.Opcode,
              frame.Mask,
              payloadLen,
              extPayloadLen,
              maskingKey,
              payload);
        }

        private static WebSocketFrame processHeader(byte[] header)
        {
            if (header.Length != 2)
                throw new WebSocketException("The header of a frame cannot be read from the stream.");

            // FIN
            var fin = (header[0] & 0x80) == 0x80 ? Fin.Final : Fin.More;

            // RSV1
            var rsv1 = (header[0] & 0x40) == 0x40 ? Rsv.On : Rsv.Off;

            // RSV2
            var rsv2 = (header[0] & 0x20) == 0x20 ? Rsv.On : Rsv.Off;

            // RSV3
            var rsv3 = (header[0] & 0x10) == 0x10 ? Rsv.On : Rsv.Off;

            // Opcode
            var opcode = (byte)(header[0] & 0x0f);

            // MASK
            var mask = (header[1] & 0x80) == 0x80 ? Mask.On : Mask.Off;

            // Payload Length
            var payloadLen = (byte)(header[1] & 0x7f);

            var err = !opcode.IsSupported()
                      ? "An unsupported opcode."
                      : !opcode.IsData() && rsv1 == Rsv.On
                        ? "A non data frame is compressed."
                        : opcode.IsControl() && fin == Fin.More
                          ? "A control frame is fragmented."
                          : opcode.IsControl() && payloadLen > 125
                            ? "A control frame has a long payload length."
                            : null;

            if (err != null)
                throw new WebSocketException(CloseStatusCode.ProtocolError, err);

            var frame = new WebSocketFrame();
            frame.Fin = fin;
            frame.Rsv1 = rsv1;
            frame.Rsv2 = rsv2;
            frame.Rsv3 = rsv3;
            frame.Opcode = (Opcode)opcode;
            frame.Mask = mask;
            frame.PayloadLength = payloadLen;

            return frame;
        }

        private static WebSocketFrame readExtendedPayloadLength(Stream stream, WebSocketFrame frame)
        {
            var len = frame.ExtendedPayloadLengthCount;
            if (len == 0)
            {
                frame.ExtendedPayloadLength = WebSocket.EmptyBytes;
                return frame;
            }

            var bytes = stream.ReadBytes(len);
            if (bytes.Length != len)
                throw new WebSocketException(
                  "The extended payload length of a frame cannot be read from the stream.");

            frame.ExtendedPayloadLength = bytes;
            return frame;
        }

        private static void readExtendedPayloadLengthAsync(
          Stream stream,
          WebSocketFrame frame,
          Action<WebSocketFrame> completed,
          Action<Exception> error)
        {
            var len = frame.ExtendedPayloadLengthCount;
            if (len == 0)
            {
                frame.ExtendedPayloadLength = WebSocket.EmptyBytes;
                completed(frame);

                return;
            }

            stream.ReadBytesAsync(
              len,
              bytes =>
              {
                  if (bytes.Length != len)
                      throw new WebSocketException(
                  "The extended payload length of a frame cannot be read from the stream.");

                  frame.ExtendedPayloadLength = bytes;
                  completed(frame);
              },
              error);
        }

        private static WebSocketFrame readHeader(Stream stream)
        {
            return processHeader(stream.ReadBytes(2));
        }

        private static void readHeaderAsync(
          Stream stream, Action<WebSocketFrame> completed, Action<Exception> error)
        {
            stream.ReadBytesAsync(2, bytes => completed(processHeader(bytes)), error);
        }

        private static WebSocketFrame readMaskingKey(Stream stream, WebSocketFrame frame)
        {
            var len = frame.IsMasked ? 4 : 0;
            if (len == 0)
            {
                frame.MaskingKey = WebSocket.EmptyBytes;
                return frame;
            }

            var bytes = stream.ReadBytes(len);
            if (bytes.Length != len)
                throw new WebSocketException("The masking key of a frame cannot be read from the stream.");

            frame.MaskingKey = bytes;
            return frame;
        }

        private static void readMaskingKeyAsync(
          Stream stream,
          WebSocketFrame frame,
          Action<WebSocketFrame> completed,
          Action<Exception> error)
        {
            var len = frame.IsMasked ? 4 : 0;
            if (len == 0)
            {
                frame.MaskingKey = WebSocket.EmptyBytes;
                completed(frame);

                return;
            }

            stream.ReadBytesAsync(
              len,
              bytes =>
              {
                  if (bytes.Length != len)
                      throw new WebSocketException(
                  "The masking key of a frame cannot be read from the stream.");

                  frame.MaskingKey = bytes;
                  completed(frame);
              },
              error);
        }

        private static WebSocketFrame readPayloadData(Stream stream, WebSocketFrame frame)
        {
            var len = frame.FullPayloadLength;
            if (len == 0)
            {
                frame.PayloadData = PayloadData.Empty;
                return frame;
            }

            if (len > PayloadData.MaxLength)
                throw new WebSocketException(CloseStatusCode.TooBig, "A frame has a long payload length.");

            var llen = (long)len;
            var bytes = frame.PayloadLength < 127
                        ? stream.ReadBytes((int)len)
                        : stream.ReadBytes(llen, 1024);

            if (bytes.LongLength != llen)
                throw new WebSocketException(
                  "The payload data of a frame cannot be read from the stream.");

            frame.PayloadData = new PayloadData(bytes, llen);
            return frame;
        }

        private static void readPayloadDataAsync(
          Stream stream,
          WebSocketFrame frame,
          Action<WebSocketFrame> completed,
          Action<Exception> error)
        {
            var len = frame.FullPayloadLength;
            if (len == 0)
            {
                frame.PayloadData = PayloadData.Empty;
                completed(frame);

                return;
            }

            if (len > PayloadData.MaxLength)
                throw new WebSocketException(CloseStatusCode.TooBig, "A frame has a long payload length.");

            var llen = (long)len;
            Action<byte[]> compl = bytes =>
            {
                if (bytes.LongLength != llen)
                    throw new WebSocketException(
                      "The payload data of a frame cannot be read from the stream.");

                frame.PayloadData = new PayloadData(bytes, llen);
                completed(frame);
            };

            if (frame.PayloadLength < 127)
            {
                stream.ReadBytesAsync((int)len, compl, error);
                return;
            }

            stream.ReadBytesAsync(llen, 1024, compl, error);
        }

        public static WebSocketFrame CreateCloseFrame(
        PayloadData payloadData, bool mask
      )
        {
            return new WebSocketFrame(
                     Fin.Final, Opcode.Close, payloadData, false, mask
                   );
        }

        public static WebSocketFrame CreatePingFrame(bool mask)
        {
            return new WebSocketFrame(
                     Fin.Final, Opcode.Ping, PayloadData.Empty, false, mask
                   );
        }

        public static WebSocketFrame CreatePingFrame(byte[] data, bool mask)
        {
            return new WebSocketFrame(
                     Fin.Final, Opcode.Ping, new PayloadData(data), false, mask
                   );
        }

        public static WebSocketFrame CreatePongFrame(
          PayloadData payloadData, bool mask
        )
        {
            return new WebSocketFrame(
                     Fin.Final, Opcode.Pong, payloadData, false, mask
                   );
        }

        public static WebSocketFrame ReadFrame(Stream stream, bool unmask)
        {
            var frame = readHeader(stream);
            readExtendedPayloadLength(stream, frame);
            readMaskingKey(stream, frame);
            readPayloadData(stream, frame);

            if (unmask)
                frame.Unmask();

            return frame;
        }

        public static void ReadFrameAsync(
          Stream stream,
          bool unmask,
          Action<WebSocketFrame> completed,
          Action<Exception> error
        )
        {
            readHeaderAsync(
              stream,
              frame =>
                readExtendedPayloadLengthAsync(
                  stream,
                  frame,
                  frame1 =>
                    readMaskingKeyAsync(
                      stream,
                      frame1,
                      frame2 =>
                        readPayloadDataAsync(
                          stream,
                          frame2,
                          frame3 =>
                          {
                              if (unmask)
                                  frame3.Unmask();

                              completed(frame3);
                          },
                          error
                        ),
                      error
                    ),
                  error
                ),
              error
            );
        }

        public void Unmask()
        {
            if (Mask == Mask.Off)
                return;

            Mask = Mask.Off;
            PayloadData.Mask(MaskingKey);
            MaskingKey = WebSocket.EmptyBytes;
        }

        public IEnumerator<byte> GetEnumerator()
        {
            foreach (var b in ToArray())
                yield return b;
        }

        public void Print(bool dumped)
        {
            Console.WriteLine(dumped ? dump(this) : print(this));
        }

        public string PrintToString(bool dumped)
        {
            return dumped ? dump(this) : print(this);
        }

        public byte[] ToArray()
        {
            using (var buff = new MemoryStream())
            {
                var header = (int)Fin;
                header = (header << 1) + (int)Rsv1;
                header = (header << 1) + (int)Rsv2;
                header = (header << 1) + (int)Rsv3;
                header = (header << 4) + (int)Opcode;
                header = (header << 1) + (int)Mask;
                header = (header << 7) + PayloadLength;
                buff.Write(((ushort)header).InternalToByteArray(ByteOrder.Big), 0, 2);

                if (PayloadLength > 125)
                    buff.Write(ExtendedPayloadLength, 0, PayloadLength == 126 ? 2 : 8);

                if (Mask == Mask.On)
                    buff.Write(MaskingKey, 0, 4);

                if (PayloadLength > 0)
                {
                    var bytes = PayloadData.ToArray();
                    if (PayloadLength < 127)
                        buff.Write(bytes, 0, bytes.Length);
                    else
                        buff.WriteBytes(bytes, 1024);
                }

                buff.Close();
                return buff.ToArray();
            }
        }

        public override string ToString()
        {
            return BitConverter.ToString(ToArray());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
