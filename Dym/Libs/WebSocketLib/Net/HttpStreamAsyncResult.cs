using System;
using System.Threading;

namespace ModuleFramework.Libs.WebSocketLib.Net
{
    public class HttpStreamAsyncResult : IAsyncResult
    {
        private readonly AsyncCallback _callback;
        private bool _completed;
        private readonly object _sync;
        private ManualResetEvent _waitHandle;

        public HttpStreamAsyncResult(AsyncCallback callback, object state)
        {
            _callback = callback;
            AsyncState = state;
            _sync = new object();
        }

        public byte[] Buffer { get; set; }

        public int Count { get; set; }

        public Exception Exception { get; private set; }

        public bool HasException => Exception != null;

        public int Offset { get; set; }

        public int SyncRead { get; set; }

        public object AsyncState { get; }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                lock (_sync)
                    return _waitHandle ?? (_waitHandle = new ManualResetEvent(_completed));
            }
        }

        public bool CompletedSynchronously => SyncRead == Count;

        public bool IsCompleted
        {
            get
            {
                lock (_sync)
                    return _completed;
            }
        }

        public void Complete()
        {
            lock (_sync)
            {
                if (_completed)
                    return;

                _completed = true;
                _waitHandle?.Set();

                _callback?.BeginInvoke(this, ar => _callback.EndInvoke(ar), null);
            }
        }

        public void Complete(Exception exception)
        {
            Exception = exception;
            Complete();
        }
    }
}
