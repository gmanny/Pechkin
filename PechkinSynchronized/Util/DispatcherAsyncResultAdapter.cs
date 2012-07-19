using System;
using System.Threading;
using System.Windows.Threading;

namespace Pechkin.Synchronized.Util
{
    public class DispatcherAsyncResultAdapter : IAsyncResult
    {
        private readonly DispatcherOperation _op;
        private readonly object _state;

        public DispatcherAsyncResultAdapter(DispatcherOperation operation)
        {
            _op = operation;
        }

        public DispatcherAsyncResultAdapter(DispatcherOperation operation, object state)
            : this(operation)
        {
            _state = state;
        }

        public DispatcherOperation Operation
        {
            get { return _op; }
        }

        public object AsyncState
        {
            get { return _state; }
        }

        public WaitHandle AsyncWaitHandle
        {
            get { return null; }
        }

        public bool CompletedSynchronously
        {
            get { return false; }
        }

        public bool IsCompleted
        {
            get { return _op.Status == DispatcherOperationStatus.Completed; }
        }
    }
}
