﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Common.Logging;
using Html2Pdf.EventHandlers;
using Pechkin.Util;

namespace Pechkin
{
    /// <summary>
    /// Static class with utility methods for the interface.
    /// </summary>
    [Serializable]
    public static partial class PechkinStatic
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private static bool _inited;
        private static bool _useHack;
        // ReSharper disable NotAccessedField.Local
        private static SimplePechkin _hackObj;
        // ReSharper restore NotAccessedField.Local

        private static AppDomain _appdomain;

        /// <summary>
        /// Initializes wrapped library. This is done automatically when you need it.
        /// </summary>
        /// <param name="useGraphics">use X11 graphics, <code>false</code> in most cases.</param>
        public static void InitLib(bool useGraphics)
        {
            if (_inited)
            {
                return;
            }

            _inited = true;

            String binPath = String.Empty;

            if (!String.IsNullOrEmpty(AppDomain.CurrentDomain.RelativeSearchPath))
            {
                String[] paths = AppDomain.CurrentDomain.RelativeSearchPath.Split(';');
                for (var i = 0; i < paths.Length; i++)
                {
                    paths[i].Remove(0, AppDomain.CurrentDomain.BaseDirectory.Length);
                }
                binPath = String.Join(";", paths);
            }

            _appdomain = AppDomain.CreateDomain("pechkinstatic_internal_domain", null,
                new AppDomainSetup
                {
                    ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                    // Sometimes, like in a web app, your bin folder is not the same
                    // as the base dir.
                    PrivateBinPath = binPath
                });
            _appdomain.Load(Assembly.GetExecutingAssembly().GetName());

            if (Log.IsTraceEnabled)
            {
                Log.Trace("T:" + Thread.CurrentThread.Name + " Initializing library (wkhtmltopdf_init)");
            }

            _appdomain.SetData("bool", useGraphics);
            _appdomain.DoCallBack(() => PechkinBindings.wkhtmltopdf_init((bool)AppDomain.CurrentDomain.GetData("bool") ? 1 : 0));

            if (_useHack)
            {
                _hackObj = new SimplePechkin(new GlobalConfig());
            }

            if (LibInit != null)
            {
                LibInit();
            }
        }

        /// <summary>
        /// Deinitializes library.
        /// </summary>
        public static void DeinitLib()
        {
            if (!_inited)
                return;

            if (LibDeInit != null)
            {
                LibDeInit();
            }

            if (Log.IsTraceEnabled)
            {
                Log.Trace("T:" + Thread.CurrentThread.Name + " Deinitializing library (wkhtmltopdf_deinit)");
            }

            _appdomain.DoCallBack(() => PechkinBindings.wkhtmltopdf_deinit());

            AppDomain.Unload(_appdomain);

            foreach (ProcessModule mod in Process.GetCurrentProcess().Modules)
            {
                if (mod.ModuleName == "wkhtmltox0.dll")
                {
                    while (PechkinBindings.FreeLibrary(mod.BaseAddress))
                    {
                    }
                }
            }

            _hackObj = null;

            _inited = false;
        }

        /// <summary>
        /// Event that happens when library gets initialized. Should be assigned before library init, or it will never fire.
        /// </summary>
        public static event LibInitEventHandler LibInit;
        /// <summary>
        /// Event that happens when library gets de-initialized.
        /// </summary>
        public static event LibDeInitEventHandler LibDeInit;

        /// <summary>
        /// If you're using the library which has a bug that needs at least one object alive for the lifetime of the library
        /// for the conversion to work after the first time, you can use this method
        /// </summary>
        [Obsolete("Bundled library returns good results without any workarounds")]
        public static void EnableSpareObjectWorkaround()
        {
            _useHack = true;
        }

        public static IntPtr CreateGlobalSetting()
        {
            InitLib(false);

            if (Log.IsTraceEnabled)
            {
                Log.Trace("T:" + Thread.CurrentThread.Name + " Creating global settings (wkhtmltopdf_create_global_settings)");
            }

            _appdomain.DoCallBack(() => AppDomain.CurrentDomain.SetData("IntPtr", PechkinBindings.wkhtmltopdf_create_global_settings()));
            return (IntPtr)_appdomain.GetData("IntPtr");
        }

