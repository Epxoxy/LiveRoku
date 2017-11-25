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
    public partial class App : Application {
        //Basic path below
        private static readonly string debugFolder = AppDomain.CurrentDomain.BaseDirectory + "debug";
        private LogDateTimeTraceListener logTracker;
        
        public App () {
            logTracker = new LogDateTimeTraceListener (System.IO.Path.Combine(debugFolder, "debug.txt"));
            System.Diagnostics.Trace.Listeners.Add (logTracker);
            System.Diagnostics.Trace.AutoFlush = true;
            DispatcherUnhandledException += onDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += outputException;
            System.Diagnostics.Debug.WriteLine("--------------------------", "block");
            System.Diagnostics.Debug.WriteLine("--------------------------", "block");
            System.Diagnostics.Debug.WriteLine("---------  begin ---------", "block");
            System.Diagnostics.Debug.WriteLine("--------------------------", "block");
        }

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
            var builder = new StringBuilder ()
                .AppendLine ("***************************")
                .AppendLine ("--------- Begin  ---------")
                .AppendLine ("--------------------------").AppendLine ()
                .AppendLine (DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss ffff")).AppendLine ()
                .AppendLine ("--------------------------").AppendLine ()
                .AppendLine (typeName).AppendLine ()
                .AppendLine ("[0].TargetSite")
                .AppendLine (exception.TargetSite.ToString ()).AppendLine ()
                .AppendLine ("[1].StackTrace")
                .AppendLine (exception.StackTrace).AppendLine ()
                .AppendLine ("[2].Source")
                .AppendLine (exception.Source).AppendLine ()
                .AppendLine ("[3].Message")
                .AppendLine (exception.Message).AppendLine ()
                .AppendLine ("[4].HResult")
                .AppendLine (exception.HResult.ToString ()).AppendLine ();
            if (exception.InnerException != null) {
                builder.AppendLine ("--------------")
                    .AppendLine ("InnerException")
                    .AppendLine ("--------------").AppendLine ()
                    .AppendLine ("[5.0].TargetSite")
                    .AppendLine (exception.InnerException.TargetSite.ToString ()).AppendLine ()
                    .AppendLine ("[5.1].StackTrace")
                    .AppendLine (exception.InnerException.StackTrace).AppendLine ()
                    .AppendLine ("[5.2].Source")
                    .AppendLine (exception.InnerException.Source).AppendLine ()
                    .AppendLine ("[5.3].Message")
                    .AppendLine (exception.InnerException.Message).AppendLine ()
                    .AppendLine ("[5.4].HResult")
                    .AppendLine (exception.InnerException.HResult.ToString ()).AppendLine ();
            }
            builder.AppendLine ("--------- End  ---------").AppendLine ();

            string logPath = System.IO.Path.Combine(debugFolder, "log.txt");
            using (var sw = new System.IO.StreamWriter (logPath, true, System.Text.Encoding.UTF8)) {
                sw.Write (builder.ToString ());
            }
        }

        //Is it need? I don't know
        private void purgeEvents () {
            DispatcherUnhandledException -= onDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException -= outputException;
        }

        #endregion

        internal class LogDateTimeTraceListener : System.Diagnostics.TextWriterTraceListener {
            public LogDateTimeTraceListener(string path) : base(path) { }

            public override void WriteLine(string message, string category) {
                base.WriteLine(string.Format("[{0:yyyy-MM-dd HH:mm:ss.fff}] [{1}]: {2}", DateTime.Now, category, message.Replace("\n", Environment.NewLine)));
            }

            public override void WriteLine(string message) {
                base.WriteLine(string.Format("[{0:yyyy-MM-dd HH:mm:ss.fff}] : {1}", DateTime.Now, message.Replace("\n", Environment.NewLine)));
            }
        }
    }
}