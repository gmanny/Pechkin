using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Remoting;
using Pechkin.Util;

namespace Pechkin
{
    /// <summary>
    /// Static class used to obtain instances of a PDF converter implementing the 
    /// IPechkin interface.
    /// </summary>
    public static class Factory
    {
        /// <summary>
        /// This is the function used by Factory to invoke any calls to
        /// the wkhtmltopdf library, whether synchronized or not.
        /// </summary>
        private static readonly Func<Func<object>, object> invocationDelegate = (Func<object> del) =>
        {
            if (Factory.useSync && Factory.synchronizer != null)
            {
                return Factory.synchronizer.Invoke(del, null);
            }
            else
            {
                return del.DynamicInvoke();
            }
        };

        /// <summary>
        /// The collection of instantiated, undisposed proxies
        /// </summary>
        private static readonly List<IPechkin> proxies = new List<IPechkin>();

        /// <summary>
        /// The AppDomain used to encapsulate calls to the wkhtmltopdf library
        /// </summary>
        private static AppDomain operatingDomain = null;

        /// <summary>
        /// In case we are running in .NET debug, the actual location of the 
        /// Pechkin assembly (not in the .NET temp folders)
        /// </summary>
        private static String realAssemblyLocation = null;

        /// <summary>
        /// A thread used to invoke all calls to the wkhtmltopdf library
        /// so that multi-threaded applications can use Pechkin
        /// </summary>
        private static SynchronizedDispatcherThread synchronizer = null;

        /// <summary>
        /// See public property
        /// </summary>
        private static bool useDynamicLoading = false;

        /// <summary>
        /// See public property
        /// </summary>
        private static bool useSync = true;

        /// <summary>
        /// See public property
        /// </summary>
        private static bool useX11Graphics = false;

        /// <summary>
        /// Used to find out which kind of wkhtmltopdf dll is loaded.
        /// </summary>
        public static Boolean ExtendedQtAvailable
        {
            get
            {
                bool tearDown = false;

                if (Factory.operatingDomain == null)
                {
                    Factory.SetupAppDomain();

                    if (Factory.useDynamicLoading)
                    {
                        tearDown = true;
                    }
                }

                Func<object> del = () =>
                {
                    Factory.operatingDomain.DoCallBack(() =>
                    {
                        AppDomain.CurrentDomain.SetData("data", PechkinBindings.wkhtmltopdf_extended_qt());
                    });

                    return (int)Factory.operatingDomain.GetData("data");
                };

                var ret = (int)Factory.invocationDelegate.DynamicInvoke(del);

                if (tearDown)
                {
                    Factory.TearDownAppDomain(null, EventArgs.Empty);
                }

                return ret != 0;
            }
        }

        /// <summary>
        /// When set to true, Pechkin.Factory will release the wkhtmltopdf library
        /// anytime that it detects that all of its converters are disposed. 
        /// Default value is false.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Thrown when the wkhtmltopdf
        /// factory has already loaded the wkhtmltopdf library and has not yet released it.</exception>
        public static Boolean UseDynamicLoading
        {
            get
            {
                return Factory.useDynamicLoading;
            }
            set
            {
                if (Factory.operatingDomain != null &&
                    Factory.useDynamicLoading != value)
                {
                    throw new InvalidOperationException("App domain already loaded; cannot change dynamic loading setting");
                }

                Factory.useDynamicLoading = value;
            }
        }

        /// <summary>
        /// When set to true, Pechkin.Factory will delegate all wkhtmltopdf functions
        /// to a dedicated thread to ensure that multiple threads can use Pechkin safely.
        /// Default value is true.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Thrown when the wkhtmltopdf
        /// factory has already loaded the wkhtmltopdf library and has not yet released it.</exception>
        public static Boolean UseSynchronization
        {
            get
            {
                return Factory.useSync;
            }
            set
            {
                if (value != Factory.useSync &&
                    null != Factory.operatingDomain)
                {
                    throw new InvalidOperationException("App domain already loaded; cannot change synchronization setting");
                }

                Factory.useSync = value;
            }
        }

        /// <summary>
        /// When set to true, Pechkin.Factory will set up wkhtmltopdf to use X11 graphics mode.
        /// Default value is false.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Thrown when the wkhtmltopdf
        /// factory has already loaded the wkhtmltopdf library and has not yet released it.</exception>
        public static Boolean UseX11Graphics
        {
            get
            {
                return Factory.useX11Graphics;
            }
            set
            {
                if (value != Factory.useX11Graphics &&
                    null != Factory.operatingDomain)
                {
                    throw new InvalidOperationException("App domain already loaded; cannot change graphic setting");
                }

                Factory.useX11Graphics = value;
            }
        }

        /// <summary>
        /// Used to find out which version of wkhtmltopdf dll is loaded.
        /// </summary>
        public static String Version
        {
            get
            {
                bool tearDown = false;

                if (Factory.operatingDomain == null)
                {
                    Factory.SetupAppDomain();

                    if (Factory.useDynamicLoading == true)
                    {
                        tearDown = true;
                    }
                }

                Func<object> del = () =>
                {
                    Factory.operatingDomain.DoCallBack(() =>
                    {
                        AppDomain.CurrentDomain.SetData("data", PechkinBindings.wkhtmltopdf_version());
                    });

                    return Factory.operatingDomain.GetData("data").ToString();
                };

                String ret = Factory.invocationDelegate.DynamicInvoke(del).ToString();

                if (tearDown)
                {
                    Factory.TearDownAppDomain(null, EventArgs.Empty);
                }

                return ret;
            }
        }

