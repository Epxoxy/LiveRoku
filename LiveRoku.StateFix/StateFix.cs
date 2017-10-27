using System;
using System.Threading;
using System.Threading.Tasks;
using LiveRoku.Base;
using LiveRoku.Base.Logger;
using LiveRoku.Base.Plugin;
using System.Diagnostics.CodeAnalysis;

namespace LiveRoku.StateFix {
    public class StateFix : LiveProgressBinderBase, IPlugin, IStatusBinder {
        public string Token => typeof(StateFix).FullName;
        public IPluginDescriptor Descriptor { get; } = new PluginDescriptor {
            Name = nameof(StateFix),
            Description = nameof(StateFix),
            Version = "0.0.0.1",
            Author = "epxoxy"
        };
        
        private ILiveFetcher fetcher;
        private DateTime lastDownloadTime;
        private CancellationTokenSource downloadTestCTS;
        private CancellationTokenSource streamTestCTS;
        private volatile bool streamChecking = false;
        //Idle timer
        private System.Timers.Timer idleTimer = null;
        private long idleTime = 5000;
        private object locker = new object ();
        private Action runOnce;
        private ISettings settings;

        public void onAttach (IPluginHost host) {
            this.fetcher = host.Fetcher;
            this.fetcher.LiveProgressBinders.add (this);
            this.fetcher.StatusBinders.add (this);
            lastDownloadTime = DateTime.Now;
            idleTimer = new System.Timers.Timer { AutoReset = false };
            idleTimer.Interval = idleTime;
            idleTimer.Elapsed += onElapsed;
        }

        private void onElapsed (object sender, System.Timers.ElapsedEventArgs e) {
            fetcher.Logger.log (Level.Warning, $"Idle for a while({idleTime}ms).");
            if (idleTimer.Enabled) {
                idleTimer.Stop ();
            }
        }

        public void onInitialize (ISettings settings) {
            this.settings = settings;
        }
        public void onDetach (IPluginHost host) {
            cancel (downloadTestCTS);
            if (idleTimer == null) return;
            idleTimer.Elapsed -= onElapsed;
        }

        [SuppressMessage("Microsoft.Performance", "CS4014")]
        private void downloadTest () {
            cancel (downloadTestCTS);
            if (!settings.get("cancel-flv", true)) return;
            downloadTestCTS = new CancellationTokenSource ();
            Task.Run (async () => {
                while (true) {
                    fetcher.Logger.log (Level.Info, "@DownloadTest. Start test after 10s....");
                    await Task.Delay (10000, downloadTestCTS.Token);
                    //Check time pass from last download
                    var pastMs = getDifferToNow (lastDownloadTime);
                    fetcher.Logger.log (Level.Info, $"@DownloadTest. Start test pastMs is {pastMs / (double)1000 }s");
                    if (pastMs < 1000) {
                        break;
                    }
                    //Check if the live is on
                    var info = fetcher.getRoomInfo (true);
                    fetcher.Logger.log (Level.Info, $"@DownloadTest. Start test Room.IsOn is {info.IsOn}");
                    //If live off or download already started.
                    if (!info.IsOn) {
                        break;
                    }
                    downloadTestCTS.Token.ThrowIfCancellationRequested ();
                    Task.Run (() => fetcherStartAfter ("DownloadTest", 50));
                }
            }, downloadTestCTS.Token).ContinueWith (task => {
                System.Diagnostics.Debug.WriteLine (task.Exception?.Message);
                if (task.IsCanceled) return;
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        [SuppressMessage("Microsoft.Performance", "CS4014")]
        private void streamTest () {
            cancel (streamTestCTS);
            if (settings.get("cancel-flv", false)) return;
            streamChecking = false;
            streamTestCTS = new CancellationTokenSource ();
            Task.Run (async () => {
                streamChecking = true;
                while (streamChecking) {
                    await Task.Delay (30000, streamTestCTS.Token);
                    //Check time pass from last download
                    var pastMs = getDifferToNow (lastDownloadTime);
                    if (pastMs < 10000) {
                        continue;
                    }
                    //Check if live off
                    var info = fetcher.getRoomInfo(true);
                    fetcher.Logger.log (Level.Info, $"@SteamTest. Room.IsOn is {info.IsOn}");
                    if (!info.IsOn) {
                        break;
                    }
                    //Check streaming one more time
                    if ((pastMs = getDifferToNow(lastDownloadTime)) < 10000) {
                        continue;
                    }
                    //Cancel if need
                    streamTestCTS.Token.ThrowIfCancellationRequested ();
                    Task.Run (() => fetcherStartAfter ("StreamTest", 50));
                }
                streamChecking = false;
            }, streamTestCTS.Token).ContinueWith (task => {
                System.Diagnostics.Debug.WriteLine (task.Exception?.Message);
                streamChecking = false;
                if (task.IsCanceled) return;
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private long getDifferToNow (DateTime last) {
            return (DateTime.Now.Ticks - last.Ticks) / TimeSpan.TicksPerMillisecond;
        }

        private async void fetcherStartAfter (string tag, int delay) {
            Action action = () => {
                fetcher.Logger.log (Level.Info, $"@{tag}. Downloader is starting");
                fetcher.start ();
            };
            if (fetcher.IsRunning) {
                runOnce = action;
                fetcher.Logger.log (Level.Info, $"@{tag}. Trying to stop downloader");
                fetcher.Logger.log (Level.Info, $"@{tag}. Downloader will start after 50ms");
                fetcher.stop ();
                await Task.Delay (delay);
            } else action.Invoke ();
        }

        private void cancel (CancellationTokenSource src) {
            if (src?.Token.CanBeCanceled == true) {
                src.Cancel ();
            }
        }

        public override void onStatusUpdate (bool isOn) {
            if (isOn && !settings.get("cancel-flv", false)) downloadTest ();
            else idleTimer?.Stop ();
        }

        public override void onDownloadSizeUpdate (long totalSize, string sizeText) {
            lastDownloadTime = DateTime.Now;
            idleTimer.Stop ();
            idleTimer.Start ();
            if (!streamChecking) {
                streamTest ();
            }
        }

        public void onPreparing () { }

        public void onWaiting () => downloadTest ();

        public void onStreaming () {
            downloadTest ();
            streamTest ();
        }

        public void onStopped () {
            idleTimer.Stop ();
            //Cancel old task
            cancel (downloadTestCTS);
            cancel (streamTestCTS);
            //Run
            var temp = runOnce;
            runOnce = null;
            temp?.Invoke ();
        }
    }
}