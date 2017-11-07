using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using LiveRoku.Base;
using LiveRoku.UI;
using LiveRoku.Base.Logger;
using LiveRoku.Loader;
using System.Runtime.CompilerServices;
using PropertyChanged;
using System.Diagnostics;
using System.Windows.Data;
using System.Globalization;

namespace LiveRoku {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        
        private UIElement viewAboutLink => aboutLinkLabel;
        private UIElement exploreFolder => savepathTextLabel;
        private UIElement exploreFolder2 => exploreArea;
        
        private CoreBridge bridge = new CoreBridge();
        
        public MainWindow () {
            InitializeComponent ();
            this.DataContext = bridge;
            this.Loaded += onLoaded;
        }

        private void onLoaded (object sender, RoutedEventArgs e) {
            this.Loaded -= onLoaded;
            bridge.invokeLoad(coreLoaded => {
                Dispatcher.invokeSafely(() => {
                    //Subscribe basic events
                    purgeEvents(resubscribe: true, justBasicEvent: !coreLoaded);
                });
            }, tipsMsgByTask);
        }

        private void tipsMsgByTask(Exception e) {
            Task.Run(() => MessageBox.Show(e.Message, "Error"));
        }

        protected override void OnClosing (CancelEventArgs e) {
            purgeEvents ();
            bridge.detachAndSave();
            base.OnClosing (e);
        }

        #region -------------- event handlers --------------
        //Part for controlling progress
        //Subscribe function like start, about,set/explore location

        private void purgeEvents (bool resubscribe = false, bool justBasicEvent = false) {
            exploreFolder.MouseLeftButtonUp -= explore;
            exploreFolder2.MouseLeftButtonUp -= explore;
            viewAboutLink.MouseLeftButtonUp -= showAbout;
            modifyLoationBtn.Click -= setLocation;
            startEndBtn01.Click -= startOrStop;
            startEndBtn02.Click -= startOrStop;
            if (resubscribe) {
                exploreFolder.MouseLeftButtonUp += explore;
                exploreFolder2.MouseLeftButtonUp += explore;
                viewAboutLink.MouseLeftButtonUp += showAbout;
                modifyLoationBtn.Click += setLocation;
                if (!justBasicEvent) {
                    startEndBtn01.Click += startOrStop;
                    startEndBtn02.Click += startOrStop;
                }
            }
        }

        private void scrollEnd (object sender, TextChangedEventArgs e) {
            (sender as TextBoxBase) ? .ScrollToEnd ();
        }

        private void startOrStop (object sender, RoutedEventArgs e) {
            if (bridge.Fetcher == null) return;
            if (bridge.Fetcher.IsRunning)
                bridge.Fetcher.stop ();
            else bridge.Fetcher.start ();
        }

        private void setLocation (object sender, RoutedEventArgs e) {
            var dialog = new LocationSettings () {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Folder = bridge.Folder,
                FileName = bridge.FileFormat
            };
            if (dialog.ShowDialog () == true) {
                bridge.Folder = dialog.Folder;
                bridge.FileFormat = dialog.FileName;
                bridge.LocationFormat = System.IO.Path.Combine (bridge.Folder, bridge.FileFormat);
            }
        }

        private void explore (object sender, MouseButtonEventArgs e) {
            System.Diagnostics.Process.Start (bridge.Folder);
        }

        private void showAbout (object sender, MouseButtonEventArgs e) {
            new MessageDialog (this) {
                Title = Constant.AboutText,
                    Content = new TextBox {
                        Text = Properties.Resources.About,
                            FontSize = 18,
                            MaxWidth = 450,
                            TextWrapping = TextWrapping.Wrap,
                            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                            Foreground = System.Windows.Media.Brushes.DimGray,
                            Style = FindResource ("OutputBox") as Style
                    }
            }.ShowDialog ();
        }

        private void updateTitleClick (object sender, RoutedEventArgs e) {
            bridge.updateRoomInfo();
        }

