using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace LiveRoku {

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, IAssemblyCaches {
        //Basic path below
        public static readonly string baseFolder = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string dataFolder = baseFolder + "data";
        public static readonly string pluginFolder = baseFolder + "plugins";
        public static readonly string coreFolder = baseFolder + "core";
        private static readonly string debugPath = baseFolder + "debug.txt";
        private Dictionary<string, Assembly> assemblyCache;
        private LogTraceListener logTracker;
        public static App instance;
        public App () {
            instance = this;
            assemblyCache = new Dictionary<string, Assembly> ();
            logTracker = new LogTraceListener (debugPath, 2048);
            System.Diagnostics.Trace.Listeners.Add (logTracker);
            DispatcherUnhandledException += onDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += outputException;
            AppDomain.CurrentDomain.AssemblyResolve += findCache;
            AppDomain.CurrentDomain.AssemblyLoad += cacheAssembly;
        }

        #region Assembly help

        public bool tryGet(string fullName, out Assembly assembly){
            assembly = null;
            if (assemblyCache.ContainsKey(fullName)) {
                assembly = assemblyCache[fullName];
            }
            if (assembly == null)
                return false;
            return true;
        }

        private void cacheAssembly (object sender, AssemblyLoadEventArgs args) {
            var assembly = args.LoadedAssembly;
            if (assemblyCache.ContainsKey (assembly.FullName))
                assemblyCache[assembly.FullName] = assembly;
            else assemblyCache.Add (assembly.FullName, assembly);
        }

        private Assembly findCache (object sender, ResolveEventArgs args) {
            if (assemblyCache.ContainsKey (args.Name))
                return assemblyCache[args.Name];
            return null;
            // you may not want to use First() here, consider FirstOrDefault() as well
            /*return  (from a in AppDomain.CurrentDomain.GetAssemblies ()
                   where a.GetName ().FullName == args.Name
                   select a).FirstOrDefault ();*/
        }

        #endregion

        #region Unexpected exception handler

        private void outputException (object sender, UnhandledExceptionEventArgs e) {
            var exception = (Exception) e.ExceptionObject;
#if DEBUG
            System.Diagnostics.Debug.WriteLine (sender.ToString () + "\n" + e.ExceptionObject);
#endif
            LogExceptionInfo (exception, "AppDomain.CurrentDomain.UnhandledException");
        }

        private void onDispatcherUnhandledException (object sender, DispatcherUnhandledExceptionEventArgs e) {
#if DEBUG
            System.Diagnostics.Debug.WriteLine (e.Exception.TargetSite);
#endif
            LogExceptionInfo (e.Exception, "AppDomain.DispatcherUnhandledException");
        }

        private void LogExceptionInfo (Exception exception, string typeName = "Undefined Exception") {
            purgeEvents ();
            var lb = new System.Text.StringBuilder ();
            lb.AppendLine ("***************************");
            lb.AppendLine ("--------- Begin  ---------");
            lb.AppendLine ("--------------------------");
            lb.AppendLine ();
            lb.AppendLine (DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss ffff"));
            lb.AppendLine ();
            lb.AppendLine ("--------------------------");
            lb.AppendLine ();
            lb.AppendLine (typeName);
            lb.AppendLine ();
            lb.AppendLine ("[0].TargetSite");
            lb.AppendLine (exception.TargetSite.ToString ());
            lb.AppendLine ();
            lb.AppendLine ("[1].StackTrace");
            lb.AppendLine (exception.StackTrace);
            lb.AppendLine ();
            lb.AppendLine ("[2].Source");
            lb.AppendLine (exception.Source);
            lb.AppendLine ();
            lb.AppendLine ("[3].Message");
            lb.AppendLine (exception.Message);
            lb.AppendLine ();
            lb.AppendLine ("[4].HResult");
            lb.AppendLine (exception.HResult.ToString ());
            lb.AppendLine ();
            if (exception.InnerException != null) {
                lb.AppendLine ("--------------");
                lb.AppendLine ("InnerException");
                lb.AppendLine ("--------------");
                lb.AppendLine ();
                lb.AppendLine ("[5.0].TargetSite");
                lb.AppendLine (exception.InnerException.TargetSite.ToString ());
                lb.AppendLine ();
                lb.AppendLine ("[5.1].StackTrace");
                lb.AppendLine (exception.InnerException.StackTrace);
                lb.AppendLine ();
                lb.AppendLine ("[5.2].Source");
                lb.AppendLine (exception.InnerException.Source);
                lb.AppendLine ();
                lb.AppendLine ("[5.3].Message");
                lb.AppendLine (exception.InnerException.Message);
                lb.AppendLine ();
                lb.AppendLine ("[5.4].HResult");
                lb.AppendLine (exception.InnerException.HResult.ToString ());
                lb.AppendLine ();
            }
            lb.AppendLine ("--------- End  ---------");
            lb.AppendLine ();

            string location = System.Reflection.Assembly.GetExecutingAssembly ().Location;
            string dir = System.IO.Path.GetDirectoryName (location);
            string log = dir + "\\log.txt";
            using (var sw = new System.IO.StreamWriter (log, true, System.Text.Encoding.UTF8)) {
                sw.Write (lb.ToString ());
            }
            logTracker.WriteToFileAndClear ();
        }

        //Is it need? I don't know
        private void purgeEvents () {
            DispatcherUnhandledException -= onDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException -= outputException;
            AppDomain.CurrentDomain.AssemblyResolve -= findCache;
        }

        #endregion
    }

}