        public static IntPtr CreateObjectSettings()
        {
            InitLib(false);

            if (Log.IsTraceEnabled)
            {
                Log.Trace("T:" + Thread.CurrentThread.Name + " Creating object settings (wkhtmltopdf_create_object_settings)");
            }

            _appdomain.DoCallBack(() => AppDomain.CurrentDomain.SetData("IntPtr", PechkinBindings.wkhtmltopdf_create_object_settings()));
            return (IntPtr)_appdomain.GetData("IntPtr");
        }

        public static int SetGlobalSetting(IntPtr setting, string name, string value)
        {
            InitLib(false);

            if (Log.IsTraceEnabled)
            {
                Log.Trace("T:" + Thread.CurrentThread.Name + " Setting global setting (wkhtmltopdf_set_global_setting)");
            }

            _appdomain.SetData("args", new object[] { setting, name, value });
            _appdomain.DoCallBack(() =>
            { 
                AppDomain domain = AppDomain.CurrentDomain;
                object[] args = (object[])domain.GetData("args");
                var a = (IntPtr)args[0];
                var b = (String)args[1];
                var c = (String)args[2];
                var ret = PechkinBindings.wkhtmltopdf_set_global_setting(a, b, c);
                domain.SetData("ret", ret);
            });
            return (int)_appdomain.GetData("ret");
        }

        public static string GetGlobalSetting(IntPtr setting, string name)
        {
            InitLib(false);

            if (Log.IsTraceEnabled)
            {
                Log.Trace("T:" + Thread.CurrentThread.Name + " Getting global setting (wkhtmltopdf_get_global_setting)");
            }

            _appdomain.SetData("args", new object[] { setting, name });
            _appdomain.DoCallBack(() =>
            {
                AppDomain domain = AppDomain.CurrentDomain;
                object[] args = (object[])domain.GetData("args");
                var a = (IntPtr)args[0];
                var b = (String)args[1];
                var c = new byte[2048]; 
                PechkinBindings.wkhtmltopdf_get_global_setting(a, b, ref c, c.Length);
                domain.SetData("ret", c);
            });
            byte[] buf = (byte[])_appdomain.GetData("ret");

            int walk = 0;
            while (walk < buf.Length && buf[walk] != 0)
            {
                walk++;
            }

            byte[] buf2 = new byte[walk];
            Array.Copy(buf, 0, buf2, 0, walk);

            return Encoding.UTF8.GetString(buf2);
        }

        public static int SetObjectSetting(IntPtr setting, string name, string value)
        {
            InitLib(false);

            if (Log.IsTraceEnabled)
            {
                Log.Trace("T:" + Thread.CurrentThread.Name + " Setting object setting (wkhtmltopdf_set_object_setting)");
            }

            _appdomain.SetData("args", new object[] { setting, name, value });
            _appdomain.DoCallBack(() =>
            {
                AppDomain domain = AppDomain.CurrentDomain;
                object[] args = (object[])domain.GetData("args");
                var a = (IntPtr)args[0];
                var b = (String)args[1];
                var c = (String)args[2];
                domain.SetData("ret", PechkinBindings.wkhtmltopdf_set_object_setting(a, b, c)); 
            });
            return (int)_appdomain.GetData("ret");
        }

        public static string GetObjectSetting(IntPtr setting, string name)
        {
            InitLib(false);

            if (Log.IsTraceEnabled)
            {
                Log.Trace("T:" + Thread.CurrentThread.Name + " Getting object setting (wkhtmltopdf_get_object_setting)");
            }

            _appdomain.SetData("args", new object[] { setting, name });
            _appdomain.DoCallBack(() =>
            {
                AppDomain domain = AppDomain.CurrentDomain;
                object[] args = (object[])domain.GetData("args");
                var a = (IntPtr)args[0];
                var b = (String)args[1];
                var c = new byte[2048];
                PechkinBindings.wkhtmltopdf_get_object_setting(a, b, ref c, c.Length);
                domain.SetData("ret", c);
            });
            byte[] buf = (byte[])_appdomain.GetData("ret");

            int walk = 0;
            while (walk < buf.Length && buf[walk] != 0)
            {
                walk++;
            }

            byte[] buf2 = new byte[walk];
            Array.Copy(buf, 0, buf2, 0, walk);

            return Encoding.UTF8.GetString(buf2);
        }

