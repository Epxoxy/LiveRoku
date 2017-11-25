namespace LiveRoku.StateFix {
    using LiveRoku.Base;
    using LiveRoku.Base.Logger;
    using LiveRoku.Base.Plugin;
    using System;
    using System.IO;
    using System.Text;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;

    public class StateFix : LiveResolverBase, IPlugin {
        public string Token => typeof(StateFix).FullName;
        public IPluginDescriptor Descriptor { get; } = new PluginDescriptor {
            Name = nameof(StateFix),
            Description = nameof(StateFix),
            Version = "0.0.0.1",
            Author = "epxoxy"
        };
        
        [PluginSetting]
        public EasySettings Extra { get; private set; }

        private ILogger logger;
        private readonly MonitorImpl monitor = new MonitorImpl();
        private int idleDuration;

        public void onInitialize(ISettings appSettings) {
            Extra = Extra ?? new EasySettings();//Get plugin settings
            idleDuration = Math.Max(Extra.get("idle-duration", (int)5000), 5000);
            Extra.put("idle-duration", idleDuration);
        }

        public void onAttach (IContext ctx) {
            ctx.Fetcher.StatusBinders.add (this);
            ctx.Fetcher.DownloadProgressBinders.add(this);
            logger = ctx.Fetcher.Logger;
        }

        public void onDetach (IContext ctx) {
            //Remove this
            ctx.Fetcher.StatusBinders.remove(this);
            ctx.Fetcher.DownloadProgressBinders.remove(this);
            this.logger = null;
            //Remove monitor
            ctx.Fetcher.DownloadProgressBinders.remove(monitor);
            ctx.Fetcher.DanmakuHandlers.remove(monitor);
            ctx.Fetcher.StatusBinders.remove(monitor);
            monitor.cleanup();
        }
        
        public override void onPreparing (IContext ctx) {
            var videoRequire = ctx.Fetcher.RuntimeExtra.get("video-require", true);
            ctx.Fetcher.DownloadProgressBinders.remove(monitor);
            ctx.Fetcher.DanmakuHandlers.remove(monitor);
            ctx.Fetcher.StatusBinders.remove(monitor);
            if (videoRequire) {
                monitor.init(ctx.Fetcher, idleDuration);
                ctx.Fetcher.DownloadProgressBinders.add(monitor);
                ctx.Fetcher.DanmakuHandlers.add(monitor);
                ctx.Fetcher.StatusBinders.add(monitor);
            } else {
                monitor.cleanup();
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
                    logger?.log(Level.Error, e.Message);
                }
            }
        }
        
        class MonitorImpl : LiveResolverBase{
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

            public override void onLiveStatusUpdate(bool isOn) {
                if (isOn) downloadTest();
            }
            
            public override void onDownloadSizeUpdate(long totalSize, string sizeText) {
                newestRecvTime = DateTime.Now;
                if (!idleTimer.Enabled)
                    idleTimer.Start();
            }
            
            public override void onWaiting(IContext ctx) {
                idleTimer.Stop();
                downloadTest();
            }

            public override void onStreaming(IContext ctx) {
                newestRecvTime = DateTime.Now;
                idleTimer.Stop();
                downloadTest();
                idleTimer.Start();
            }

            public override void onStopped(IContext ctx) {
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
                downloadTestCTS.Token.Register(() => {
                    Debug.WriteLine($"@{nameof(downloadTest)}. Test cancel", "state");
                });
                Task.Run(async () => {
                    //Delay test
                    Debug.WriteLine($"@{nameof(downloadTest)}. Test after 10s....", "state");
                    await Task.Delay(10000, downloadTestCTS.Token);
                    if (downloadTestCTS.Token.IsCancellationRequested)
                        return;
                    //Verify if need to restart
                    await verifyAndRestart(nameof(downloadTest), 1000, downloadTestCTS.Token);
                    Debug.WriteLine($"@{nameof(downloadTest)}. Test complete.", "state");
                }, downloadTestCTS.Token).ContinueWith(task => {
                    if (task?.Exception != null)
                        Debug.WriteLine(task.Exception.ToString());
                }, TaskContinuationOptions.OnlyOnFaulted);
            }

            //return if it will restart
            private Task<bool?> verifyAndRestart(string sender, double maxIdleAllowTime, CancellationToken token) {
                double pastMs = 0;
                //Check streaming one more time
                if ((pastMs = pastMsToNowFrom(newestRecvTime)) < maxIdleAllowTime)
                    return Task.FromResult<bool?>(false);
                return Task.Run(() => {
                    Debug.WriteLine($"@{sender}. Test pastMs is {pastMs / (double)1000 }s", "verify");
                    //Check if live off
                    var info = fetcher.getRoomInfo(true);
                    Debug.WriteLine($"@{sender}. Room.IsOn is [{info?.IsOn}]", "verify");
                    //Check streaming one more time
                    if (token.IsCancellationRequested
                    || (pastMs = pastMsToNowFrom(newestRecvTime)) < maxIdleAllowTime
                    || info?.IsOn == false) {
                        return false;
                    }
                    //Cancel if need
                    if (token.IsCancellationRequested)
                        return false;
                    restartFetcherAfter(sender, 50);
                    return true;
                }, token).ContinueWith(task => {
                    if(task.Exception != null)
                        Debug.WriteLine(task.Exception.ToString());
                    return task?.Result;
                });
            }
            
            private Task restartFetcherAfter(string sender, int delay) {
                return Task.Run(() => {
                    if (fetcher.IsRunning) {
                        fetcher.Logger.log(Level.Info, $"@{sender}. Need to restart downloader");
                        fetcher.Logger.log(Level.Info, $"@{sender}. Downloader will start after {delay}ms");
                        fetcher.stop();
                        Thread.Sleep(delay);
                    }
                    Debug.WriteLine($"@{sender}. Downloader is starting", "restart");
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