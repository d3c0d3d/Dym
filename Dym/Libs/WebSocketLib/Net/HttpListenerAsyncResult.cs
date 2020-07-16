using System;
using System.Threading;

namespace Dym.Libs.WebSocketLib.Net
{
    public class HttpListenerAsyncResult : IAsyncResult
    {
        private readonly AsyncCallback _callback;
        private bool _completed;
        private HttpListenerContext _context;
        private Exception _exception;
        private readonly object _sync;
        private ManualResetEvent _waitHandle;

        public HttpListenerAsyncResult(AsyncCallback callback, object state)
        {
            _callback = callback;
            AsyncState = state;
            _sync = new object();
        }

        public bool EndCalled { get; set; }

        public bool InGet { get; set; }

        public object AsyncState { get; }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                lock (_sync)
                    return _waitHandle ?? (_waitHandle = new ManualResetEvent(_completed));
            }
        }

        public bool CompletedSynchronously { get; private set; }

        public bool IsCompleted
        {
            get
            {
                lock (_sync)
                    return _completed;
            }
        }

        private static void complete(HttpListenerAsyncResult asyncResult)
        {
            lock (asyncResult._sync)
            {
                asyncResult._completed = true;

                var waitHandle = asyncResult._waitHandle;
                waitHandle?.Set();
            }

            var callback = asyncResult._callback;
            if (callback == null)
                return;

            ThreadPool.QueueUserWorkItem(
              state =>
              {
                  try
                  {
                      callback(asyncResult);
                  }
                  catch
                  {
                  }
              },
              null
            );
        }

        public void Complete(Exception exception)
        {
            _exception = InGet && (exception is ObjectDisposedException)
                         ? new HttpListenerException(995, "The listener is closed.")
                         : exception;

            complete(this);
        }

        public void Complete(HttpListenerContext context, bool syncCompleted = false)
        {
            _context = context;
            CompletedSynchronously = syncCompleted;

            complete(this);
        }

        public HttpListenerContext GetContext()
        {
            if (_exception != null)
                throw _exception;

            return _context;
        }
    }
}
