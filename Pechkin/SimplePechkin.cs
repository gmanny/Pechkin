using System;
using System.Linq;
using System.Text;
using System.Threading;
using Common.Logging;
using Pechkin.EventHandlers;
using Pechkin.Util;

namespace Pechkin
{
    /// <summary>
    /// Covers the necessary converter functionality, for internal
    /// use behind a remote proxy implementing the same interface.
    /// </summary>
    [Serializable]
    internal class SimplePechkin : MarshalByRefObject, IPechkin
    {
        public bool IsDisposed { get; private set; }

        private readonly ILog _log = LogManager.GetCurrentClassLogger();

        private readonly GlobalConfig _globalConfig;
        private IntPtr _globalConfigUnmanaged;
        private IntPtr _converter = IntPtr.Zero;

        public event DisposedEventHandler Disposed;

        #region events

        /// <summary>
        /// This event happens every time the conversion starts
        /// </summary>
        public event BeginEventHandler Begin;

        protected virtual void OnBegin(IntPtr converter)
        {
            int expectedPhaseCount = PechkinStatic.GetPhaseCount(converter);

            if (_log.IsTraceEnabled)
            {
                _log.Trace("T:" + Thread.CurrentThread.Name + " Conversion started, " + expectedPhaseCount + " phases awaiting");
            }

            BeginEventHandler handler = Begin;
            try
            {
                if (handler != null)
                    handler(this, expectedPhaseCount);
            }
            catch (Exception e)
            {
                _log.Warn("T:" + Thread.CurrentThread.Name + " Exception in Begin event handler", e);
            }
        }

        /// <summary>
        /// This event handler is called whenever warning happens during conversion process.
        /// 
        /// You can also see javascript errors and warnings if you enable <code>SetJavascriptDebugMode</code> in <code>ObjectConfig</code>
        /// </summary>
        public event WarningEventHandler Warning;

        protected virtual void OnWarning(IntPtr converter, string warningText)
        {
            if (_log.IsTraceEnabled)
            {
                _log.Warn("T:" + Thread.CurrentThread.Name + " Conversion Warning: " + warningText);
            }

            WarningEventHandler handler = Warning;
            try
            {
                if (handler != null)
                    handler(this, warningText);
            }
            catch (Exception e)
            {
                _log.Warn("T:" + Thread.CurrentThread.Name + " Exception in Warning event handler", e);
            }
        }

        private StringCallback _onWarningDelegate;

        /// <summary>
        /// Event handler is called whenever error happens during conversion process.
        /// 
        /// Error typically means that conversion will be terminated.
        /// </summary>
        public event ErrorEventHandler Error;

        protected virtual void OnError(IntPtr converter, string errorText)
        {
            if (_log.IsTraceEnabled)
            {
                _log.Error("T:" + Thread.CurrentThread.Name + " Conversion Error: " + errorText);
            }

            ErrorEventHandler handler = Error;
            try
            {
                if (handler != null)
                    handler(this, errorText);
            }
            catch (Exception e)
            {
                _log.Warn("T:" + Thread.CurrentThread.Name + " Exception in Error event handler", e);
            }
        }

        private StringCallback _onErrorDelegate;

        /// <summary>
        /// This event handler signals phase change of the conversion process.
        /// </summary>
        public event PhaseChangedEventHandler PhaseChanged;

        protected virtual void OnPhaseChanged(IntPtr converter)
        {
            int phaseNumber = PechkinStatic.GetPhaseNumber(converter);
            string phaseDescription = PechkinStatic.GetPhaseDescription(converter, phaseNumber);

            if (_log.IsTraceEnabled)
            {
                _log.Trace("T:" + Thread.CurrentThread.Name + " Conversion Phase Changed: #" + phaseNumber + " " + phaseDescription);
            }

            PhaseChangedEventHandler handler = PhaseChanged;
            try
            {
                if (handler != null)
                    handler(this, phaseNumber, phaseDescription);
            }
            catch (Exception e)
            {
                _log.Warn("T:" + Thread.CurrentThread.Name + " Exception in PhaseChange event handler", e);
            }
        }

