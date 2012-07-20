using System;
using System.Text;
using Common.Logging;
using Pechkin.EventHandlers;

namespace Pechkin
{
    /// <summary>
    /// Simple HTML to PDF converter.
    /// 
    /// This class isn't thread safe and should be used from one thread. 
    /// Even two objects can't be used from different thread simultaneously. For that purpose you should use
    /// <code>SynchronizedPechkin</code> from <code>Pechkin.Synchronized</code> package.
    /// </summary>
    public class SimplePechkin : IPechkin
    {
        private readonly ILog _log = LogManager.GetCurrentClassLogger();

        private readonly GlobalConfig _globalConfig;
        private IntPtr _globalConfigUnmanaged;
        private IntPtr _converter = IntPtr.Zero;
        private bool _reinitConverter;

        /// <summary>
        /// This event happens every time the conversion starts
        /// </summary>
        public event BeginEventHandler Begin;

        protected virtual void OnBegin(IntPtr converter)
        {
            int expectedPhaseCount = PechkinStatic.GetPhaseCount(converter);

            if (_log.IsTraceEnabled)
            {
                _log.Trace("Conversion started, " + expectedPhaseCount + " phases awaiting");
            }

            BeginEventHandler handler = Begin;
            try
            {
                if (handler != null) handler(this, expectedPhaseCount);
            } catch (Exception e)
            {
                _log.Warn("Exception in Begin event handler", e);
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
                _log.Warn("Conversion Warning: " + warningText);
            }

            WarningEventHandler handler = Warning;
            try
            {
                if (handler != null) handler(this, warningText);
            }
            catch (Exception e)
            {
                _log.Warn("Exception in Warning event handler", e);
            }
        }

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
                _log.Error("Conversion Error: " + errorText);
            }

            ErrorEventHandler handler = Error;
            try
            {
                if (handler != null) handler(this, errorText);
            }
            catch (Exception e)
            {
                _log.Warn("Exception in Error event handler", e);
            }
        }

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
                _log.Trace("Conversion Phase Changed: #" + phaseNumber + " " + phaseDescription);
            }

            PhaseChangedEventHandler handler = PhaseChanged;
            try
            {
                if (handler != null) handler(this, phaseNumber, phaseDescription);
            }
            catch (Exception e)
            {
                _log.Warn("Exception in PhaseChange event handler", e);
            }
        }

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
                _log.Trace("Conversion Progress Changed: (" + progress + ") " + progressDescription);
            }

            ProgressChangedEventHandler handler = ProgressChanged;
            try
            {
                if (handler != null) handler(this, progress, progressDescription);
            }
            catch (Exception e)
            {
                _log.Warn("Exception in Progress event handler", e);
            }
        }

        /// <summary>
        /// This event handler is fired when conversion is finished.
        /// </summary>
        public event FinishEventHandler Finished;

        protected virtual void OnFinished(IntPtr converter, int success)
        {
            if (_log.IsTraceEnabled)
            {
                _log.Trace("Conversion Finished: " + (success != 0 ? "Succeede" : "Failed"));
            }

            FinishEventHandler handler = Finished;
            try
            {
                if (handler != null) handler(this, success != 0);
            }
            catch (Exception e)
            {
                _log.Warn("Exception in Finish event handler", e);
            }
        }

        private void CreateConverter()
        {
            if (!_converter.Equals(IntPtr.Zero))
            {
                PechkinStatic.DestroyConverter(_converter);

                if (_log.IsTraceEnabled)
                    _log.Trace("Destroyed previous converter");
            }

            // the damn lib... we can't reuse anything
            _globalConfigUnmanaged = _globalConfig.CreateGlobalConfig();
            _converter = PechkinStatic.CreateConverter(_globalConfigUnmanaged);

            if (_log.IsTraceEnabled)
                _log.Trace("Created converter");

            PechkinStatic.SetErrorCallback(_converter, OnError);
            PechkinStatic.SetWarningCallback(_converter, OnWarning);
            PechkinStatic.SetPhaseChangeCallback(_converter, OnPhaseChanged);
            PechkinStatic.SetProgressChangeCallback(_converter, OnProgressChanged);
            PechkinStatic.SetFinishedCallback(_converter, OnFinished);

            if (_log.IsTraceEnabled)
                _log.Trace("Added callbacks to converter");

            _reinitConverter = false;
        }

        /// <summary>
        /// Constructs HTML to PDF converter instance from <code>GlobalConfig</code>.
        /// </summary>
        /// <param name="config">global configuration object</param>
        public SimplePechkin(GlobalConfig config)
        {
            PechkinStatic.InitLib(false);

            _globalConfig = config;
            
            if (_log.IsTraceEnabled)
                _log.Trace("Created global config");

            CreateConverter();
        }

        ~SimplePechkin()
        {
            if (!_converter.Equals(IntPtr.Zero))
            {
                PechkinStatic.DestroyConverter(_converter);
            }
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
            if (_reinitConverter)
            {
                CreateConverter();
            }

            // create unmanaged object config
            IntPtr objConf = doc.CreateObjectConfig();

            if (_log.IsTraceEnabled)
                _log.Trace("Created object config");

            // add object to converter
            PechkinStatic.AddObject(_converter, objConf, html);

            if (_log.IsTraceEnabled)
                _log.Trace("Added object to converter");

            try
            {
                // run conversion process
                if (!PechkinStatic.PerformConversion(_converter))
                {
                    if (_log.IsTraceEnabled)
                        _log.Trace("Conversion failed, null returned");

                    return null;
                }

                // get output
                return PechkinStatic.GetConverterResult(_converter);
            }
            finally
            {
                // next time we'll need a new one, but for now we'll preserve the old one for the properties (such as http error code)
                // to work properly
                _reinitConverter = true;
            }
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
            get { return PechkinStatic.GetPhaseNumber(_converter); }
        }
        /// <summary>
        /// Phase count for the current conversion process.
        /// 
        /// We recommend to use this property only inside the event handlers.
        /// </summary>
        public int PhaseCount
        {
            get { return PechkinStatic.GetPhaseCount(_converter); }
        }
        /// <summary>
        /// Current phase string description for the converter.
        /// 
        /// We recommend to use this property only inside the event handlers.
        /// </summary>
        public string PhaseDescription
        {
            get { return PechkinStatic.GetPhaseDescription(_converter, CurrentPhase); }
        }
        /// <summary>
        /// Current progress string description. It includes percent count, btw.
        /// 
        /// We recommend to use this property only inside the event handlers.
        /// </summary>
        public string ProgressString
        {
            get { return PechkinStatic.GetProgressDescription(_converter); }
        }
        /// <summary>
        /// Error code returned by server when converter tried to request the page or the resource. Should be available after failed conversion attempt.
        /// </summary>
        public int HttpErrorCode
        {
            get { return PechkinStatic.GetHttpErrorCode(_converter); }
        }
    }
}
