using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using LiveRoku.Base;
using LiveRoku.UI;

namespace LiveRoku {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, ILogger, IRequestModel, ILiveDataResolver, IStatusBinder {
        #region ------- UIElements proxy -----------
        //--------------------------------------
        //PART A : UIElements for IConfigHolder
        //--------------------------------------
        private TextBox roomIdBox => roomIdTBox;
        private CheckBox saveDanmaku => saveCommentBox;
        private CheckBox autoStart => autoStartBox;
        private TextBox locationBox => locationTBox;

        //--------------------------------------
        //PART B : UIElement for extend function
        //--------------------------------------
        private UIElement aboutLink => aboutLinkLabel;
        private UIElement exploreFolder => savepathTextLabel;
        private ButtonBase editPathBtn => openSavepathConfigDialogButton;
        private UIElement configViewRoot => paramsElements;

        //--------------------------------------
        //PART C : UIElements for controlling process
        //--------------------------------------
        private ButtonBase ctlBtn01 => startEndBtn01;
        private ButtonBase ctlBtn02 => startEndBtn02;

        //--------------------------------------
        //PART D : Some resources
        //--------------------------------------
        private UIElement waitingSymbol;
        private UIElement stoppedSymbol;
        private UIElement startedSymbol;

        #endregion -----------------------------

        //--------------------------------------
        //IMPLEMENTS Download parameters model
        //--------------------------------------
        public string RoomId => roomIdBox.Text;
        public string Folder => settings.DownloadFolder;
        public string FileFormat => settings.DownloadFileFormat;
        public bool DownloadDanmaku => saveDanmaku.IsChecked == true;
        public bool AutoStart => autoStart.IsChecked == true;
        //For generating file name
        public string formatFileName (string realRoomId) {
            return FileFormat.formatPath (realRoomId);
        }

        //Helpers
        private ILiveDownloader downloader;
        private MySettings settings;
        private IStorage storage;
        private List<IPlugin> plugins;
        private bool coreLoaded;

        public MainWindow () {
            InitializeComponent ();
            this.Loaded += onLoaded;
        }

        private void onLoaded (object sender, RoutedEventArgs e) {
            this.Loaded -= onLoaded;
            coreLoaded = false;
            object instance = null;
            var types = PluginLoader.LoadTypesListImpl<ILiveDownloader> (App.coreFolder, App.instance);
            if (types == null || types.Count () <= 0 ||
                (instance = Activator.CreateInstance (types.First (), this, "")) == null ||
                (downloader = instance as ILiveDownloader) == null) {
                string msg = types == null ? "Core dll not exist." : "Load core dll fail.";
                System.Threading.Tasks.Task.Run (() => { MessageBox.Show (msg, "Error"); });
                return;
            }
            coreLoaded = true;
            plugins = PluginLoader.LoadInstances<IPlugin> (App.pluginFolder, App.instance);
            storage = Storage.StorageHelper.instance (App.dataFolder);
            plugins.ForEach (p => p.onInitialize (storage));
            stoppedSymbol = FindResource (Constant.PauseSymbolKey) as UIElement;
            startedSymbol = FindResource (Constant.RightSymbolKey) as UIElement;
            waitingSymbol = FindResource (Constant.LoadingSymbolKey) as UIElement;
            //Subscribe events
            purgeEvents (resubscribe : true);
            initSettings ();
            //Generate helpers
            //downloader = new LiveDownloader(this, "");
            downloader.LiveDataResolvers.add (this);
            downloader.StatusBinders.add (this);
            downloader.Loggers.add (this);
            plugins.ForEach (p => p.onAttach (downloader));
        }

        protected override void OnClosing (CancelEventArgs e) {
            if (coreLoaded) {
                purgeEvents ();
                downloader.stop ();
                plugins.ForEach (p => p.onDetach ());
                saveSettings ();
            }
            base.OnClosing (e);
        }

        private void initSettings () {
            //Load settings
            if (!storage.tryGet<MySettings> ("settings", out settings)) {
                settings = new MySettings ();
            }
            roomIdBox.Text = settings.LastRoomId.ToString ();
            saveDanmaku.IsChecked = settings.DownloadDanmaku;
            autoStart.IsChecked = settings.AutoStart;
            locationBox.Text = System.IO.Path.Combine (Folder, FileFormat);
        }

        private void saveSettings () {
            int roomId = 0;
            if (int.TryParse (RoomId, out roomId)) {
                settings.addLastRoomId (roomId);
            }
            settings.AutoStart = AutoStart;
            settings.DownloadDanmaku = DownloadDanmaku;
            storage.add ("settings", settings);
            storage.save ();
        }

        //Part for controlling progress
        //Subscribe function like start, about,set/explore location
        #region -------------- event handlers --------------
        private void purgeEvents (bool resubscribe = false) {
            exploreFolder.MouseLeftButtonUp -= explore;
            aboutLink.MouseLeftButtonUp -= showAbout;
            editPathBtn.Click -= setLocation;
            ctlBtn01.Click -= startOrStop;
            ctlBtn02.Click -= startOrStop;
            if (resubscribe) {
                exploreFolder.MouseLeftButtonUp += explore;
                aboutLink.MouseLeftButtonUp += showAbout;
                editPathBtn.Click += setLocation;
                ctlBtn01.Click += startOrStop;
                ctlBtn02.Click += startOrStop;
            }
        }

