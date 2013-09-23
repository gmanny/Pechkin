using System;
using Pechkin.EventHandlers;
using Pechkin.Util;

namespace Pechkin
{
    internal class Proxy : MarshalByRefObject, IPechkin
    {
        private readonly Delegate invoker = null;

        private readonly IPechkin remoteInstance;

        public bool IsDisposed { get; private set; }

        internal Proxy(IPechkin remote, Delegate invoker)
        {
            this.remoteInstance = remote;
            this.invoker = invoker;
            this.IsDisposed = remote.IsDisposed;

            // For all these event handlers, making sure to re-signal
            // using the PROXY as arg A, not the Remote, otherwise
            // synchronization could break

            remote.Begin += (a, b) =>
            {
                if (this.Begin != null)
                {
                    this.Begin(this, b);
                }
            };

            remote.Error += (a, b) =>
            {
                if (this.Error != null)
                {
                    this.Error(this, b);
                }
            };

            remote.Finished += (a, b) =>
            {
                if (this.Finished != null)
                {
                    this.Finished(this, b);
                }
            };

            remote.PhaseChanged += (a, b, c) =>
            {
                if (this.PhaseChanged != null)
                {
                    this.PhaseChanged(this, b, c);
                }
            };

            remote.ProgressChanged += (a, b, c) =>
            {
                if (this.ProgressChanged != null)
                {
                    this.ProgressChanged(this, b, c);
                }
            };

            remote.Warning += (a, b) =>
            {
                if (this.Warning != null)
                {
                    this.Warning(this, b);
                }
            };
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing,
        /// or resetting unmanaged resources.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">Thrown when the object has already been disposed.</exception>
        public void Dispose()
        {
            Func<object> del = () =>
            {
                this.remoteInstance.Dispose();
                return null;
            };

            this.Invoke(del);

            this.IsDisposed = true;

            if (this.Disposed != null)
            {
                this.Disposed(this);
            }
        }

        private void VerifyNotDisposed()
        {
            if (this.IsDisposed || this.remoteInstance.IsDisposed)
            {
                throw new ObjectDisposedException("Cannot perform operation; converter is disposed");
            }
        }
  
        private object Invoke(Func<object> del)
        {
            this.VerifyNotDisposed();

            return this.invoker.DynamicInvoke(del);
        }

        public byte[] Convert(ObjectConfig doc, string html)
        {
            Func<object> del = () =>
            {
                return this.remoteInstance.Convert(doc, html);
            };

            return this.Invoke(del) as byte[];
        }

        public byte[] Convert(ObjectConfig doc, byte[] html)
        {
            Func<object> del = () =>
            {
                return this.remoteInstance.Convert(doc, html);
            };

            return this.Invoke(del) as byte[];
        }

        public byte[] Convert(ObjectConfig doc)
        {
            Func<object> del = () =>
            {
                return this.remoteInstance.Convert(doc);
            };

            return this.Invoke(del) as byte[];
        }

        public byte[] Convert(string html)
        {
            Func<object> del = () =>
            {
                return this.remoteInstance.Convert(html);
            };

            return this.Invoke(del) as byte[];
        }

        public byte[] Convert(byte[] html)
        {
            Func<object> del = () =>
            {
                return this.remoteInstance.Convert(html);
            };

            return this.Invoke(del) as byte[];
        }

        public byte[] Convert(Uri url)
        {
            Func<object> del = () =>
            {
                return this.remoteInstance.Convert(url);
            };

            return this.Invoke(del) as byte[];
        }

        public event BeginEventHandler Begin;

        public event WarningEventHandler Warning;

        public event ErrorEventHandler Error;

        public event PhaseChangedEventHandler PhaseChanged;

        public event ProgressChangedEventHandler ProgressChanged;

        public event FinishEventHandler Finished;

        public event DisposedEventHandler Disposed;

        public int CurrentPhase
        {
            get
            {
                Func<object> del = () =>
                {
                    return this.remoteInstance.CurrentPhase;
                };

                return (int)this.Invoke(del);
            }
        }

        public int PhaseCount
        {
            get
            {
                Func<object> del = () =>
                {
                    return this.remoteInstance.PhaseCount;
                };

                return (int)this.Invoke(del);
            }
        }

        public string PhaseDescription
        {
            get
            {
                Func<object> del = () =>
                {
                    return this.remoteInstance.PhaseDescription;
                };

                return this.Invoke(del).ToString();
            }
        }

        public string ProgressString
        {
            get
            {
                Func<object> del = () =>
                {
                    return this.remoteInstance.ProgressString;
                };

                return this.Invoke(del).ToString();
            }
        }

        public int HttpErrorCode
        {
            get
            {
                Func<object> del = () =>
                {
                    return this.remoteInstance.HttpErrorCode;
                };

                return (int)this.Invoke(del);
            }
        }
    }
}