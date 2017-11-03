namespace LiveRoku.StateFix {
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using LiveRoku.Base;
    using LiveRoku.Base.Logger;
    using LiveRoku.Base.Plugin;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Text;
    using System.Linq;

    public class StateFix : StatusAndLiveProgressBinderBase, IPlugin {
        public string Token => typeof(StateFix).FullName;
        public IPluginDescriptor Descriptor { get; } = new PluginDescriptor {
            Name = nameof(StateFix),
            Description = nameof(StateFix),
            Version = "0.0.0.1",
            Author = "epxoxy"
        };
        
        [PluginSetting]
        public EasySettings Extra { get; private set; }

        private ILiveFetcher fetcher;
        private readonly MonitorImpl monitor = new MonitorImpl();
        private int idleDuration;

        public void onAttach (IPluginHost host) {
            this.fetcher = host.Fetcher;
            this.fetcher.StatusBinders.add (this);
            this.fetcher.LiveProgressBinders.add(this);
        }

        public void onInitialize (ISettings settings) {
            Extra = Extra ?? new EasySettings();
            idleDuration = Math.Max(Extra.get("idle-duration", (int)5000), 5000);
            Extra.put("idle-duration", idleDuration);
            monitor.init(fetcher, idleDuration);
        }

        public void onDetach (IPluginHost host) {
            monitor?.cleanup();
        }
        
        public override void onPreparing () {
            var videoRequire = fetcher.Extra.get("video-require", true);
            if (!videoRequire) {
                monitor.cleanup();
                fetcher.LiveProgressBinders.remove(monitor);
                fetcher.StatusBinders.remove(monitor);
            } else {
                monitor.init(fetcher, idleDuration);
                fetcher.LiveProgressBinders.add(monitor);
                fetcher.StatusBinders.add(monitor);
            }
        }

        public override void onMissionComplete(IMission mission) {
            if (!string.IsNullOrEmpty(mission.VideoObjectName)) {
                try {
                    var file = new FileInfo(mission.VideoObjectName);
                    if (file.Directory.Exists) {
                        using (var writer = new StreamWriter(Path.Combine(file.Directory.FullName, "mission.txt"), true , Encoding.UTF8)) {
                            var builder = new StringBuilder().AppendLine("------------").AppendLine(file.Name);
                            foreach (var info in mission.RoomInfoHistory) {
                                builder.Append("{(TimeLine) >> ").Append(info.TimeLine).Append("} ")
                                    .Append("{(Title) >> ").Append(info.Title).Append("} ").AppendLine();
                            }
                            builder.Append("{(TimeSlice) >> ").Append(mission.BeginTime).Append(" -- ").Append(mission.EndTime).Append("}");
                            writer.WriteLine(builder.ToString());
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
            private DateTime newestRecvTime;
            private CancellationTokenSource downloadTestCTS;
            private CancellationTokenSource idleTestCTS;
            private System.Timers.Timer idleTimer = null;
            private int idleCheckTime;

            private object locker = new object();
            private ILiveFetcher fetcher;
            
            public void init(ILiveFetcher fetcher, int idleCheckTime) {
                this.fetcher = fetcher;
                this.idleCheckTime = idleCheckTime;
                if (idleTimer != null) {
                    idleTimer.Elapsed -= onIdleElapsed;
                    idleTimer.Dispose();
                }
                idleTimer = new System.Timers.Timer { AutoReset = true };
                idleTimer.Interval = idleCheckTime;
                idleTimer.Elapsed += onIdleElapsed;
                newestRecvTime = DateTime.Now;
            }

            public void cleanup() {
                this.fetcher = null;
                cancel(downloadTestCTS);
                cancel(idleTestCTS);
                if (idleTimer == null) return;
                idleTimer.Elapsed -= onIdleElapsed;
                idleTimer.Stop();
            }

            public override void onStatusUpdate(bool isOn) {
                if (isOn) downloadTest();
                else idleTimer.Stop();
            }

            public override void onDownloadSizeUpdate(long totalSize, string sizeText) {
                newestRecvTime = DateTime.Now;
                if (!idleTimer.Enabled)
                    idleTimer.Start();
            }
            
            public override void onWaiting() {
                idleTimer.Stop();
                downloadTest();
            }

            public override void onStreaming() {
                newestRecvTime = DateTime.Now;
                idleTimer.Stop();
                downloadTest();
                idleTimer.Start();
            }

            public override void onStopped() {
                idleTimer.Stop();
                cancel(downloadTestCTS);
                cancel(idleTestCTS);
            }

            //Only tips idle
            private void onIdleElapsed(object sender, System.Timers.ElapsedEventArgs e) {
                var pastMs = pastMsToNowFrom(newestRecvTime);
                if (pastMs > idleCheckTime) {
                    fetcher.Logger.log(Level.Warning, $"Idle for a while({idleCheckTime}ms).");
                    //Stop timer
                    idleTimer.Enabled = false;
                    cancel(idleTestCTS);
                    idleTestCTS = new CancellationTokenSource();
                    verifyAndRestart("IdleTest", 2000, idleTestCTS.Token).Wait();
                    //Restart timer
                    idleTimer.Enabled = true;
                }
            }

            [SuppressMessage("Microsoft.Performance", "CS4014")]
            private void downloadTest() {
                cancel(downloadTestCTS);
                downloadTestCTS = new CancellationTokenSource();
                Task.Run(async () => {
                    //Delay test
                    fetcher.Logger.log(Level.Info, $"@{nameof(downloadTest)}. Test after 10s....");
                    await Task.Delay(10000, downloadTestCTS.Token);
                    downloadTestCTS.Token.ThrowIfCancellationRequested();
                    //Verify if need to restart
                    await verifyAndRestart(nameof(downloadTest), 1000, downloadTestCTS.Token);
                    fetcher.Logger.log(Level.Info, $"@{nameof(downloadTest)}. Test complete.");
                }, downloadTestCTS.Token).ContinueWith(task => {
                    if (task?.Exception != null)
                        System.Diagnostics.Debug.WriteLine(task.Exception.ToString());
                }, TaskContinuationOptions.OnlyOnFaulted);
            }

            //return if it will restart
            private Task<bool?> verifyAndRestart(string sender, double maxIdleAllowTime, CancellationToken token) {
                double pastMs = 0;
                //Check streaming one more time
                if ((pastMs = pastMsToNowFrom(newestRecvTime)) < maxIdleAllowTime)
                    return Task.FromResult<bool?>(false);
                return Task.Run(() => {
                    fetcher.Logger.log(Level.Info, $"@{sender}. Test pastMs is {pastMs / (double)1000 }s");
                    //Check if live off
                    var info = fetcher.getRoomInfo(true);
                    fetcher.Logger.log(Level.Info, $"@{sender}. Room.IsOn is [{info?.IsOn}]");
                    token.ThrowIfCancellationRequested();
                    //Check streaming one more time
                    if ((pastMs = pastMsToNowFrom(newestRecvTime)) < maxIdleAllowTime)
                        return false;
                    if (info?.IsOn == false)
                        return false;
                    //Cancel if need
                    token.ThrowIfCancellationRequested();
                    restartFetcherAfter(sender, 50);
                    return true;
                }, token).ContinueWith(task => {
                    if(task.Exception != null)
                        System.Diagnostics.Debug.WriteLine(task.Exception.ToString());
                    return task?.Result;
                });
            }
            
            private Task restartFetcherAfter(string sender, int delay) {
                return Task.Run(() => {
                    if (fetcher.IsRunning) {
                        fetcher.Logger.log(Level.Info, $"@{sender}. Trying to stop downloader");
                        fetcher.Logger.log(Level.Info, $"@{sender}. Downloader will start after 50ms");
                        fetcher.stop();
                        Thread.Sleep(delay);
                    }
                    fetcher.Logger.log(Level.Info, $"@{sender}. Downloader is starting");
                    fetcher.start();
                });
            }

            private long pastMsToNowFrom(DateTime baseTime) {
                return (DateTime.Now.Ticks - baseTime.Ticks) / TimeSpan.TicksPerMillisecond;
            }

            private void cancel(CancellationTokenSource src) {
                if (src?.Token.CanBeCanceled == true) {
                    src.Cancel();
                }
            }

        }

    }
}