        private void hideTitleClick (object sender, MouseButtonEventArgs e) {
            titleView.Visibility = titleView.Visibility == Visibility.Visible ?
                Visibility.Hidden :
                Visibility.Visible;
        }
        #endregion ------------------------------------------

    }

    //TODO Implement MVVM
    //TODO Move command from MainWindow's event handlers
    [AddINotifyPropertyChangedInterface]
    public class CoreBridge : LiveResolverBase, IPreferences, ILogHandler {
        public string ShortRoomId { get; set; }
        public bool IsShortIdTheRealId { get; set; } = false;
        public string Folder { get; set; }
        public string FileFormat { get; set; }
        public bool DanmakuRequire { get; set; } = true;
        public bool VideoRequire { get; set; } = true;
        public bool AutoStart { get; set; } = true;
        public string UserAgent { get; set; } = string.Empty;
        [DoNotNotify]
        public ISettingsBase Extra { get; set; }

        public string LocationFormat { get; set; }
        public long Popularity { get; set; }
        public string BitRate { get; set; }
        public string ReceiveSize { get; set; }
        public string Duration { get; set; }
        public bool CmdEnabled { get; set; } = true;
        public bool PreferencesEnabled { get; set; } = true;
        public IRoomInfo RoomInfo { get; set; }
        public BootStates BootStates { get; set; }
        public string LiveStatusText { get; set; }

        internal ILiveFetcher Fetcher => ctx?.Fetcher;
        private LoadContext ctx;
        private LoadManager mgr;
        private bool isLoaded = false;
        
        public Task invokeLoad(Action<bool> onCoreLoaded, Action<Exception> tipsError) {
            if (isLoaded)
                return Task.FromResult(false);
            return Task.Run(() => {
                isLoaded = true;
                mgr = new LoadManager(AppDomain.CurrentDomain.BaseDirectory);
                LoadContextBase ctxBase = null;
                bool coreLoaded = false;
                Utils.runSafely(() => {
                    ctxBase = mgr.initCtxBase();
                    coreLoaded = ctxBase?.LoadOk == true;
                    this.restore(ctxBase.AppLocalData.getAppSettings());
                }, tipsError);
                onCoreLoaded?.Invoke(coreLoaded);
                //Create core context
                try { if (!coreLoaded || (ctx = mgr.create(this)) == null) return; }
                catch (Exception ex) { tipsError(ex); }
                //Register handlers of this to fetcher
                ctx.Fetcher.Logger.LogHandlers.add(this);
                ctx.Fetcher.LiveProgressBinders.add(this);
                ctx.Fetcher.DanmakuHandlers.add(this);
                ctx.Fetcher.StatusBinders.add(this);
                ctx.Plugins.ForEach(plugin => {
                    Utils.runSafely(() => {
                        plugin.onInitialize(ctx.AppLocalData.getAppSettings());
                        ctx.Fetcher.Logger.log(Level.Info, $"{plugin.GetType().Name} Loaded.");
                    });
                    Utils.runSafely(() => {
                        plugin.onAttach(ctx);
                        ctx.Fetcher.Logger.log(Level.Info, $"{plugin.GetType().Name} Attach.");
                    });
                });
            });
        }

        public void detachAndSave() {
            if (ctx != null) {
                ctx.Fetcher?.stop();
                ctx.Fetcher?.Dispose();
                ctx.Plugins.ForEach(plugin => {
                    Utils.runSafely(() => plugin.onDetach(ctx));
                });
                MyPreferences pref = new MyPreferences();
                update(ctx.AppLocalData.getAppSettings(), ref pref);
                ctx.saveAppData();
            }
        }

        public void restore(ISettings settings) {
            var previous = settings.get<MyPreferences>("Args", null);
            if (previous == null)
                return;
            ShortRoomId = previous.LastRoomId.ToString();
            Folder = previous.DownloadFolder;
            FileFormat = previous.DownloadFileNameFormat;
            LocationFormat = System.IO.Path.Combine(Folder, FileFormat);
            DanmakuRequire = previous.DanmakuRequire;
            VideoRequire = previous.VideoRequire;
            AutoStart = previous.AutoStart;
        }

        public void update(ISettings settings, ref MyPreferences pref) {
            if (pref == null)
                pref = new MyPreferences();
            if (int.TryParse(ShortRoomId, out int roomId)) {
                pref.addLastRoomId(roomId);
            }
            pref.DownloadFolder = Folder;
            pref.DownloadFileNameFormat = FileFormat;
            pref.DanmakuRequire = DanmakuRequire;
            pref.VideoRequire = VideoRequire;
            pref.AutoStart = AutoStart;
            settings.put("Args", pref);
        }

        public Task updateRoomInfo() {
            return Task.Run(() => {
                this.RoomInfo = this.Fetcher?.getRoomInfo(true);
            });
        }

        public void onLog(Level level, string message) {
            string info = $"[{level}] {message}\n";
            Debug.WriteLine(message, level.ToString());
            /*Dispatcher.invokeSafely(() => {
                if (debugView.Text.Length > 25600) {
                    var text = debugView.Text.Substring(12800);
                    var index2 = text.IndexOf("\n");
                    var builder = new StringBuilder();
                    if (index2 > 0) {
                        builder.Append(text.Substring(index2));
                    } else {
                        builder.Append(text);
                    }
                    builder.Append(info);
                    debugView.Clear();
                    debugView.AppendText(builder.ToString());
                } else {
                    debugView.AppendText(info);
                }
            });*/
        }

        //--------------- IStatusBinder ------------------
        public override void onPreparing() {
            BootStates = BootStates.Preparing;
            PreferencesEnabled = false;
            CmdEnabled = false;
        }

        public override void onWaiting() {
            BootStates = BootStates.Waiting;
            PreferencesEnabled = false;
            CmdEnabled = true;
        }

        public override void onStreaming() {
            BootStates = BootStates.Streaming;
            PreferencesEnabled = false;
            CmdEnabled = true;
        }

        public override void onStopped() {
            BootStates = BootStates.Stopped;
            PreferencesEnabled = true;
            CmdEnabled = true;
        }

        //--------------- IDanmakuResolver ------------------
        public override void onLiveStatusUpdateByDanmaku(bool isOn) {
            LiveStatusText = isOn ? Constant.LiveOnText : Constant.LiveOffText;
        }

        public override void onHotUpdateByDanmaku(long popularity) {
            Popularity = popularity;
        }

        //--------------- IDownloadProgressBinder ------------------
        public override void onDurationUpdate(long duration, string timeText) {
            Duration = timeText;
        }

        public override void onDownloadSizeUpdate(long size, string sizeText) {
            ReceiveSize = sizeText;
        }

        public override void onBitRateUpdate(long bitRate, string bitRateText) {
            BitRate = bitRateText;
        }
        
    }

    public enum BootStates {
        Stopped,
        Preparing,
        Waiting,
        Streaming
    }

    public class BootStatesToValueConverter : IValueConverter {
        public object StoppedValue { get; set; }
        public object PreparingValue { get; set; }
        public object WaitingValue { get; set; }
        public object StreamingValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is BootStates) {
                switch ((BootStates)value) {
                    case BootStates.Stopped:
                        return StoppedValue;
                    case BootStates.Preparing:
                        return PreparingValue;
                    case BootStates.Waiting:
                        return WaitingValue;
                    case BootStates.Streaming:
                        return StreamingValue;
                }
            }
            return StoppedValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    static class Utils {
        public static void runSafely (Action doWhat) {
            try {
                doWhat?.Invoke ();
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine (e.ToString ());
            }
        }

        public static void runSafely (Action doWhat, Action<Exception> onError) {
            try {
                doWhat?.Invoke ();
            } catch (Exception e) {
                onError.Invoke(e);
            }
        }

        public static T runSafely<T> (Func<T> doWhat, Action<Exception> onError) {
            try {
                return doWhat.Invoke ();
            } catch (Exception e) {
                onError.Invoke(e);
            }
            return default(T);
        }

        public static void able (this bool enable, params UIElement[] elements) {
            foreach (UIElement element in elements) {
                element.IsEnabled = enable;
            }
        }

        public static void invokeSafely (this Dispatcher dispatcher, Action action) {
            if (Thread.CurrentThread == dispatcher.Thread) {
                action ();
            } else dispatcher.Invoke (DispatcherPriority.Normal, action);
        }

        public static TResult invokeSafely<TResult> (this Dispatcher dispatcher, Func<TResult> func) {
            if (Thread.CurrentThread == dispatcher.Thread) {
                return func ();
            } else return dispatcher.Invoke<TResult> (func, DispatcherPriority.Normal);
        }

    }

    //TODO Delete it
    public class Bindable : INotifyPropertyChanged {
        private Dictionary<string, object> properties = new Dictionary<string, object>();

        protected T Get<T>([CallerMemberName] string name = null) {
            Debug.Assert(name != null, "name != null");
            object value = null;
            if (properties.TryGetValue(name, out value))
                return value == null ? default(T) : (T)value;
            return default(T);
        }

        protected void Set<T>(T value, [CallerMemberName] string name = null) {
            Debug.Assert(name != null, "name != null");
            if (Equals(value, Get<T>(name)))
                return;
            properties[name] = value;
            OnPropertyChanged(name);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

}