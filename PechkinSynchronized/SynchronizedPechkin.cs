using System;
using System.ComponentModel;
using Pechkin.EventHandlers;
using Pechkin.Synchronized.Util;
using ProgressChangedEventHandler = Pechkin.EventHandlers.ProgressChangedEventHandler;

namespace Pechkin.Synchronized
{
    /// <summary>
    /// This class implements blockable HTML to PDF converter. That is, you can create and use multiple objects
    /// in different threads and can work with one object from different threads.
    /// 
    /// There could errors when using this class mixed with <code>SimplePechkin</code>, so we advise strongly
    /// that you use only <code>SimplePechkin</code> or <code>SynchronizedPechkin</code>, but not both in your
    /// project.
    /// </summary>
    public class SynchronizedPechkin : IPechkin
    {
        //private readonly ILog _log = LogManager.GetCurrentClassLogger();

        private readonly IPechkin _converter;
        private static ISynchronizeInvoke _synchronizer = new DispatcherThread();

        /// <summary>
        /// This method is used to stop service thread when executing tests. 
        /// When running in production, everything is automatically cleaned up
        /// and no action needed.
        /// </summary>
        public static void ClearBeforeExit()
        {
            if (_synchronizer != null)
            {
                DispatcherThread dispatcherThread = _synchronizer as DispatcherThread;
                if (dispatcherThread != null) dispatcherThread.Terminate();
                _synchronizer = null;
            }
        }

        /// <summary>
        /// Constructs HTML to PDF converter instance from <code>GlobalConfig</code>.
        /// </summary>
        /// <param name="config">global configuration object</param>
        public SynchronizedPechkin(GlobalConfig config)
        {
            _converter = (IPechkin) _synchronizer.Invoke((Func<GlobalConfig, IPechkin>)(cfg => new SimplePechkin(cfg)), new[] { config });

            //_synchronizer.Invoke((Action<IPechkin, BeginEventHandler>)((conv, handler) => { conv.Begin += handler; }), new object[] { _converter, Begin });
            //_synchronizer.Invoke((Action<IPechkin, WarningEventHandler>)((conv, handler) => { conv.Warning += handler; }), new object[] { _converter, Warning });
            //_synchronizer.Invoke((Action<IPechkin, ErrorEventHandler>)((conv, handler) => { conv.Error += handler; }), new object[] { _converter, Error });
            //_synchronizer.Invoke((Action<IPechkin, PhaseChangedEventHandler>)((conv, handler) => { conv.PhaseChanged += handler; }), new object[] { _converter, PhaseChanged });
            //_synchronizer.Invoke((Action<IPechkin, ProgressChangedEventHandler>)((conv, handler) => { conv.ProgressChanged += handler; }), new object[] { _converter, ProgressChanged });
            //_synchronizer.Invoke((Action<IPechkin, FinishEventHandler>)((conv, handler) => { conv.Finished += handler; }), new object[] { _converter, Finished });
        }

        /// <summary>
        /// Runs conversion process.
        /// 
        /// Allows to convert both external HTML resource and HTML string.
        /// </summary>
        /// <param name="doc">document parameters</param>
        /// <param name="html">document body, ignored if <code>ObjectConfig.SetPageUri</code> is set</param>
        /// <returns>PDF document body</returns>
        public byte[] Convert(ObjectConfig doc, string html)
        {
            return (byte[])_synchronizer.Invoke((Func<IPechkin, ObjectConfig, string, byte[]>)((conv, obj, txt) => conv.Convert(obj, txt)), new object[]{ _converter, doc, html });
        }

        /// <summary>
        /// Runs conversion process.
        /// 
        /// Allows to convert both external HTML resource and HTML string.
        /// 
        /// Takes html source as a byte array for when you don't know the encoding.
        /// </summary>
        /// <param name="doc">document parameters</param>
        /// <param name="html">document body, ignored if <code>ObjectConfig.SetPageUri</code> is set</param>
        /// <returns>PDF document body</returns>
        public byte[] Convert(ObjectConfig doc, byte[] html)
        {
            return (byte[])_synchronizer.Invoke((Func<IPechkin, ObjectConfig, byte[], byte[]>)((conv, obj, txt) => conv.Convert(obj, txt)), new object[] { _converter, doc, html });
        }

        /// <summary>
        /// Converts external HTML resource into PDF.
        /// </summary>
        /// <param name="doc">document parameters, <code>ObjectConfig.SetPageUri</code> should be set</param>
        /// <returns>PDF document body</returns>
        public byte[] Convert(ObjectConfig doc)
        {
            return (byte[])_synchronizer.Invoke((Func<IPechkin, ObjectConfig, byte[]>)((conv, obj) => conv.Convert(obj)), new object[] { _converter, doc });
        }

        /// <summary>
        /// Converts HTML string to PDF with default settings.
        /// </summary>
        /// <param name="html">HTML string</param>
        /// <returns>PDF document body</returns>
        public byte[] Convert(string html)
        {
            return (byte[])_synchronizer.Invoke((Func<IPechkin, string, byte[]>)((conv, txt) => conv.Convert(txt)), new object[] { _converter, html });
        }

        /// <summary>
        /// Converts HTML string to PDF with default settings.
        /// 
        /// Takes html source as a byte array for when you don't know the encoding.
        /// </summary>
        /// <param name="html">HTML string</param>
        /// <returns>PDF document body</returns>
        public byte[] Convert(byte[] html)
        {
            return (byte[])_synchronizer.Invoke((Func<IPechkin, byte[], byte[]>)((conv, txt) => conv.Convert(txt)), new object[] { _converter, html });
        }

