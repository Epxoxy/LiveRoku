using System.Windows;
using LiveRoku.Base;
using LiveRoku.Base.Plugin;
using LiveRoku.Base.Logger;
namespace LiveRoku.Notifications {
    public class LevitatedUI : LiveProgressBinderBase, IPlugin, ILogHandler, IStatusBinder {
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

        public LevitatedUI () { }

        public void onInitialize (ISettings settings) {
            if (box != null) {
                box.close ();
                box = null;
            }
            Extra = Extra ?? new EasySettings ();
            var defaultBox = typeof (MessageFlowBox).Name;
            var exist = Extra.get("token-box", defaultBox);
            var app = Application.Current;
            if (defaultBox.Equals (exist)) {
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
                host.Fetcher.LiveProgressBinders.add (this);
                host.Fetcher.StatusBinders.add (this);
                host.Fetcher.Logger.LogHandlers.add (this);
                host.Fetcher.DanmakuHandlers.add (onDanmaku);
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

        public void onDanmaku (DanmakuModel danmaku) {
            if (danmaku.MsgType == MsgTypeEnum.Comment)
                box.addMessage (danmaku.UserName, danmaku.CommentText);
        }

        public override void onStatusUpdate (bool on) {
            box.updateStatus (on);
            if (on) {
                box.updateTips (TipsType.Normal, string.Empty);
            } else {
                box.updateTips (TipsType.Blue, "Stop");
            }
        }

        public override void onDownloadSizeUpdate (long totalSize, string sizeText) {
            box.updateSizeText (sizeText);
        }

        public override void onHotUpdate (long popularity) { }

        public void onLog (Level level, string message) {
            box.addMessage (level.ToString (), message);
        }

        public void onPreparing () {
            box.updateSizeText ("0000.00K");
            box.updateTips (TipsType.Yellow, "Prepare");
            box.updateStatus (false);
        }

        public void onWaiting () {
            box.updateTips (TipsType.Yellow, "Wait");
            box.updateStatus (false);
        }

        public void onStreaming () {
            box.updateTips (TipsType.Normal, string.Empty);
            box.updateStatus (true);
        }

        public void onStopped () {
            box.updateTips (TipsType.Blue, "Stop");
            box.updateStatus (false);
        }

    }
}