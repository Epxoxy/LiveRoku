using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveRoku
{
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
                Storage.FileHelper.writeTxt (debugMsgBuilder.ToString (), path, true);
            } catch (Exception e) { }
            debugMsgBuilder.Clear ();
        }

        public override void WriteLine (string message) {
            if(!string.IsNullOrEmpty(message))
                message = message.Replace ("\n", "\n----------------");
            Write ("[" + DateTime.Now.ToString ("HH:mm:ss.fff") + "] " + message + Environment.NewLine);
        }
    }
}
