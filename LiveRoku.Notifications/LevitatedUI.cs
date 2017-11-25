namespace LiveRoku.Notifications {
    using System.Windows;
    using LiveRoku.Base;
    using LiveRoku.Base.Plugin;
    using LiveRoku.Base.Logger;
    public class LevitatedUI : LiveResolverBase, IPlugin, ILogHandler {
        public string Token => typeof(LevitatedUI).FullName;
        public IPluginDescriptor Descriptor { get; } = new PluginDescriptor {
            Name = nameof(LevitatedUI),
            Description = nameof(LevitatedUI),
            Version = "0.0.0.1",
            Author = "epxoxy"
        };

        [PluginSetting]
        public EasySettings Extra { get; private set; }
        //private FloatingBox box;
        private object locker = new object();
        private IFloatingHost box;
        private bool skipTVGiftDm;

        public LevitatedUI () { }

        public void onInitialize (ISettings settings) {
            box?.close();
            box = null;
            Extra = Extra ?? new EasySettings ();
            skipTVGiftDm = Extra.get("skip-tv-gift-danmaku", false);
            Extra.put("skip-tv-gift-danmaku", skipTVGiftDm);
            var defaultBox = typeof (MessageFlowBox).Name;
            var boxType = Extra.get("token-box", defaultBox);
            var app = Application.Current;
            if (defaultBox.Equals (boxType)) {
                Extra.put("token-box", typeof(MessageFlowBox).Name);
                app.Dispatcher.Invoke(() => {
                    var win = Application.Current?.MainWindow;
                    box = new MessageFlowBox(Extra) { Owner = win };
                });
            } else {
                Extra.put("token-box", typeof(FloatingBox).Name);
                app.Dispatcher.Invoke(() => {
                    var win = app?.MainWindow;
                    box = new FloatingBox(Extra) { Owner = win };
                });
            }
            box.setOnClick (() => {
                box.addMessage ("Tips", "Nothing yet!");
            });
        }

        public void onAttach (IContext ctx) {
            if (box == null) return;
            if (ctx != null && ctx.Fetcher != null) {
                ctx.Fetcher.Logger.LogHandlers.add(this);
                ctx.Fetcher.DownloadProgressBinders.add (this);
                ctx.Fetcher.DanmakuHandlers.add(this);
                ctx.Fetcher.StatusBinders.add (this);
                box.EasyAccessFolder = ctx.Preferences.StoreFolder;
            }
            box.show ();
        }

        public void onDetach (IContext ctx) {
            ctx.Fetcher.Logger.LogHandlers.remove(this);
            ctx.Fetcher.DownloadProgressBinders.remove(this);
            ctx.Fetcher.DanmakuHandlers.remove(this);
            ctx.Fetcher.StatusBinders.remove(this);
            if (box == null)
                return;
            box.onStoringSettings(Extra);
            box.close();
            box = null;
        }

        public override void onLog(Level level, string message) {
            box.addMessage(level.ToString(), message);
        }

        public override void onDanmakuReceive(DanmakuModel danmaku) {
            base.onDanmakuReceive(danmaku);
            //Show only the chat message
            if ((danmaku.MsgType == MsgTypeEnum.Comment)) {
                //Skip the "SmallTV" gift auto danmaku if in need
                if (string.IsNullOrEmpty(danmaku.RoomID) || !skipTVGiftDm) {
                    box.addMessage(danmaku.UserName, danmaku.CommentText);
                }
            } else if(danmaku.MsgType == MsgTypeEnum.SystemMsg && danmaku.Extra != null) {
                SmallTV smallTV = null;
                if ((smallTV = danmaku.Extra as SmallTV) != null) {
                    box.addMessage(smallTV.RealRoomId, smallTV.Url);
                }
            }
        }

        public override void onLiveStatusUpdate(bool isOn) {
            box.updateLiveStatus (isOn);
            if (isOn) {
                box.updateTips (TipsType.Normal, string.Empty);
            } else {
                box.updateTips (TipsType.Blue, "Stop");
            }
        }

        public override void onHotUpdateByDanmaku(long popularity) {
            box.updateHot(toBaseKUnit(popularity));
        }

        public override void onDownloadSizeUpdate (long totalSize, string sizeText) {
            box.updateSizeText (sizeText);
        }
        
        public override void onPreparing (IContext ctx) {
            box.updateSizeText ("----");
            box.updateTips (TipsType.Yellow, "Prepare");
            box.updateLiveStatus (false);
            box.updateIsRunning(true);
            box.EasyAccessFolder = ctx.RuntimeExtra.get<string>("store-folder", null);
            box.danmakuOnlyModeSetTo(!ctx.Fetcher.RuntimeExtra.get("video-require", true));
        }

        public override void onWaiting (IContext ctx) {
            box.updateTips (TipsType.Yellow, "Wait");
            box.updateLiveStatus (false);
        }

        public override void onStreaming (IContext ctx) {
            box.updateTips (TipsType.Normal, string.Empty);
            box.updateLiveStatus (true);
        }

        public override void onStopped (IContext ctx) {
            box.updateTips (TipsType.Blue, "Stop");
            box.updateLiveStatus (false);
            box.updateIsRunning(false);
        }

        private string toBaseKUnit(long v) {
            if (v > 1000)
                return string.Format("{0:N2}k", v / 1000d);
            return v.ToString();
        }
    }
}