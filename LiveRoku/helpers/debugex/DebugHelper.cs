using System;
using System.Diagnostics;
using System.Text;

namespace LiveRoku {
    public static class DebugHelper {
        public static void printStackTrace (this Exception e) {
            var msgs = new System.Text.StringBuilder ("Exception Thrown: \n\t");
            //Add first level
            var m1 = new StackTrace ().GetFrame (1) ? .GetMethod ();
            msgs.appendFormatIf (m1 != null, "{0}.{1}\n\t", m1.ReflectedType.Name, m1.Name);
            //Add second level
            var m2 = new StackTrace ().GetFrame (2) ? .GetMethod ();
            msgs.appendFormatIf (m2 != null, "{0}.{1}\n\t", m2.ReflectedType.Name, m2.Name);
            msgs.appendLineIf (!string.IsNullOrEmpty (e.Message), e.Message);
            msgs.appendLineIf (!string.IsNullOrEmpty (e.StackTrace), e.StackTrace);
            if (e.InnerException != null) {
                var ex = e.InnerException;
                msgs.appendLineIf (!string.IsNullOrEmpty (ex.Message), ex.Message);
                msgs.appendLineIf (!string.IsNullOrEmpty (ex.StackTrace), ex.StackTrace);
            }
            Debug.WriteLine (msgs.ToString ());
        }
        public static void appendLineIf (this StringBuilder builder, bool condiction, string line) {
            if (condiction)
                builder.AppendLine (line);
        }
        public static void appendFormatIf (this StringBuilder builder, bool condiction, string format, params object[] objects) {
            if (condiction)
                builder.AppendFormat (format, objects);
        }
    }
}