        public static IntPtr CreateConverter(IntPtr globalSettings)
        {
            InitLib(false);

            if (Log.IsTraceEnabled)
            {
                Log.Trace("T:" + Thread.CurrentThread.Name + " Creating converter (wkhtmltopdf_create_converter)");
            }

            _appdomain.SetData("arg", globalSettings);
            _appdomain.DoCallBack(() =>
            {
                IntPtr arg = (IntPtr)AppDomain.CurrentDomain.GetData("arg");
                AppDomain.CurrentDomain.SetData("IntPtr", PechkinBindings.wkhtmltopdf_create_converter(arg));
            });
            return (IntPtr)_appdomain.GetData("IntPtr");
        }

        public static void DestroyConverter(IntPtr converter)
        {
            InitLib(false);

            if (Log.IsTraceEnabled)
            {
                Log.Trace("T:" + Thread.CurrentThread.Name + " Destroying converter (wkhtmltopdf_destroy_converter)");
            }

            _appdomain.SetData("arg", converter);
            _appdomain.DoCallBack(() =>
            {
                IntPtr arg = (IntPtr)AppDomain.CurrentDomain.GetData("arg");
                PechkinBindings.wkhtmltopdf_destroy_converter(arg);
            });
        }

        public static void SetWarningCallback(IntPtr converter, StringCallback callback)
        {
            InitLib(false);

            if (Log.IsTraceEnabled)
            {
                Log.Trace("T:" + Thread.CurrentThread.Name + " Setting warning callback (wkhtmltopdf_set_warning_callback)");
            }

            _appdomain.SetData("args", new object[] { converter, callback });
            _appdomain.DoCallBack(() =>
            {
                object[] args = (object[])AppDomain.CurrentDomain.GetData("args");
                PechkinBindings.warning_callback = (StringCallback)args[1];
                PechkinBindings.wkhtmltopdf_set_warning_callback((IntPtr)args[0], (StringCallback)args[1]); 
            });
        }

        public static void SetErrorCallback(IntPtr converter, StringCallback callback)
        {
            InitLib(false);

            if (Log.IsTraceEnabled)
            {
                Log.Trace("T:" + Thread.CurrentThread.Name + " Setting error callback (wkhtmltopdf_set_error_callback)");
            }

            _appdomain.SetData("args", new object[] { converter, callback });
            _appdomain.DoCallBack(() =>
            {
                object[] args = (object[])AppDomain.CurrentDomain.GetData("args");
                PechkinBindings.error_callback = (StringCallback)args[1];
                PechkinBindings.wkhtmltopdf_set_error_callback((IntPtr)args[0], (StringCallback)args[1]);
            });
        }

        public static void SetFinishedCallback(IntPtr converter, IntCallback callback)
        {
            InitLib(false);

            if (Log.IsTraceEnabled)
            {
                Log.Trace("T:" + Thread.CurrentThread.Name + " Setting finished callback (wkhtmltopdf_set_finished_callback)");
            }

            _appdomain.SetData("args", new object[] { converter, callback });
            _appdomain.DoCallBack(() =>
            {
                object[] args = (object[])AppDomain.CurrentDomain.GetData("args");
                PechkinBindings.finished_callback = (IntCallback)args[1];
                PechkinBindings.wkhtmltopdf_set_finished_callback((IntPtr)args[0], (IntCallback)args[1]);
            });
        }

        public static void SetPhaseChangeCallback(IntPtr converter, VoidCallback callback)
        {
            InitLib(false);

            if (Log.IsTraceEnabled)
            {
                Log.Trace("T:" + Thread.CurrentThread.Name + " Setting phase change callback (wkhtmltopdf_set_phase_changed_callback)");
            }

            _appdomain.SetData("args", new object[] { converter, callback });
            _appdomain.DoCallBack(() =>
            {
                object[] args = (object[])AppDomain.CurrentDomain.GetData("args");
                PechkinBindings.phase_changed_callback = (VoidCallback)args[1];
                PechkinBindings.wkhtmltopdf_set_phase_changed_callback((IntPtr)args[0], (VoidCallback)args[1]);
            });
        }