        private VoidCallback _onPhaseChangedDelegate;

        /// <summary>
        /// This event handler signals progress change of the conversion process.
        /// 
        /// Number of percents is included in text description.
        /// </summary>
        public event ProgressChangedEventHandler ProgressChanged;

        protected virtual void OnProgressChanged(IntPtr converter, int progress)
        {
            string progressDescription = PechkinStatic.GetProgressDescription(converter);

            if (_log.IsTraceEnabled)
            {
                _log.Trace("T:" + Thread.CurrentThread.Name + " Conversion Progress Changed: (" + progress + ") " + progressDescription);
            }

            ProgressChangedEventHandler handler = ProgressChanged;
            try
            {
                if (handler != null)
                    handler(this, progress, progressDescription);
            }
            catch (Exception e)
            {
                _log.Warn("T:" + Thread.CurrentThread.Name + " Exception in Progress event handler", e);
            }
        }

        private IntCallback _onProgressChangedDelegate;

        /// <summary>
        /// This event handler is fired when conversion is finished.
        /// </summary>
        public event FinishEventHandler Finished;

        protected virtual void OnFinished(IntPtr converter, int success)
        {
            if (_log.IsTraceEnabled)
            {
                _log.Trace("T:" + Thread.CurrentThread.Name + " Conversion Finished: " + (success != 0 ? "Succeede" : "Failed"));
            }

            FinishEventHandler handler = Finished;
            try
            {
                if (handler != null)
                    handler(this, success != 0);
            }
            catch (Exception e)
            {
                _log.Warn("T:" + Thread.CurrentThread.Name + " Exception in Finish event handler", e);
            }
        }

        private IntCallback _onFinishedDelegate;

        #endregion

        /// <summary>
        /// Constructs HTML to PDF converter instance from <code>GlobalConfig</code>.
        /// </summary>
        /// <param name="config">global configuration object</param>
        public SimplePechkin(GlobalConfig config)
        {
            this._onErrorDelegate = new StringCallback(this.OnError);
            this._onFinishedDelegate = new IntCallback(this.OnFinished);
            this._onPhaseChangedDelegate = new VoidCallback(this.OnPhaseChanged);
            this._onProgressChangedDelegate = new IntCallback(this.OnProgressChanged);
            this._onWarningDelegate = new StringCallback(this.OnWarning);

            if (_log.IsTraceEnabled)
                _log.Trace("T:" + Thread.CurrentThread.Name + " Creating SimplePechkin");

            _globalConfig = config;
            
            if (_log.IsTraceEnabled)
                _log.Trace("T:" + Thread.CurrentThread.Name + " Created global config");

            this.IsDisposed = false;
        }

        public void Dispose()
        {
            if (!_converter.Equals(IntPtr.Zero))
            {
                if (_log.IsTraceEnabled)
                    _log.Trace("T:" + Thread.CurrentThread.Name + " Releasing unmanaged converter");

                PechkinStatic.DestroyConverter(_converter);
            }

            this.IsDisposed = true;

            if (this.Disposed != null)
            {
                this.Disposed(this);
            }
        }

