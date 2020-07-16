using ModuleFramework.Libs.WebSocketLib;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ModuleFramework.Libs.WebSocketLib
{
    public class PayloadData : IEnumerable<byte>
    {
        private ushort _code;
        private bool _codeSet;
        private readonly byte[] _data;
        private readonly long _length;
        private string _reason;
        private bool _reasonSet;

        /// <summary>
        /// Represents the empty payload data.
        /// </summary>
        public static readonly PayloadData Empty;

        /// <summary>
        /// Represents the allowable max length.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   A <see cref="WebSocketException"/> will occur if the payload data length is
        ///   greater than the value of this field.
        ///   </para>
        ///   <para>
        ///   If you would like to change the value, you must set it to a value between
        ///   <c>WebSocket.FragmentLength</c> and <c>Int64.MaxValue</c> inclusive.
        ///   </para>
        /// </remarks>
        public static readonly ulong MaxLength;

        static PayloadData()
        {
            Empty = new PayloadData();
            MaxLength = long.MaxValue;
        }

        public PayloadData()
        {
            _code = 1005;
            _reason = string.Empty;

            _data = WebSocket.EmptyBytes;

            _codeSet = true;
            _reasonSet = true;
        }

        public PayloadData(byte[] data)
          : this(data, data.LongLength)
        {
        }

        public PayloadData(byte[] data, long length)
        {
            _data = data;
            _length = length;
        }

        public PayloadData(ushort code, string reason)
        {
            _code = code;
            _reason = reason ?? string.Empty;

            _data = code.Append(reason);
            _length = _data.LongLength;

            _codeSet = true;
            _reasonSet = true;
        }

        public ushort Code
        {
            get
            {
                if (!_codeSet)
                {
                    _code = _length > 1
                            ? _data.SubArray(0, 2).ToUInt16(ByteOrder.Big)
                            : (ushort)1005;

                    _codeSet = true;
                }

                return _code;
            }
        }

        public long ExtensionDataLength { get; set; }

        public bool HasReservedCode => _length > 1 && Code.IsReserved();

        public string Reason
        {
            get
            {
                if (!_reasonSet)
                {
                    _reason = _length > 2
                              ? _data.SubArray(2, _length - 2).UTF8Decode()
                              : string.Empty;

                    _reasonSet = true;
                }

                return _reason;
            }
        }

        public byte[] ApplicationData => ExtensionDataLength > 0
            ? _data.SubArray(ExtensionDataLength, _length - ExtensionDataLength)
            : _data;

        public byte[] ExtensionData => ExtensionDataLength > 0
            ? _data.SubArray(0, ExtensionDataLength)
            : WebSocket.EmptyBytes;

        public ulong Length => (ulong)_length;

        public void Mask(byte[] key)
        {
            for (long i = 0; i < _length; i++)
                _data[i] = (byte)(_data[i] ^ key[i % 4]);
        }

        public IEnumerator<byte> GetEnumerator()
        {
            foreach (var b in _data)
                yield return b;
        }

        public byte[] ToArray()
        {
            return _data;
        }

        public override string ToString()
        {
            return BitConverter.ToString(_data);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
