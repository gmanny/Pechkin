using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;

namespace Pechkin.Synchronized.Util
{
    /// <summary>
    /// This object wraps <code>Dispatcher</code> class into <code>ISynchronizeInvoke</code> interface
    /// and also serves as the independent thread for Dispatcher to host its loop.
    /// 
    /// The thread is background and is terminated when your application exits.
    /// </summary>
    public class DispatcherThread : ISynchronizeInvoke
    {
        private readonly Thread _thread;
        private Dispatcher _dispatcher;

        private static int _threadId;

        /// <summary>
        /// This method is used as a Thread.Run for the delegate hosting thread.
        /// </summary>
        protected void Run()
        {
            Thread.CurrentThread.Name = "Dispatcher Thread #" + (_threadId++);

            lock (this)
            {
                _dispatcher = Dispatcher.CurrentDispatcher;

                Monitor.PulseAll(this);
            }

            Dispatcher.Run();
        }


        /// <summary>
        /// Creates new <code>DispatcherThread</code>, the object is initialized and thread is started here.
        /// </summary>
        public DispatcherThread()
        {
            _thread = new Thread(Run) {IsBackground = true};

            lock (this)
            {
                _thread.Start();

                Monitor.Wait(this);
            }
        }

        /// <summary>
        /// Invokes specified delegate with parameters on the dispatcher thread asynchronously.
        /// </summary>
        /// <param name="method">delegate to run on the thread</param>
        /// <param name="args">arguments to supply to the delegate</param>
        /// <returns>asynchronous result accessor</returns>
        public IAsyncResult BeginInvoke(Delegate method, object[] args)
        {
            return new DispatcherAsyncResultAdapter(_dispatcher.BeginInvoke(method, args));
        }

        /// <summary>
        /// Waits till specified delegate is executed and returns the result of the delegate.
        /// In other words, makes asynchronously lauched action synchronous.
        /// </summary>
        /// <param name="result">asynchronous result accessor returned by <code>BeginInvoke</code></param>
        /// <returns>result of an action</returns>
        public object EndInvoke(IAsyncResult result)
        {
            DispatcherAsyncResultAdapter res = result as DispatcherAsyncResultAdapter;
            if (res == null)
            {
                throw new InvalidCastException("result must be of DispatcherAsyncResultAdapter type");
            }

            res.Operation.Wait();
            return res.Operation.Result;
        }

        /// <summary>
        /// Invokes specified delegate with parameters on the dispatcher thread synchronously.
        /// </summary>
        /// <param name="method">delegate to run on the thread</param>
        /// <param name="args">arguments to supply to the delegate</param>
        /// <returns>result of an action</returns>
        public object Invoke(Delegate method, object[] args)
        {
            return _dispatcher.Invoke(method, args);
        }

        /// <summary>
        /// Tells whether you're on the same thread with the dispatcher or not. If you are, then no <code>Invoke</code> is required. In fact,
        /// synchronous <code>Invoke</code> will deadlock your thread.
        /// </summary>
        public bool InvokeRequired
        {
            get { return _dispatcher.Thread != Thread.CurrentThread; }
        }

        /// <summary>
        /// Tells the dispatcher to shutdown its worker thread.
        /// </summary>
        public void Terminate()
        {
            _dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
        }
    }
}