        private void CreateConverter()
        {
            if (!_converter.Equals(IntPtr.Zero))
            {
                PechkinStatic.DestroyConverter(_converter);

                if (_log.IsTraceEnabled)
                    _log.Trace("T:" + Thread.CurrentThread.Name + " Destroyed previous converter");
            }

            // the damn lib... we can't reuse anything
            _globalConfigUnmanaged = _globalConfig.CreateGlobalConfig();
            _converter = PechkinStatic.CreateConverter(_globalConfigUnmanaged);

            if (_log.IsTraceEnabled)
                _log.Trace("T:" + Thread.CurrentThread.Name + " Created converter");

            PechkinStatic.SetErrorCallback(_converter, _onErrorDelegate);
            PechkinStatic.SetWarningCallback(_converter, _onWarningDelegate);
            PechkinStatic.SetPhaseChangedCallback(_converter, _onPhaseChangedDelegate);
            PechkinStatic.SetProgressChangedCallback(_converter, _onProgressChangedDelegate);
            PechkinStatic.SetFinishedCallback(_converter, _onFinishedDelegate);

            if (_log.IsTraceEnabled)
                _log.Trace("T:" + Thread.CurrentThread.Name + " Added callbacks to converter");
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
            CreateConverter();

            // create unmanaged object config
            IntPtr objConf = doc.CreateObjectConfig();

            if (_log.IsTraceEnabled)
                _log.Trace("T:" + Thread.CurrentThread.Name + " Created object config");

            // add object to converter
            PechkinStatic.AddObject(_converter, objConf, html);

            if (_log.IsTraceEnabled)
                _log.Trace("T:" + Thread.CurrentThread.Name + " Added object to converter");

            // run OnBegin
            OnBegin(_converter);

            // run conversion process
            if (!PechkinStatic.PerformConversion(_converter))
            {
                if (_log.IsTraceEnabled)
                    _log.Trace("T:" + Thread.CurrentThread.Name + " Conversion failed, null returned");

                return null;
            }

            // get output
            return PechkinStatic.GetConverterResult(_converter);
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
            return Convert(doc, Encoding.UTF8.GetBytes(html));
        }

        /// <summary>
        /// Converts external HTML resource into PDF.
        /// </summary>
        /// <param name="doc">document parameters, <code>ObjectConfig.SetPageUri</code> should be set</param>
        /// <returns>PDF document body</returns>
        public byte[] Convert(ObjectConfig doc)
        {
            return Convert(doc, (byte[])null);
        }

        /// <summary>
        /// Converts HTML string to PDF with default settings.
        /// </summary>
        /// <param name="html">HTML string</param>
        /// <returns>PDF document body</returns>
        public byte[] Convert(string html)
        {
            return Convert(new ObjectConfig(), html);
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
            return Convert(new ObjectConfig(), html);
        }

        /// <summary>
        /// Converts HTML page at specified URL to PDF with default settings.
        /// </summary>
        /// <param name="url">url of page, can be either http/https or file link</param>
        /// <returns>PDF document body</returns>
        public byte[] Convert(Uri url)
        {
            return Convert(new ObjectConfig().SetPageUri(url.AbsoluteUri));
        }

        // some properties for convenience

        /// <summary>
        /// Current phase number for the converter.
        /// 
        /// We recommend to use this property only inside the event handlers.
        /// </summary>
        public int CurrentPhase
        {
            get
            {
                return PechkinStatic.GetPhaseNumber(_converter);
            }
        }

        /// <summary>
        /// Phase count for the current conversion process.
        /// 
        /// We recommend to use this property only inside the event handlers.
        /// </summary>
        public int PhaseCount
        {
            get
            {
                return PechkinStatic.GetPhaseCount(_converter);
            }
        }

        /// <summary>
        /// Current phase string description for the converter.
        /// 
        /// We recommend to use this property only inside the event handlers.
        /// </summary>
        public string PhaseDescription
        {
            get
            {
                return PechkinStatic.GetPhaseDescription(_converter, CurrentPhase);
            }
        }

        /// <summary>
        /// Current progress string description. It includes percent count, btw.
        /// 
        /// We recommend to use this property only inside the event handlers.
        /// </summary>
        public string ProgressString
        {
            get
            {
                return PechkinStatic.GetProgressDescription(_converter);
            }
        }

        /// <summary>
        /// Error code returned by server when converter tried to request the page or the resource. Should be available after failed conversion attempt.
        /// </summary>
        public int HttpErrorCode
        {
            get
            {
                return PechkinStatic.GetHttpErrorCode(_converter);
            }
        }
    }
}