        /// <summary>
        /// Returns an instance of a PDF converter that implements the IPechkin interface.
        /// </summary>
        /// <param name="config">A GlobalConfig object for the converter to apply.</param>
        /// <returns>IPechkin</returns>
        public static IPechkin Create(GlobalConfig config)
        {
            if (Factory.operatingDomain == null)
            {
                Factory.SetupAppDomain();
            }

            String location = Factory.realAssemblyLocation ?? Assembly.GetExecutingAssembly().Location;

            ObjectHandle handle = Activator.CreateInstanceFrom(
                Factory.operatingDomain,
                location,
                typeof(SimplePechkin).FullName,
                false,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new object[] { config },
                null,
                null,
                null);

            IPechkin instance = handle.Unwrap() as IPechkin;

            Proxy proxy = new Proxy(instance, Factory.invocationDelegate);

            Factory.proxies.Add(proxy);

            proxy.Disposed += Factory.OnInstanceDisposed;

            return proxy;
        }

        /// <summary>
        /// Event handler for proxies that get disposed.
        /// </summary>
        /// <param name="disposed"></param>
        private static void OnInstanceDisposed(IPechkin disposed)
        {
            Factory.proxies.Remove(disposed);

            if (Factory.proxies.Count == 0 && Factory.useDynamicLoading)
            {
                Factory.TearDownAppDomain(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Creates and initializes a private AppDomain and therein loads and initializes the
        /// wkhtmltopdf library. Attaches to the current AppDomain's DomainUnload event in IIS environments 
        /// to ensure that on re-deploy, the library is freed so the new AppDomain will be able to use it.
        /// </summary>
        private static void SetupAppDomain()
        {
            if (Factory.useSync)
            {
                Factory.synchronizer = new SynchronizedDispatcherThread();
            }

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

            Factory.operatingDomain = AppDomain.CreateDomain("pechkin_internal_domain", null,
                new AppDomainSetup
                {
                    ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                    // Sometimes, like in a web app, your bin folder is not the same
                    // as the base dir.
                    PrivateBinPath = binPath
                });

            if (binPath != String.Empty)
            {
                Factory.operatingDomain.SetData("assemblyLocation", Assembly.GetExecutingAssembly().Location);

                Factory.operatingDomain.DoCallBack(() =>
                {
                    String location = AppDomain.CurrentDomain.GetData("assemblyLocation").ToString();
                    String filename = System.IO.Path.GetFileName(location);
                    List<String> paths = new List<String>(AppDomain.CurrentDomain.RelativeSearchPath.Split(';'));

                    foreach (String path in paths.ToArray())
                    {
                        paths.Remove(path);
                        paths.AddRange(System.IO.Directory.GetFiles(path, filename));
                    }

                    Assembly.LoadFrom(paths[0]);

                    AppDomain.CurrentDomain.SetData("assemblyLocation", paths[0]);
                });

                Factory.realAssemblyLocation = Factory.operatingDomain.GetData("assemblyLocation").ToString();
            }
            else
            {
                Factory.operatingDomain.Load(Assembly.GetExecutingAssembly().FullName);
            }

            Func<object> del = () =>
            {
                Factory.operatingDomain.SetData("useX11Graphics", Factory.useX11Graphics);

                Factory.operatingDomain.DoCallBack(() =>
                {
                    PechkinBindings.wkhtmltopdf_init((bool)AppDomain.CurrentDomain.GetData("useX11Graphics") ? 1 : 0);
                });

                return null;
            };

            Factory.invocationDelegate.DynamicInvoke(del);

            if (AppDomain.CurrentDomain.IsDefaultAppDomain() == false)
            {
                AppDomain.CurrentDomain.DomainUnload += Factory.TearDownAppDomain;
            }
        }

        /// <summary>
        /// Unloads the private AppDomain and the wkhtmltopdf library, and if applicable, destroys
        /// the synchronization thread.
        /// </summary>
        /// <param name="sender">Typically a null value, not used in the method.</param>
        /// <param name="e">Typically EventArgs.Empty, not used in the method.</param>
        private static void TearDownAppDomain(object sender, EventArgs e)
        {
            if (Factory.operatingDomain != null)
            {
                Func<object> del = () =>
                {
                    Factory.operatingDomain.DoCallBack(() => PechkinBindings.wkhtmltopdf_deinit());

                    return null;
                };

                Factory.invocationDelegate.DynamicInvoke(del);

                AppDomain.Unload(Factory.operatingDomain);

                foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
                {
                    if (module.ModuleName == PechkinBindings.LibFilename)
                    {
                        while (PechkinBindings.FreeLibrary(module.BaseAddress))
                        {
                        }
                    }
                }

                Factory.operatingDomain = null;
            }
            
            if (Factory.synchronizer != null)
            {
                Factory.synchronizer.Terminate();
                Factory.synchronizer = null;
            }
        }
    }
}