using System.Collections.Generic;
using System.Windows;
using LiveRoku.Base;
using LiveRoku.Floating;


namespace LiveRoku.Plugin {
    public class FloatingPlugin : LiveProgressBinderBase, IPlugin, ILogHandler, IStatusBinder {
        //private FloatingBox box;
        private object locker = new object ();
        private IHost box;
        private IStorage storage;
        private Settings settings;

        class Settings {
            public string BoxToken { get; set; }
            public Dictionary<string, object> Extra { get; set; }
        }

        public string Name => nameof (FloatingPlugin);

        public string Description => nameof (FloatingPlugin);

        public FloatingPlugin () { }

        public void onInitialize (IStorage storage) {
            this.storage = storage;
            if (box != null) {
                box.close ();
                box = null;
            }
            storage?.tryGet ("float-plugin-settings", out settings);
            settings = settings ?? new Settings ();
            settings.Extra = settings.Extra ?? new Dictionary<string, object> ();
            var defaultBox = typeof (FloatingBox).Name;
            if (defaultBox.Equals (settings.BoxToken)) {
                settings.BoxToken = typeof (FloatingBox).Name;
                box = new FloatingBox (storage) {
                    Owner = Application.Current.MainWindow
                };
            } else {
                settings.BoxToken = typeof (MessageFlowBox).Name;
                box = (new MessageFlowBox (storage) {
                    Owner = Application.Current.MainWindow
                });
            }
            box.onClick (() => {
                box.addMessage ("Tips", "Nothing yet!");
            });
        }

        public void onAttach (ILiveFetcher fetcher) {
            if (box == null) return;
            if (fetcher != null) {
                fetcher.LiveProgressBinders.add (this);
                fetcher.StatusBinders.add (this);
                fetcher.Logger.LogHandlers.add (this);
                fetcher.DanmakuHandlers.add (onDanmaku);
            }
            box.show ();
        }

        public void onDetach () {
            storage.add ("float-plugin-settings", settings);
            storage.save ();
            if (box == null)
                return;
            box.saveSettings ();
            box.close ();
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