        public static void SetProgressChangeCallback(IntPtr converter, IntCallback callback)
        {
            InitLib(false);

            if (Log.IsTraceEnabled)
            {
                Log.Trace("T:" + Thread.CurrentThread.Name + " Setting progress change callback (wkhtmltopdf_set_progress_changed_callback)");
            }

            _appdomain.SetData("args", new object[] { converter, callback });
            _appdomain.DoCallBack(() =>
            {
                object[] args = (object[])AppDomain.CurrentDomain.GetData("args");
                PechkinBindings.progress_changed_callback = (IntCallback)args[1];
                PechkinBindings.wkhtmltopdf_set_progress_changed_callback((IntPtr)args[0], (IntCallback)args[1]);
            });
        }

        public static bool PerformConversion(IntPtr converter)
        {
            InitLib(false);

            if (Log.IsTraceEnabled)
            {
                Log.Trace("T:" + Thread.CurrentThread.Name + " Starting conversion (wkhtmltopdf_convert)");
            }

            _appdomain.SetData("arg", converter);
            _appdomain.DoCallBack(() =>
            {
                IntPtr arg = (IntPtr)AppDomain.CurrentDomain.GetData("arg");
                AppDomain.CurrentDomain.SetData("int", PechkinBindings.wkhtmltopdf_convert(arg));
            });
            return (int)_appdomain.GetData("int") != 0;
        }

        public static void AddObject(IntPtr converter, IntPtr objectConfig, string html)
        {
            InitLib(false);

            if (Log.IsTraceEnabled)
            {
                Log.Trace("T:" + Thread.CurrentThread.Name + " Adding string object (wkhtmltopdf_add_object)");
            }

            _appdomain.SetData("args", new object[] { converter, objectConfig, html });
            _appdomain.DoCallBack(() =>
            {
                AppDomain domain = AppDomain.CurrentDomain;
                object[] args = (object[])domain.GetData("args");
                var a = (IntPtr)args[0];
                var b = (IntPtr)args[1];
                var c = (String)args[2];
                PechkinBindings.wkhtmltopdf_add_object(a, b, c);
            });
        }

        public static void AddObject(IntPtr converter, IntPtr objectConfig, byte[] html)
        {
            InitLib(false);

            if (Log.IsTraceEnabled)
            {
                Log.Trace("T:" + Thread.CurrentThread.Name + " Adding byte[] object (wkhtmltopdf_add_object)");
            }

            _appdomain.SetData("args", new object[] { converter, objectConfig, html });
            _appdomain.DoCallBack(() =>
            {
                AppDomain domain = AppDomain.CurrentDomain;
                object[] args = (object[])domain.GetData("args");
                var a = (IntPtr)args[0];
                var b = (IntPtr)args[1];
                var c = (byte[])args[2];
                PechkinBindings.wkhtmltopdf_add_object(a, b, c);
            });
        }

        public static int GetPhaseNumber(IntPtr converter)
        {
            InitLib(false);

            if (Log.IsTraceEnabled)
            {
                Log.Trace("T:" + Thread.CurrentThread.Name + " Requesting current phase (wkhtmltopdf_current_phase)");
            }

            _appdomain.SetData("arg", converter);
            _appdomain.DoCallBack(() =>
            {
                IntPtr arg = (IntPtr)AppDomain.CurrentDomain.GetData("arg");
                AppDomain.CurrentDomain.SetData("int", PechkinBindings.wkhtmltopdf_current_phase(arg));
            });
            return (int)_appdomain.GetData("int");
        }

        public static int GetPhaseCount(IntPtr converter)
        {
            InitLib(false);

            if (Log.IsTraceEnabled)
            {
                Log.Trace("T:" + Thread.CurrentThread.Name + " Requesting phase count (wkhtmltopdf_phase_count)");
            }

            _appdomain.SetData("arg", converter);
            _appdomain.DoCallBack(() =>
            {
                IntPtr arg = (IntPtr)AppDomain.CurrentDomain.GetData("arg");
                AppDomain.CurrentDomain.SetData("int", PechkinBindings.wkhtmltopdf_phase_count(arg));
            });
            return (int)_appdomain.GetData("int");
        }

