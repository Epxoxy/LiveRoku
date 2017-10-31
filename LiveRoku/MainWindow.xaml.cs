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

namespace LiveRoku {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, ILogHandler, IFetchArgsHost, ILiveProgressBinder, IStatusBinder {
        #region ------- UIElements proxy -----------
        //--------------------------------------
        //PART A : UIElements for IConfigHolder
        //--------------------------------------
        private TextBox roomIdBox => roomIdTBox;
        private CheckBox saveDanmaku => saveCommentBox;
        private CheckBox autoStart => autoStartBox;
        private TextBlock locationBox => locationTBox;

        //--------------------------------------
        //PART B : UIElement for extend function
        //--------------------------------------
        private UIElement aboutLink => aboutLinkLabel;
        private UIElement exploreFolder => savepathTextLabel;
        private UIElement exploreFolder2 => exploreArea;
        private ButtonBase editPathBtn => openSavepathConfigDialogButton;
        private UIElement configViewRoot => paramsElements;

        //--------------------------------------
        //PART C : UIElements for controlling process
        //--------------------------------------
        private ButtonBase downloadCtl => startEndBtn01;
        private ButtonBase downloadCtl2 => startEndBtn02;

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
        public string RoomId => Dispatcher.invokeSafely (() => roomIdBox.Text);
        public string Folder => Dispatcher.invokeSafely (() => settings.DownloadFolder);
        public string FileFormat => Dispatcher.invokeSafely (() => settings.DownloadFileFormat);
        public bool DownloadDanmaku => Dispatcher.invokeSafely (() => saveDanmaku.IsChecked == true);
        public bool AutoStart => Dispatcher.invokeSafely (() => autoStart.IsChecked == true);
        public string UserAgent => string.Empty;

        //Helpers
        private MySettings settings;
        private LoadManager mgr;
        private LoadContext ctx;

        public MainWindow () {
            InitializeComponent ();
            this.Loaded += onLoaded;
        }

        private void onLoaded (object sender, RoutedEventArgs e) {
            this.Loaded -= onLoaded;
            //Load basic resources
            stoppedSymbol = FindResource (Constant.StopSymbolKey) as UIElement;
            startedSymbol = FindResource (Constant.RightSymbolKey) as UIElement;
            waitingSymbol = FindResource (Constant.LoadingSymbolKey) as UIElement;
            mgr = new LoadManager(AppDomain.CurrentDomain.BaseDirectory);
            LoadContextBase ctxBase = null;
            try {
                ctxBase = mgr.initCtxBase();
                var mArgs = ctxBase.AppLocalData.getAppSettings().get("Args", new MySettings());
                this.settings = mArgs;
                initSettings(mArgs);
            } catch(Exception ex) {
                Task.Run (() => { MessageBox.Show (ex.Message, "Error"); });
            }
            this.settings = this.settings ?? new MySettings();
            var coreLoaded = ctxBase?.LoadOk == true;
            //Subscribe basic events
            Dispatcher.invokeSafely (() => purgeEvents (resubscribe : true, justBasicEvent: !coreLoaded));
            try {
                if (!coreLoaded || (ctx = mgr.create(this)) == null) return;
            } catch(Exception ex) {
                Task.Run (() => { MessageBox.Show (ex.Message, "Error"); });
            }
            ctx.Fetcher.Logger.LogHandlers.add(this);
            ctx.Fetcher.LiveProgressBinders.add(this);
            ctx.Fetcher.StatusBinders.add(this);
            ctx.Plugins.ForEach(plugin => {
                Utils.runSafely(() => {
                    plugin.onInitialize(ctx.AppLocalData.getAppSettings());
                    ctx.Fetcher.Logger.log(Level.Info, $"{plugin.GetType().Name} loaded.");
                });
                Utils.runSafely(() => {
                    plugin.onAttach(ctx);
                    ctx.Fetcher.Logger.log(Level.Info, $"{plugin.GetType().Name} Attach.");
                });
            });
        }

        protected override void OnClosing (CancelEventArgs e) {
            purgeEvents ();
            if(ctx != null) {
                ctx.Fetcher?.stop();
                ctx.Fetcher?.Dispose();
                ctx.Plugins.ForEach(plugin => {
                    Utils.runSafely(() => plugin.onDetach(ctx));
                });
                //assign settings
                if (int.TryParse(RoomId, out int roomId)) {
                    settings.addLastRoomId(roomId);
                }
                settings.AutoStart = AutoStart;
                settings.DownloadDanmaku = DownloadDanmaku;
                //save data
                ctx.AppLocalData.getAppSettings().put("Args", this.settings);
                ctx.saveAppData();
            }
            base.OnClosing (e);
        }
        
        private void initSettings (MySettings settings) {
            Dispatcher.invokeSafely (() => {
                roomIdBox.Text = settings.LastRoomId.ToString ();
                saveDanmaku.IsChecked = settings.DownloadDanmaku;
                autoStart.IsChecked = settings.AutoStart;
                locationBox.Text = System.IO.Path.Combine (Folder, FileFormat);
            });
        }
        