        private void scrollEnd (object sender, TextChangedEventArgs e) { }

        private void startOrStop (object sender, RoutedEventArgs e) {
            if (downloader == null) return;
            if (downloader.IsRunning)
                downloader.stop ();
            else downloader.start ();
        }

        private void setLocation (object sender, RoutedEventArgs e) {
            var dialog = new LocationSettings () {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Folder = settings.DownloadFolder,
                FileName = settings.DownloadFileFormat
            };
            if (dialog.ShowDialog () == true) {
                settings.DownloadFolder = dialog.Folder;
                settings.DownloadFileFormat = dialog.FileName;
                locationBox.Text = System.IO.Path.Combine (Folder, FileFormat);
            }
        }

        private void explore (object sender, MouseButtonEventArgs e) {
            System.Diagnostics.Process.Start (Folder);
        }

        private void showAbout (object sender, MouseButtonEventArgs e) {
            new MessageDialog (this) {
                Title = "About LiveRoku",
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
        #endregion ------------------------------------------

        //Part for controlling the UIElement status
        //And the process of downloader
        #region ------------- implement IStatusBinder -------------
        public void onPreparing () {
            Dispatcher.invokeSafely (() => {
                ctlBtn01.Content = Constant.PreparingText;
                ctlBtn02.Content = waitingSymbol;
                false.able (ctlBtn01, ctlBtn02, configViewRoot);
            });
        }

        public void onWaiting () {
            Dispatcher.invokeSafely (() => {
                ctlBtn01.Content = Constant.WaitingText;
                ctlBtn02.Content = waitingSymbol;
                false.able (configViewRoot);
                true.able (ctlBtn01, ctlBtn02);
            });
        }

        public void onStreaming () {
            Dispatcher.invokeSafely (() => {
                ctlBtn01.Content = Constant.StopText;
                ctlBtn02.Content = startedSymbol;
                true.able (ctlBtn01, ctlBtn02);
            });
        }

        public void onStopped () {
            Dispatcher.invokeSafely (() => {
                ctlBtn01.Content = Constant.StartText;
                ctlBtn02.Content = stoppedSymbol;
                true.able (ctlBtn01, ctlBtn02, configViewRoot);
            });
        }
        #endregion ---------------------------------------------

        //Part for showing some data in UIElements which named end with view
        #region ------------- implement interfaces -------------

        public void appendLine (string tag, string log) {
            string info = $"[{tag}] {log}\n";
            System.Diagnostics.Debug.Write (info);
            Dispatcher.invokeSafely (() => { debugView.AppendText (info); });
        }

        public void onLiveStatusUpdate (LiveStatus status) {
            string tips = status.getText ();
            appendLine ("Now status", tips);
        }

        public void onDurationUpdate (long duration, string timeText) {
            Dispatcher.invokeSafely (() => { recordTimeView.Content = timeText; });
        }

        public void onDownloadSizeUpdate (long size, string sizeText) {
            Dispatcher.invokeSafely (() => { sizeView.Content = sizeText; });
        }

        public void onBitRateUpdate (long bitRate, string bitRateText) {
            Dispatcher.invokeSafely (() => { bitRateView.Content = bitRateText; });
        }

        public void onHotUpdate (long popularity) {
            Dispatcher.invokeSafely (() => { userCountView.Content = popularity; });
            System.Diagnostics.Debug.WriteLine ("Updated : Hot -> " + popularity);
        }

        #endregion ---------------------------------------------

    }

    static class Utils {

        public static void able (this bool enable, params UIElement[] elements) {
            foreach (UIElement element in elements) {
                element.IsEnabled = enable;
            }
        }

        public static string getText (this LiveStatus status) {
            switch (status) {
                case LiveStatus.Start:
                    return "On Live";
                case LiveStatus.End:
                    return "Live End";
                case LiveStatus.Unchecked:
                    return "Waiting";
                default:
                    return string.Empty;
            }
        }

        public static void invokeSafely (this Dispatcher dispatcher, Action action) {
            if (Thread.CurrentThread == dispatcher.Thread) {
                action ();
            } else dispatcher.Invoke (DispatcherPriority.Normal, action);
        }

        public static string formatPath (this string format, string roomId) {
            return format.Replace ("{roomId}", roomId)
                .Replace ("{Y}", DateTime.Now.Year.ToString ())
                .Replace ("{M}", DateTime.Now.Month.ToString ())
                .Replace ("{d}", DateTime.Now.Day.ToString ())
                .Replace ("{H}", DateTime.Now.Hour.ToString ())
                .Replace ("{m}", DateTime.Now.Minute.ToString ())
                .Replace ("{s}", DateTime.Now.Second.ToString ());;
        }
    }

}