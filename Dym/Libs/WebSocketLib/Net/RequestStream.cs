using System;
using System.IO;

namespace Dym.Libs.WebSocketLib.Net
{
    public class RequestStream : Stream
    {
        private long _bodyLeft;
        private readonly byte[] _buffer;
        private int _count;
        private bool _disposed;
        private int _offset;
        private readonly Stream _stream;

        public RequestStream(Stream stream, byte[] buffer, int offset, int count, long contentLength = -1)
        {
            _stream = stream;
            _buffer = buffer;
            _offset = offset;
            _count = count;
            _bodyLeft = contentLength;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();

            set => throw new NotSupportedException();
        }

        // Returns 0 if we can keep reading from the base stream,
        // > 0 if we read something from the buffer,
        // -1 if we had a content length set and we finished reading that many bytes.
        private int fillFromBuffer(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "A negative value.");

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "A negative value.");

            var len = buffer.Length;
            if (offset + count > len)
                throw new ArgumentException(
                  "The sum of 'offset' and 'count' is greater than 'buffer' length.");

            if (_bodyLeft == 0)
                return -1;

            if (_count == 0 || count == 0)
                return 0;

            if (count > _count)
                count = _count;

            if (_bodyLeft > 0 && count > _bodyLeft)
                count = (int)_bodyLeft;

            Buffer.BlockCopy(_buffer, _offset, buffer, offset, count);
            _offset += count;
            _count -= count;
            if (_bodyLeft > 0)
                _bodyLeft -= count;

            return count;
        }

        public override IAsyncResult BeginRead(
        byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().ToString());

            var nread = fillFromBuffer(buffer, offset, count);
            if (nread > 0 || nread == -1)
            {
                var ares = new HttpStreamAsyncResult(callback, state)
                {
                    Buffer = buffer,
                    Offset = offset,
                    Count = count,
                    SyncRead = nread > 0 ? nread : 0
                };
                ares.Complete();

                return ares;
            }

            // Avoid reading past the end of the request to allow for HTTP pipelining.
            if (_bodyLeft >= 0 && count > _bodyLeft)
                count = (int)_bodyLeft;

            return _stream.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(
        byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new NotSupportedException();
        }

        public override void Close()
        {
            _disposed = true;
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().ToString());

            if (asyncResult == null)
                throw new ArgumentNullException(nameof(asyncResult));

            var result = asyncResult as HttpStreamAsyncResult;
            if (result != null)
            {
                var ares = result;
                if (!ares.IsCompleted)
                    ares.AsyncWaitHandle.WaitOne();

                return ares.SyncRead;
            }

            // Close on exception?
            var nread = _stream.EndRead(asyncResult);
            if (nread > 0 && _bodyLeft > 0)
                _bodyLeft -= nread;

            return nread;
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().ToString());

            // Call the fillFromBuffer method to check for buffer boundaries even when _bodyLeft is 0.
            var nread = fillFromBuffer(buffer, offset, count);
            if (nread == -1) // No more bytes available (Content-Length).
                return 0;

            if (nread > 0)
                return nread;

            nread = _stream.Read(buffer, offset, count);
            if (nread > 0 && _bodyLeft > 0)
                _bodyLeft -= nread;

            return nread;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