        //Part for controlling progress
        //Subscribe function like start, about,set/explore location
        #region -------------- event handlers --------------
        private void purgeEvents (bool resubscribe = false, bool justBasicEvent = false) {
            exploreFolder.MouseLeftButtonUp -= explore;
            exploreFolder2.MouseLeftButtonUp -= explore;
            aboutLink.MouseLeftButtonUp -= showAbout;
            editPathBtn.Click -= setLocation;
            downloadCtl.Click -= startOrStop;
            downloadCtl2.Click -= startOrStop;
            if (resubscribe) {
                exploreFolder.MouseLeftButtonUp += explore;
                exploreFolder2.MouseLeftButtonUp += explore;
                aboutLink.MouseLeftButtonUp += showAbout;
                editPathBtn.Click += setLocation;
                if (!justBasicEvent) {
                    downloadCtl.Click += startOrStop;
                    downloadCtl2.Click += startOrStop;
                }
            }
        }

        private void scrollEnd (object sender, TextChangedEventArgs e) {
            (sender as TextBoxBase) ? .ScrollToEnd ();
        }

        private void startOrStop (object sender, RoutedEventArgs e) {
            if (ctx.Fetcher == null) return;
            if (ctx.Fetcher.IsRunning)
                ctx.Fetcher.stop ();
            else ctx.Fetcher.start ();
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
            Task.Run (() => {
                var title = ctx.Fetcher.getRoomInfo (true) ?.Title;
                if (string.IsNullOrEmpty (title)) return;
                Dispatcher.invokeSafely (() => titleView.Text = title);
            });
        }

        private void hideTitleClick (object sender, MouseButtonEventArgs e) {
            titleView.Visibility = titleView.Visibility == Visibility.Visible ?
                Visibility.Hidden :
                Visibility.Visible;
        }
        #endregion ------------------------------------------

        //Part for controlling the UIElement status
        //And the process of downloader
        #region ------------- implement IStatusBinder -------------
        public void onPreparing () {
            Dispatcher.invokeSafely (() => {
                downloadCtl.Content = Constant.PreparingText;
                downloadCtl2.Content = waitingSymbol;
                false.able (downloadCtl, downloadCtl2, configViewRoot);
                statusOfLiveView.Content = Constant.PreparingText;
            });
        }

        public void onWaiting () {
            Dispatcher.invokeSafely (() => {
                downloadCtl.Content = Constant.WaitingText;
                downloadCtl2.Content = waitingSymbol;
                false.able (configViewRoot);
                true.able (downloadCtl, downloadCtl2);
                statusOfLiveView.Content = Constant.WaitingText;
            });
        }

        public void onStreaming () {
            Dispatcher.invokeSafely (() => {
                downloadCtl.Content = Constant.StopText;
                downloadCtl2.Content = startedSymbol;
                true.able (downloadCtl, downloadCtl2);
                statusOfLiveView.Content = Constant.RecordingText;
            });
        }

        public void onStopped () {
            Dispatcher.invokeSafely (() => {
                downloadCtl.Content = Constant.StartText;
                downloadCtl2.Content = stoppedSymbol;
                true.able (downloadCtl, downloadCtl2, configViewRoot);
                statusOfLiveView.Content = Constant.RecordStopText;
            });
        }
        #endregion ---------------------------------------------

        //Part for showing some data in UIElements which named end with view
        #region ------------- implement interfaces -------------

        public void onLog (Level level, string message) {
            string info = $"[{level}] {message}\n";
            System.Diagnostics.Debug.WriteLine (message, level.ToString ());
            Dispatcher.invokeSafely (() => {
                if (debugView.Text.Length > 25600) {
                    var text = debugView.Text.Substring (12800);
                    var index2 = text.IndexOf ("\n");
                    var builder = new StringBuilder ();
                    if (index2 > 0) {
                        builder.Append (text.Substring (index2));
                    } else {
                        builder.Append (text);
                    }
                    builder.Append (info);
                    debugView.Clear ();
                    debugView.AppendText (builder.ToString ());
                } else {
                    debugView.AppendText (info);
                }
            });
        }

        public void onStatusUpdate (bool on) {
            string tips = on ? Constant.LiveOnText : Constant.LiveOffText;
            ctx.Fetcher.Logger.log (Level.Info, "Now status is " + tips);
            Dispatcher.invokeSafely (() => { statusOfLiveView.Content = tips; });
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
            Dispatcher.invokeSafely (() => { hotView.Content = popularity; });
            System.Diagnostics.Debug.WriteLine ("Updated : Hot -> " + popularity);
        }

        public void onMissionComplete (IMission mission) { }

        #endregion ---------------------------------------------

    }

    static class Utils {
        public static void runSafely (Action doWhat) {
            try {
                doWhat?.Invoke ();
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine (e.ToString ());
            }
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

}