        /// <summary>
        /// Converts HTML page at specified URL to PDF with default settings.
        /// </summary>
        /// <param name="url">url of page, can be either http/https or file link</param>
        /// <returns>PDF document body</returns>
        public byte[] Convert(Uri url)
        {
            return (byte[])_synchronizer.Invoke((Func<IPechkin, Uri, byte[]>)((conv, uri) => conv.Convert(uri)), new object[] { _converter, url });
        }

        /// <summary>
        /// This event happens every time the conversion starts
        /// </summary>
        public event BeginEventHandler Begin
        {
            add { _synchronizer.Invoke((Action<IPechkin, BeginEventHandler>)((conv, handler) => { conv.Begin += handler; }), new object[] { _converter, value }); }
            remove { _synchronizer.Invoke((Action<IPechkin, BeginEventHandler>)((conv, handler) => { conv.Begin -= handler; }), new object[] { _converter, value }); }
        }

        /// <summary>
        /// This event handler is called whenever warning happens during conversion process.
        /// 
        /// You can also see javascript errors and warnings if you enable <code>SetJavascriptDebugMode</code> in <code>ObjectConfig</code>
        /// </summary>
        public event WarningEventHandler Warning
        {
            add { _synchronizer.Invoke((Action<IPechkin, WarningEventHandler>)((conv, handler) => { conv.Warning += handler; }), new object[] { _converter, value }); }
            remove { _synchronizer.Invoke((Action<IPechkin, WarningEventHandler>)((conv, handler) => { conv.Warning -= handler; }), new object[] { _converter, value }); }
        }

        /// <summary>
        /// Event handler is called whenever error happens during conversion process.
        /// 
        /// Error typically means that conversion will be terminated.
        /// </summary>
        public event ErrorEventHandler Error
        {
            add { _synchronizer.Invoke((Action<IPechkin, ErrorEventHandler>)((conv, handler) => { conv.Error += handler; }), new object[] { _converter, value }); }
            remove { _synchronizer.Invoke((Action<IPechkin, ErrorEventHandler>)((conv, handler) => { conv.Error -= handler; }), new object[] { _converter, value }); }
        }

        /// <summary>
        /// This event handler signals phase change of the conversion process.
        /// </summary>
        public event PhaseChangedEventHandler PhaseChanged
        {
            add { _synchronizer.Invoke((Action<IPechkin, PhaseChangedEventHandler>)((conv, handler) => { conv.PhaseChanged += handler; }), new object[] { _converter, value }); }
            remove { _synchronizer.Invoke((Action<IPechkin, PhaseChangedEventHandler>)((conv, handler) => { conv.PhaseChanged -= handler; }), new object[] { _converter, value }); }
        }

        /// <summary>
        /// This event handler signals progress change of the conversion process.
        /// 
        /// Number of percents is included in text description.
        /// </summary>
        public event ProgressChangedEventHandler ProgressChanged
        {
            add { _synchronizer.Invoke((Action<IPechkin, ProgressChangedEventHandler>)((conv, handler) => { conv.ProgressChanged += handler; }), new object[] { _converter, value }); }
            remove { _synchronizer.Invoke((Action<IPechkin, ProgressChangedEventHandler>)((conv, handler) => { conv.ProgressChanged -= handler; }), new object[] { _converter, value }); }
        }

        /// <summary>
        /// This event handler is fired when conversion is finished.
        /// </summary>
        public event FinishEventHandler Finished
        {
            add { _synchronizer.Invoke((Action<IPechkin, FinishEventHandler>)((conv, handler) => { conv.Finished += handler; }), new object[] { _converter, value }); }
            remove { _synchronizer.Invoke((Action<IPechkin, FinishEventHandler>)((conv, handler) => { conv.Finished -= handler; }), new object[] { _converter, value }); }
        }

        /// <summary>
        /// Current phase number for the converter.
        /// 
        /// We recommend to use this property only inside the event handlers.
        /// </summary>
        public int CurrentPhase
        {
            get { return (int)_synchronizer.Invoke((Func<IPechkin, int>)(conv => conv.CurrentPhase), new[] { _converter }); }
        }

        /// <summary>
        /// Phase count for the current conversion process.
        /// 
        /// We recommend to use this property only inside the event handlers.
        /// </summary>
        public int PhaseCount
        {
            get { return (int)_synchronizer.Invoke((Func<IPechkin, int>)(conv => conv.PhaseCount), new[] { _converter }); }
        }

        /// <summary>
        /// Current phase string description for the converter.
        /// 
        /// We recommend to use this property only inside the event handlers.
        /// </summary>
        public string PhaseDescription
        {
            get { return (string)_synchronizer.Invoke((Func<IPechkin, string>)(conv => conv.PhaseDescription), new[] { _converter }); }
        }

        /// <summary>
        /// Current progress string description. It includes percent count, btw.
        /// 
        /// We recommend to use this property only inside the event handlers.
        /// </summary>
        public string ProgressString
        {
            get { return (string)_synchronizer.Invoke((Func<IPechkin, string>)(conv => conv.ProgressString), new[] { _converter }); }
        }

        /// <summary>
        /// Error code returned by server when converter tried to request the page or the resource. Should be available after failed conversion attempt.
        /// </summary>
        public int HttpErrorCode
        {
            get { return (int)_synchronizer.Invoke((Func<IPechkin, int>)(conv => conv.HttpErrorCode), new[] { _converter }); }
        }
    }
}
