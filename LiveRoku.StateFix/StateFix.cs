namespace LiveRoku.StateFix {
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using LiveRoku.Base;
    using LiveRoku.Base.Logger;
    using LiveRoku.Base.Plugin;
    using System.Diagnostics.CodeAnalysis;
    public class StateFix : StatusAndLiveProgressBinderBase, IPlugin {
        public string Token => typeof(StateFix).FullName;
        public IPluginDescriptor Descriptor { get; } = new PluginDescriptor {
            Name = nameof(StateFix),
            Description = nameof(StateFix),
            Version = "0.0.0.1",
            Author = "epxoxy"
        };
        
        private ILiveFetcher fetcher;
        private readonly MonitorImpl monitor = new MonitorImpl();

        public void onAttach (IPluginHost host) {
            this.fetcher = host.Fetcher;
            this.fetcher.StatusBinders.add (this);
            this.fetcher.LiveProgressBinders.add(this);
        }

        public void onInitialize (ISettings settings) { }

        public void onDetach (IPluginHost host) {
            monitor?.cleanup();
        }
        
        public override void onPreparing () {
            var cancelFlv = fetcher.Extra.get("cancel-flv", false);
            monitor.cleanup();
            if (cancelFlv) {
                fetcher.LiveProgressBinders.remove(monitor);
            } else {
                monitor.init(fetcher);
                fetcher.LiveProgressBinders.add(monitor);
            }
        }

        public override void onMissionComplete(IMission mission) {
            if (!string.IsNullOrEmpty(mission.VideoObjectName)) {
                try {
                    var file = new System.IO.FileInfo(mission.VideoObjectName);
                    if (file.Directory.Exists) {
                        var msFile = new System.IO.FileInfo(System.IO.Path.Combine(file.Directory.FullName, "mission.txt"));
                        using (var fs = msFile.Exists ? msFile.OpenWrite() : msFile.Create()) {
                            using (var writer = new System.IO.StreamWriter(fs, System.Text.Encoding.UTF8)) {
                                var builder = new System.Text.StringBuilder();
                                foreach (var info in mission.RoomInfoHistory) {
                                    builder.Append("@TimeLine>>").Append(info.TimeLine)
                                        .Append("@Title>>").Append(info.Title);
                                }
                                builder.Append("@TimeSlice>>").Append(mission.BeginTime).Append(" -- ").Append(mission.EndTime);
                                writer.WriteLine("------------");
                                writer.WriteLine(mission.VideoObjectName);
                                writer.WriteLine(builder.ToString());
                            }
                        }
                    }
                    if (file.Exists && file.Length <= 1024 * 10) {
                        file.Delete();
                    }
                }catch(Exception e) {
                    fetcher.Logger.log(Level.Error, e.Message);
                }
            }
        }

        class MonitorImpl : StatusAndLiveProgressBinderBase{
            private DateTime lastDownloadTime;
            private CancellationTokenSource downloadTestCTS;
            private CancellationTokenSource streamTestCTS;
            private volatile bool streamChecking = false;
            //Idle timer
            private System.Timers.Timer idleTimer = null;
            private long idleTime = 5000;
            private object locker = new object();
            private bool cancelFlv = false;
            private ILiveFetcher fetcher;

            public void init(ILiveFetcher fetcher) {
                this.fetcher = fetcher;
                idleTimer = new System.Timers.Timer { AutoReset = false };
                idleTimer.Interval = idleTime;
                idleTimer.Elapsed -= onIdleElapsed;
                idleTimer.Elapsed += onIdleElapsed;
                lastDownloadTime = DateTime.Now;
            }

            public void cleanup() {
                this.fetcher = null;
                cancel(downloadTestCTS);
                cancel(streamTestCTS);
                if (idleTimer == null) return;
                idleTimer.Stop();
                idleTimer.Elapsed -= onIdleElapsed;
            }

            public override void onStatusUpdate(bool isOn) {
                if (isOn) downloadTest();
                else idleTimer?.Stop();
            }

            public override void onDownloadSizeUpdate(long totalSize, string sizeText) {
                lastDownloadTime = DateTime.Now;
                idleTimer.Stop();
                idleTimer.Start();
                if (!streamChecking) {
                    streamTest();
                }
            }
            
            public override void onWaiting() {
                downloadTest();
            }

            public override void onStreaming() {
                if (cancelFlv) return;
                downloadTest();
                streamTest();
            }

            public override void onStopped() {
                idleTimer.Stop();
                cancel(downloadTestCTS);
                cancel(streamTestCTS);
            }

            private void onIdleElapsed(object sender, System.Timers.ElapsedEventArgs e) {
                fetcher.Logger.log(Level.Warning, $"Idle for a while({idleTime}ms).");
                if (idleTimer.Enabled) {
                    idleTimer.Stop();
                }
            }

            [SuppressMessage("Microsoft.Performance", "CS4014")]
            private void downloadTest() {
                cancel(downloadTestCTS);
                downloadTestCTS = new CancellationTokenSource();
                Task.Run(async () => {
                    fetcher.Logger.log(Level.Info, "@DownloadTest. Start test after 10s....");
                    await Task.Delay(10000, downloadTestCTS.Token);
                    //Check time pass from last download
                    var pastMs = getDifferToNow(lastDownloadTime);
                    fetcher.Logger.log(Level.Info, $"@DownloadTest. Start test pastMs is {pastMs / (double)1000 }s");
                    if (pastMs < 1000)
                    {
                        return;
                    }
                    //Check if the live is on
                    var info = fetcher.getRoomInfo(true);
                    fetcher.Logger.log(Level.Info, $"@DownloadTest. Start test Room.IsOn is {info.IsOn}");
                    //If live off or download already started.
                    if (!info.IsOn)
                    {
                        return;
                    }
                    downloadTestCTS.Token.ThrowIfCancellationRequested();
                    fetcherStartAfter("DownloadTest", 50);
                }, downloadTestCTS.Token).ContinueWith(task => {
                    System.Diagnostics.Debug.WriteLine(task.Exception?.Message);
                }, TaskContinuationOptions.OnlyOnFaulted);
            }

            [SuppressMessage("Microsoft.Performance", "CS4014")]
            private void streamTest() {
                cancel(streamTestCTS);
                streamChecking = false;
                streamTestCTS = new CancellationTokenSource();
                Task.Run(async () => {
                    streamChecking = true;
                    while (streamChecking) {
                        await Task.Delay(30000, streamTestCTS.Token);
                        //Check time pass from last download
                        var pastMs = getDifferToNow(lastDownloadTime);
                        if (pastMs < 10000) {
                            continue;
                        }
                        //Check if live off
                        var info = fetcher.getRoomInfo(true);
                        fetcher.Logger.log(Level.Info, $"@SteamTest. Room.IsOn is {info.IsOn}");
                        if (!info.IsOn) {
                            break;
                        }
                        //Check streaming one more time
                        if ((pastMs = getDifferToNow(lastDownloadTime)) < 10000) {
                            continue;
                        }
                        //Cancel if need
                        streamTestCTS.Token.ThrowIfCancellationRequested();
                        fetcherStartAfter("StreamTest", 50);
                        break;
                    }
                    streamChecking = false;
                }, streamTestCTS.Token).ContinueWith(task => {
                    System.Diagnostics.Debug.WriteLine(task.Exception?.Message);
                    streamChecking = false;
                }, TaskContinuationOptions.OnlyOnFaulted);
            }

            private long getDifferToNow(DateTime last) {
                return (DateTime.Now.Ticks - last.Ticks) / TimeSpan.TicksPerMillisecond;
            }

            private void fetcherStartAfter(string tag, int delay) {
                Task.Run(() => {
                    if (fetcher.IsRunning) {
                        fetcher.Logger.log(Level.Info, $"@{tag}. Trying to stop downloader");
                        fetcher.Logger.log(Level.Info, $"@{tag}. Downloader will start after 50ms");
                        fetcher.stop();
                        Thread.Sleep(delay);
                    }
                    fetcher.Logger.log(Level.Info, $"@{tag}. Downloader is starting");
                    fetcher.start();
                });
            }

            private void cancel(CancellationTokenSource src) {
                if (src?.Token.CanBeCanceled == true) {
                    src.Cancel();
                }
            }

        }

    }
}