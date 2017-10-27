using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace LiveRoku.Notifications.helpers {
    internal class TimerWrapper : IDisposable {
        private Timer timer;
        public bool IsRunning { get; private set; }
        private Action onTick;

        public TimerWrapper (Action onTick, long interval) {
            this.onTick = onTick;
            timer = new Timer ();
            timer.Interval = interval;
            timer.AutoReset = false;
            timer.Elapsed += onElapsed;
        }

        public static TimerWrapper wrap (Action onTick, long interval) {
            return new TimerWrapper (onTick, interval);
        }

        public void stop () {
            if (IsRunning) {
                timer.Stop ();
                IsRunning = false;
            }
        }

        public void restart () {
            if (IsRunning) {
                timer.Stop ();
            }
            IsRunning = true;
            timer.Start ();
        }

        private void onElapsed (object sender, ElapsedEventArgs e) {
            IsRunning = false;
            onTick?.Invoke ();
        }

        public void Dispose () {
            onTick = null;
            timer?.Dispose ();
        }
    }

}