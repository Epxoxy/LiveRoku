using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveRoku {
    internal class LogTraceListener : System.Diagnostics.TraceListener {
        private StringBuilder debugMsgBuilder = new StringBuilder ();
        private string path;
        private int maxSize;

        public LogTraceListener (string path, int maxSize) {
            this.path = path;
            this.maxSize = maxSize;
        }

        public override void Write (string message) {
            debugMsgBuilder.Append (message);
            if (debugMsgBuilder.Length > maxSize) {
                WriteToFileAndClear ();
            }
        }

        public void WriteToFileAndClear () {
            if (debugMsgBuilder.Length <= 0) return;
            try {
                Loader.FileHelper.writeText (debugMsgBuilder.ToString (), path, true);
            } catch (Exception) { throw; }
            debugMsgBuilder.Clear ();
        }

        public override void WriteLine (string message) {
            if (!string.IsNullOrEmpty (message)) {
                message = message.Replace ("\n", $"{Environment.NewLine}----------------");
            }
            Write ("[" + DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss.fff") + "] " + message + Environment.NewLine);
        }
    }
}