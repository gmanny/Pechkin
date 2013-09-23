using System;
using System.Collections.Generic;
using System.Threading;

namespace Pechkin.Util
{
    /// <summary>
    /// This class runs the thread and lets users to run delegates synchronously on that thread while obtaining results of the execution.
    /// 
    /// It's like <code>ISynchronizedInvoke</code>, but with only synchronous methods (because we don't need more).
    /// </summary>
    public class SynchronizedDispatcherThread
    {
        /// <summary>
        /// Task object that's pushed to the queue.
        /// </summary>
        private class DispatcherTask
        {
            // task code
            public Delegate Task;

            // task parameters
            public object[] Params;

            // result, filled out after it's executed
            public object Result;
        }

        private readonly Thread _thread;
        private readonly object _sync = new Object(); // we wait on this object
        private bool _shutdown;
        private readonly Queue<DispatcherTask> _taskQueue = new Queue<DispatcherTask>();

        private static int _threadId;

        /// <summary>
        /// This method is used as a Thread.Run for the delegate hosting thread.
        /// </summary>
        protected void Run()
        {
            // name thread for debugging purposes
            Thread.CurrentThread.Name = "Synchronized Dispatcher Thread #" + (_threadId++);

            lock (_sync)
            {
                // wake up constructor thread
                Monitor.PulseAll(_sync);
            }

            try
            {
                // we process task queue
                while (true)
                {
                    DispatcherTask task;

                    lock (_sync)
                    {
                        if (_shutdown)
                        {// shutdown should be synchronized
                            break;
                        }

                        try
                        {
                            task = _taskQueue.Dequeue();
                        }
                        catch (InvalidOperationException)
                        {// if there were no tasks, we wait. Since we've got the lock, noone added anything yet
                            Monitor.Wait(_sync);

                            continue;
                        }
                    }

                    // if there's a task, process it asynchronously
                    lock (task)
                    {
                        task.Result = task.Task.DynamicInvoke(task.Params);

                        // notify waiting thread about completeion
                        Monitor.PulseAll(task);
                    }
                }
            }
            catch (Exception e)
            {
                Tracer.Critical("Exception in SynchronizedDispatcherThread \"" + Thread.CurrentThread.Name + "\"", e);
            }
        }


        /// <summary>
        /// Creates new <code>SynchronizedDispatcherThread</code>, the object is initialized and thread is started here.
        /// </summary>
        public SynchronizedDispatcherThread()
        {
            _thread = new Thread(Run) {IsBackground = true};

            lock (_sync)
            {
                _thread.Start();

                // we wait for the thread to set it's name
                // no need for that actually, but it can be useful in the future to pass thread-specific parameters from thread to the object
                Monitor.Wait(_sync);
            }
        }

        /// <summary>
        /// Invokes specified delegate with parameters on the dispatcher thread synchronously.
        /// </summary>
        /// <param name="method">delegate to run on the thread</param>
        /// <param name="args">arguments to supply to the delegate</param>
        /// <returns>result of an action</returns>
        public object Invoke(Delegate method, object[] args)
        {
            // create the task
            DispatcherTask task = new DispatcherTask{Task = method, Params = args};

            // we don't want the task to be completed before we start waiting for that, so the outer lock
            lock (task)
            {
                lock (_sync)
                {
                    _taskQueue.Enqueue(task);

                    Monitor.PulseAll(_sync);
                }

                // until this point, evaluation could not start
                Monitor.Wait(task);
                // and when we're done waiting, we know that the result was already set

                return task.Result;
            }
        }

        /// <summary>
        /// Tells whether you're on the same thread with the dispatcher or not. If you are, then no <code>Invoke</code> is required. In fact,
        /// synchronous <code>Invoke</code> will deadlock your thread.
        /// </summary>
        public bool InvokeRequired
        {
            // um, well, not tested actually
            get { return _thread != Thread.CurrentThread; }
        }

        /// <summary>
        /// Tells the dispatcher to shutdown its worker thread.
        /// </summary>
        public void Terminate()
        {
            lock (_sync)
            {
                // on the contrary to the Dispatcher from WPF, we don't have any priorities,
                // so we just shut the thread down after it finishes current work (or immediately, if it hasn't any)
                _shutdown = true;

                Monitor.PulseAll(_sync);
            }
        }
    }
}