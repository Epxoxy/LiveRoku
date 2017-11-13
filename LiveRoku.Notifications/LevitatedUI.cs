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
            if (box != null) {
                box.close ();
                box = null;
            }
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
            box.onClick (() => {
                box.addMessage ("Tips", "Nothing yet!");
            });
        }

        public void onAttach (IPluginHost host) {
            if (box == null) return;
            if (host!=null && host.Fetcher != null) {
                host.Fetcher.Logger.LogHandlers.add(this);
                host.Fetcher.LiveProgressBinders.add (this);
                host.Fetcher.DanmakuHandlers.add(this);
                host.Fetcher.StatusBinders.add (this);
            }
            box.show ();
        }

        public void onDetach (IPluginHost host) {
            if (box == null)
                return;
            box.putSettingsTo(Extra);
            box.close();
            box = null;
        }

        public void onLog(Level level, string message) {
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

        public override void onLiveStatusUpdate(bool on) {
            box.updateStatus (on);
            if (on) {
                box.updateTips (TipsType.Normal, string.Empty);
            } else {
                box.updateTips (TipsType.Blue, "Stop");
            }
        }

        public override void onHotUpdateByDanmaku(long popularity) { }

        public override void onDownloadSizeUpdate (long totalSize, string sizeText) {
            box.updateSizeText (sizeText);
        }
        
        public override void onPreparing () {
            box.updateSizeText ("0000.00");
            box.updateTips (TipsType.Yellow, "Prepare");
            box.updateStatus (false);
        }

        public override void onWaiting () {
            box.updateTips (TipsType.Yellow, "Wait");
            box.updateStatus (false);
        }

        public override void onStreaming () {
            box.updateTips (TipsType.Normal, string.Empty);
            box.updateStatus (true);
        }

        public override void onStopped () {
            box.updateTips (TipsType.Blue, "Stop");
            box.updateStatus (false);
        }

    }
}