        public static string GetPhaseDescription(IntPtr converter, int phase)
        {
            InitLib(false);

            if (Log.IsTraceEnabled)
            {
                Log.Trace("T:" + Thread.CurrentThread.Name + " Requesting phase description (wkhtmltopdf_phase_description)");
            }

            _appdomain.SetData("args", new object[] { converter, phase });
            _appdomain.DoCallBack(() =>
            {
                AppDomain domain = AppDomain.CurrentDomain;
                object[] args = (object[])domain.GetData("args");
                var a = (IntPtr)args[0];
                var b = (int)args[1];
                domain.SetData("IntPtr", PechkinBindings.wkhtmltopdf_phase_description(a, b));
            });
            return Marshal.PtrToStringAnsi((IntPtr)_appdomain.GetData("IntPtr"));
        }

        public static string GetProgressDescription(IntPtr converter)
        {
            InitLib(false);

            if (Log.IsTraceEnabled)
            {
                Log.Trace("T:" + Thread.CurrentThread.Name + " Requesting progress string (wkhtmltopdf_progress_string)");
            }

            _appdomain.SetData("arg", converter);
            _appdomain.DoCallBack(() =>
            {
                AppDomain domain = AppDomain.CurrentDomain;
                var a = (IntPtr)domain.GetData("arg");
                domain.SetData("IntPtr", PechkinBindings.wkhtmltopdf_progress_string(a));
            });
            return Marshal.PtrToStringAnsi((IntPtr)_appdomain.GetData("IntPtr"));
        }

        public static int GetHttpErrorCode(IntPtr converter)
        {
            InitLib(false);

            if (Log.IsTraceEnabled)
            {
                Log.Trace("T:" + Thread.CurrentThread.Name + " Requesting http error code (wkhtmltopdf_http_error_code)");
            }

            _appdomain.SetData("arg", converter);
            _appdomain.DoCallBack(() =>
            {
                IntPtr arg = (IntPtr)AppDomain.CurrentDomain.GetData("arg");
                AppDomain.CurrentDomain.SetData("int", PechkinBindings.wkhtmltopdf_http_error_code(arg));
            });
            return (int)_appdomain.GetData("int");
        }

        public static byte[] GetConverterResult(IntPtr converter)
        {
            InitLib(false);

            /*
            IntPtr unmanagedBuf;
            long length = PechkinBindings.wkhtmltopdf_get_output(converter, out unmanagedBuf);
            byte[] buf = new byte[length];
            Marshal.Copy(unmanagedBuf, buf, 0, buf.Length);
            return buf;
            */
            if (Log.IsTraceEnabled)
            {
                Log.Trace("T:" + Thread.CurrentThread.Name + " Requesting converter result (wkhtmltopdf_get_output)");
            }

            _appdomain.SetData("intptr", converter);
            _appdomain.DoCallBack(() =>
            {
                IntPtr tmp;
                var len = PechkinBindings.wkhtmltopdf_get_output((IntPtr)AppDomain.CurrentDomain.GetData("intptr"), out tmp);
                var output = new byte[len];
                Marshal.Copy(tmp, output, 0, output.Length);
                AppDomain.CurrentDomain.SetData("ret", output);
            });

            return (byte[])_appdomain.GetData("ret");
        }

        public static string Version
        {
            get
            {
                InitLib(false);

                if (Log.IsTraceEnabled)
                {
                    Log.Trace("T:" + Thread.CurrentThread.Name + " Requesting library version (wkhtmltopdf_version)");
                }

                _appdomain.DoCallBack(() =>
                {
                    AppDomain.CurrentDomain.SetData("string", PechkinBindings.wkhtmltopdf_version());
                });
                return (String)_appdomain.GetData("string");
            }
        }

        public static bool EntendedQtAvailable
        {
            get
            {
                InitLib(false);

                if (Log.IsTraceEnabled)
                {
                    Log.Trace("T:" + Thread.CurrentThread.Name + " Requesting extended Qt availability (wkhtmltopdf_extended_qt)");
                }

                _appdomain.DoCallBack(() =>
                {
                    AppDomain.CurrentDomain.SetData("int", PechkinBindings.wkhtmltopdf_extended_qt());
                });
                return (int)_appdomain.GetData("int") != 0;
            }
        }

        internal class StrPaperSize
        {
            private readonly string _width;
            private readonly string _height;

            public StrPaperSize(string width, string height)
            {
                _width = width;
                _height = height;
            }

            public string Height
            {
                get
                {
                    return _height;
                }
            }

            public string Width
            {
                get
                {
                    return _width;
                }
            }
        }

        internal static readonly Dictionary<PaperKind, StrPaperSize> PaperSizes = new Dictionary<PaperKind, StrPaperSize>();

        static PechkinStatic()
        {
            // paper sizes from http://msdn.microsoft.com/en-us/library/system.drawing.printing.paperkind.aspx
            PaperSizes.Add(PaperKind.Letter, new StrPaperSize("8.5in", "11in"));
            PaperSizes.Add(PaperKind.Legal, new StrPaperSize("8.5in", "14in"));
            PaperSizes.Add(PaperKind.A4, new StrPaperSize("210mm", "297mm"));
            PaperSizes.Add(PaperKind.CSheet, new StrPaperSize("17in", "22in"));
            PaperSizes.Add(PaperKind.DSheet, new StrPaperSize("22in", "34in"));
            PaperSizes.Add(PaperKind.ESheet, new StrPaperSize("34in", "44in"));
            PaperSizes.Add(PaperKind.LetterSmall, new StrPaperSize("8.5in", "11in"));
            PaperSizes.Add(PaperKind.Tabloid, new StrPaperSize("11in", "17in"));
            PaperSizes.Add(PaperKind.Ledger, new StrPaperSize("17in", "11in"));
            PaperSizes.Add(PaperKind.Statement, new StrPaperSize("5.5in", "8.5in"));
            PaperSizes.Add(PaperKind.Executive, new StrPaperSize("7.25in", "10.5in"));
            PaperSizes.Add(PaperKind.A3, new StrPaperSize("297mm", "420mm"));
            PaperSizes.Add(PaperKind.A4Small, new StrPaperSize("210mm", "297mm"));
            PaperSizes.Add(PaperKind.A5, new StrPaperSize("148mm", "210mm"));
            PaperSizes.Add(PaperKind.B4, new StrPaperSize("250mm", "353mm"));
            PaperSizes.Add(PaperKind.B5, new StrPaperSize("176mm", "250mm"));
            PaperSizes.Add(PaperKind.Folio, new StrPaperSize("8.5in", "13in"));
            PaperSizes.Add(PaperKind.Quarto, new StrPaperSize("215mm", "275mm"));
            PaperSizes.Add(PaperKind.Standard10x14, new StrPaperSize("10in", "14in"));
            PaperSizes.Add(PaperKind.Standard11x17, new StrPaperSize("11in", "17in"));
            PaperSizes.Add(PaperKind.Note, new StrPaperSize("8.5in", "11in"));
            PaperSizes.Add(PaperKind.Number9Envelope, new StrPaperSize("3.875in", "8.875in"));
            PaperSizes.Add(PaperKind.Number10Envelope, new StrPaperSize("4.125in", "9.5in"));
            PaperSizes.Add(PaperKind.Number11Envelope, new StrPaperSize("4.5in", "10.375in"));
            PaperSizes.Add(PaperKind.Number12Envelope, new StrPaperSize("4.75in", "11in"));
            PaperSizes.Add(PaperKind.Number14Envelope, new StrPaperSize("5in", "11.5in"));
            PaperSizes.Add(PaperKind.DLEnvelope, new StrPaperSize("110mm", "220mm"));
            PaperSizes.Add(PaperKind.C5Envelope, new StrPaperSize("162mm", "229mm"));
            PaperSizes.Add(PaperKind.C3Envelope, new StrPaperSize("324mm", "458mm"));
            PaperSizes.Add(PaperKind.C4Envelope, new StrPaperSize("229mm", "324mm"));
            PaperSizes.Add(PaperKind.C6Envelope, new StrPaperSize("114mm", "162mm"));
            PaperSizes.Add(PaperKind.C65Envelope, new StrPaperSize("114mm", "229mm"));
            PaperSizes.Add(PaperKind.B4Envelope, new StrPaperSize("250mm", "353mm"));
            PaperSizes.Add(PaperKind.B5Envelope, new StrPaperSize("176mm", "250mm"));
            PaperSizes.Add(PaperKind.B6Envelope, new StrPaperSize("176mm", "125mm"));
            PaperSizes.Add(PaperKind.ItalyEnvelope, new StrPaperSize("110mm", "230mm"));
            PaperSizes.Add(PaperKind.MonarchEnvelope, new StrPaperSize("3.875in", "7.5in"));
            PaperSizes.Add(PaperKind.PersonalEnvelope, new StrPaperSize("3.625in", "6.5in"));
            PaperSizes.Add(PaperKind.USStandardFanfold, new StrPaperSize("14.875in", "11in"));
            PaperSizes.Add(PaperKind.GermanStandardFanfold, new StrPaperSize("8.5in", "12in"));
            PaperSizes.Add(PaperKind.GermanLegalFanfold, new StrPaperSize("8.5in", "13in"));
            PaperSizes.Add(PaperKind.IsoB4, new StrPaperSize("250mm", "353mm"));
            PaperSizes.Add(PaperKind.JapanesePostcard, new StrPaperSize("100mm", "148mm"));
            PaperSizes.Add(PaperKind.Standard9x11, new StrPaperSize("9in", "11in"));
            PaperSizes.Add(PaperKind.Standard10x11, new StrPaperSize("10in", "11in"));
            PaperSizes.Add(PaperKind.Standard15x11, new StrPaperSize("15in", "11in"));
            PaperSizes.Add(PaperKind.InviteEnvelope, new StrPaperSize("220mm", "220mm"));
            PaperSizes.Add(PaperKind.LetterExtra, new StrPaperSize("9.275in", "12in"));
            PaperSizes.Add(PaperKind.LegalExtra, new StrPaperSize("9.275in", "15in"));
            PaperSizes.Add(PaperKind.TabloidExtra, new StrPaperSize("11.69in", "18in"));
            PaperSizes.Add(PaperKind.A4Extra, new StrPaperSize("236mm", "322mm"));
            PaperSizes.Add(PaperKind.LetterTransverse, new StrPaperSize("8.275in", "11in"));
            PaperSizes.Add(PaperKind.A4Transverse, new StrPaperSize("210mm", "297mm"));
            PaperSizes.Add(PaperKind.LetterExtraTransverse, new StrPaperSize("9.275in", "12in"));
            PaperSizes.Add(PaperKind.APlus, new StrPaperSize("227mm", "356mm"));
            PaperSizes.Add(PaperKind.BPlus, new StrPaperSize("305mm", "487mm"));
            PaperSizes.Add(PaperKind.LetterPlus, new StrPaperSize("8.5in", "12.69in"));
            PaperSizes.Add(PaperKind.A4Plus, new StrPaperSize("210mm", "330mm"));
            PaperSizes.Add(PaperKind.A5Transverse, new StrPaperSize("148mm", "210mm"));
            PaperSizes.Add(PaperKind.B5Transverse, new StrPaperSize("182mm", "257mm"));
            PaperSizes.Add(PaperKind.A3Extra, new StrPaperSize("322mm", "445mm"));
            PaperSizes.Add(PaperKind.A5Extra, new StrPaperSize("174mm", "235mm"));
            PaperSizes.Add(PaperKind.B5Extra, new StrPaperSize("201mm", "276mm"));
            PaperSizes.Add(PaperKind.A2, new StrPaperSize("420mm", "594mm"));
            PaperSizes.Add(PaperKind.A3Transverse, new StrPaperSize("297mm", "420mm"));
            PaperSizes.Add(PaperKind.A3ExtraTransverse, new StrPaperSize("322mm", "445mm"));
            PaperSizes.Add(PaperKind.JapaneseDoublePostcard, new StrPaperSize("200mm", "148mm"));
            PaperSizes.Add(PaperKind.A6, new StrPaperSize("105mm", "148mm"));
            PaperSizes.Add(PaperKind.LetterRotated, new StrPaperSize("11in", "8.5in"));
            PaperSizes.Add(PaperKind.A3Rotated, new StrPaperSize("420mm", "297mm"));
            PaperSizes.Add(PaperKind.A4Rotated, new StrPaperSize("297mm", "210mm"));
            PaperSizes.Add(PaperKind.A5Rotated, new StrPaperSize("210mm", "148mm"));
            PaperSizes.Add(PaperKind.B4JisRotated, new StrPaperSize("364mm", "257mm"));
            PaperSizes.Add(PaperKind.B5JisRotated, new StrPaperSize("257mm", "182mm"));
            PaperSizes.Add(PaperKind.JapanesePostcardRotated, new StrPaperSize("148mm", "100mm"));
            PaperSizes.Add(PaperKind.JapaneseDoublePostcardRotated, new StrPaperSize("148mm", "200mm"));
            PaperSizes.Add(PaperKind.A6Rotated, new StrPaperSize("148mm", "105mm"));
            PaperSizes.Add(PaperKind.B6Jis, new StrPaperSize("128mm", "182mm"));
            PaperSizes.Add(PaperKind.B6JisRotated, new StrPaperSize("182mm", "128mm"));
            PaperSizes.Add(PaperKind.Standard12x11, new StrPaperSize("12in", "11in"));
            PaperSizes.Add(PaperKind.Prc16K, new StrPaperSize("146mm", "215mm"));
            PaperSizes.Add(PaperKind.Prc32K, new StrPaperSize("97mm", "151mm"));
            PaperSizes.Add(PaperKind.Prc32KBig, new StrPaperSize("97mm", "151mm"));
            PaperSizes.Add(PaperKind.PrcEnvelopeNumber1, new StrPaperSize("102mm", "165mm"));
            PaperSizes.Add(PaperKind.PrcEnvelopeNumber2, new StrPaperSize("102mm", "176mm"));
            PaperSizes.Add(PaperKind.PrcEnvelopeNumber3, new StrPaperSize("125mm", "176mm"));
            PaperSizes.Add(PaperKind.PrcEnvelopeNumber4, new StrPaperSize("110mm", "208mm"));
            PaperSizes.Add(PaperKind.PrcEnvelopeNumber5, new StrPaperSize("110mm", "220mm"));
            PaperSizes.Add(PaperKind.PrcEnvelopeNumber6, new StrPaperSize("120mm", "230mm"));
            PaperSizes.Add(PaperKind.PrcEnvelopeNumber7, new StrPaperSize("160mm", "230mm"));
            PaperSizes.Add(PaperKind.PrcEnvelopeNumber8, new StrPaperSize("120mm", "309mm"));
            PaperSizes.Add(PaperKind.PrcEnvelopeNumber9, new StrPaperSize("229mm", "324mm"));
            PaperSizes.Add(PaperKind.PrcEnvelopeNumber10, new StrPaperSize("324mm", "458mm"));
            PaperSizes.Add(PaperKind.Prc16KRotated, new StrPaperSize("146mm", "215mm"));
            PaperSizes.Add(PaperKind.Prc32KRotated, new StrPaperSize("97mm", "151mm"));
            PaperSizes.Add(PaperKind.Prc32KBigRotated, new StrPaperSize("97mm", "151mm"));
            PaperSizes.Add(PaperKind.PrcEnvelopeNumber1Rotated, new StrPaperSize("165mm", "102mm"));
            PaperSizes.Add(PaperKind.PrcEnvelopeNumber2Rotated, new StrPaperSize("176mm", "102mm"));
            PaperSizes.Add(PaperKind.PrcEnvelopeNumber3Rotated, new StrPaperSize("176mm", "125mm"));
            PaperSizes.Add(PaperKind.PrcEnvelopeNumber4Rotated, new StrPaperSize("208mm", "110mm"));
            PaperSizes.Add(PaperKind.PrcEnvelopeNumber5Rotated, new StrPaperSize("220mm", "110mm"));
            PaperSizes.Add(PaperKind.PrcEnvelopeNumber6Rotated, new StrPaperSize("230mm", "120mm"));
            PaperSizes.Add(PaperKind.PrcEnvelopeNumber7Rotated, new StrPaperSize("230mm", "160mm"));
            PaperSizes.Add(PaperKind.PrcEnvelopeNumber8Rotated, new StrPaperSize("309mm", "120mm"));
            PaperSizes.Add(PaperKind.PrcEnvelopeNumber9Rotated, new StrPaperSize("324mm", "229mm"));
            PaperSizes.Add(PaperKind.PrcEnvelopeNumber10Rotated, new StrPaperSize("458mm", "324mm"));
        }
    }
}