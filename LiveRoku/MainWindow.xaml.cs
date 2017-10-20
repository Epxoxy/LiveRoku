﻿using System;
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
using System.Threading.Tasks;
using System.Text;

namespace LiveRoku {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, ILogHandler, IFetchSettings, ILiveProgressBinder, IStatusBinder {
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
        public string RoomId => Dispatcher.invokeSafely(() => roomIdBox.Text);
        public string Folder => Dispatcher.invokeSafely(() => settings.DownloadFolder);
        public string FileFormat => Dispatcher.invokeSafely(() => settings.DownloadFileFormat);
        public bool DownloadDanmaku => Dispatcher.invokeSafely(() => saveDanmaku.IsChecked == true);
        public bool AutoStart => Dispatcher.invokeSafely(() => autoStart.IsChecked == true);

        //Helpers
        private MySettings settings;
        private IStorage storage;
        private List<IPlugin> plugins;
        private ILiveFetcher fetcher;

        public MainWindow () {
            InitializeComponent ();
            this.Loaded += onLoaded;
        }

        private void onLoaded (object sender, RoutedEventArgs e) {
            this.Loaded -= onLoaded;
            //Load basic resources
            stoppedSymbol = FindResource(Constant.PauseSymbolKey) as UIElement;
            startedSymbol = FindResource(Constant.RightSymbolKey) as UIElement;
            waitingSymbol = FindResource(Constant.LoadingSymbolKey) as UIElement;
            string loadError = null;
            bool coreLoaded = tryInitCore(ref fetcher, ref loadError, App.coreFolder, App.instance, this, "");
            //Load plugins first, safely load all assembly
            //Let storage deserialize method can find assembly right.
            if (coreLoaded) {
                plugins = PluginLoader.LoadInstances<IPlugin>(App.pluginFolder, App.instance);
            } else {
                Task.Run(() => { MessageBox.Show(loadError, "Error"); });
            }
            //Subscribe basic events
            Dispatcher.invokeSafely(() => purgeEvents(resubscribe: true, justBasicEvent: !coreLoaded));
            //Get settings
            storage = Storage.StorageHelper.instance(App.dataFolder);
            findSettings();
            //Load plugins
            if (coreLoaded) {
                for(int i = 0; i < plugins.Count; i++) {
                    var typeName = plugins[i].GetType().Name;
                    if (!settings.Plugins.ContainsKey(typeName)) {
                        settings.Plugins.Add(typeName, true);
                    }else if (!settings.Plugins[typeName]) {
                        plugins.Remove(plugins[i]);
                        System.Diagnostics.Debug.WriteLine("remove " + typeName);
                    } else {
                        settings.Plugins[typeName] = true;
                    }
                }
                if (settings.Extras != null && settings.Extras.Count > 0) {
                    foreach (var key in settings.Extras.Keys) {
                        fetcher.putExtra(key, settings.Extras[key]);
                    }
                }
                plugins.forEachSafely(p => {
                    p.onInitialize(storage);
                    fetcher.Logger.log(Level.Info, $"{p.GetType().Name} loaded.");
                });
                //Generate helpers
                //downloader = new LiveDownloader(this, "");
                fetcher.Logger.LogHandlers.add(this);
                fetcher.LiveProgressBinders.add(this);
                fetcher.StatusBinders.add(this);
                plugins.forEachSafely(p => p.onAttach(fetcher));
            }
        }

        protected override void OnClosing (CancelEventArgs e) {
            purgeEvents ();
            fetcher?.Dispose();
            plugins?.forEachSafely(p => p.onDetach());
            saveSettings();
            base.OnClosing (e);
        }

        private bool tryInitCore(ref ILiveFetcher fetcher, ref string error, string folder, IAssemblyCaches assembly, IFetchSettings settings, string userAgent) {
            //Load core dll
            var types = PluginLoader.LoadTypesListImpl<ILiveFetcher> (folder, assembly);
            try {
                object instance;
                if (types == null || types.Count() <= 0 ||
                   (instance = Activator.CreateInstance(types.First(), settings, userAgent)) == null ||
                   (fetcher = instance as ILiveFetcher) == null) {
                    error = types == null ? "Core dll not exist." : "Load core dll fail.";
                }
                else return true;
            }
            catch(Exception ex) {
                ex.printStackTrace();
                error = "Load core error, msg : " + ex.Message;
            }
            return false;
        }

        private void findSettings () {
            //Load settings
            if (!storage.tryGet<MySettings> ("settings", out settings)) {
                settings = new MySettings ();
            }
            Dispatcher.invokeSafely(() => {
                roomIdBox.Text = settings.LastRoomId.ToString();
                saveDanmaku.IsChecked = settings.DownloadDanmaku;
                autoStart.IsChecked = settings.AutoStart;
                locationBox.Text = System.IO.Path.Combine(Folder, FileFormat);
            });
        }

        private void saveSettings () {
            if (settings == null || storage == null) return;
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
        private void purgeEvents (bool resubscribe = false, bool justBasicEvent = false) {
            exploreFolder.MouseLeftButtonUp -= explore;
            exploreFolder2.MouseLeftButtonUp -= explore;
            aboutLink.MouseLeftButtonUp -= showAbout;
            editPathBtn.Click -= setLocation;
            ctlBtn01.Click -= startOrStop;
            ctlBtn02.Click -= startOrStop;
            if (resubscribe) {
                exploreFolder.MouseLeftButtonUp += explore;
                exploreFolder2.MouseLeftButtonUp += explore;
                aboutLink.MouseLeftButtonUp += showAbout;
                editPathBtn.Click += setLocation;
                if (!justBasicEvent) {
                    ctlBtn01.Click += startOrStop;
                    ctlBtn02.Click += startOrStop;
                }
            }
        }

        private void scrollEnd (object sender, TextChangedEventArgs e) {
            (sender as TextBoxBase)?.ScrollToEnd();
        }

        private void startOrStop (object sender, RoutedEventArgs e) {
            if (fetcher == null) return;
            if (fetcher.IsRunning)
                fetcher.stop ();
            else fetcher.start ();
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

        private void updateTitleClick(object sender, RoutedEventArgs e) {
            Task.Run(() => {
                var title = fetcher.fetchRoomInfo(true)?.Title;
                if (string.IsNullOrEmpty(title)) return;
                Dispatcher.invokeSafely(() => titleView.Text = title);
            });
        }

        private void hideTitleClick(object sender, MouseButtonEventArgs e) {
            titleView.Visibility = titleView.Visibility == Visibility.Visible 
                ? Visibility.Hidden 
                : Visibility.Visible;
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
                statusOfLiveView.Content = Constant.PreparingText;
            });
        }

        public void onWaiting () {
            Dispatcher.invokeSafely (() => {
                ctlBtn01.Content = Constant.WaitingText;
                ctlBtn02.Content = waitingSymbol;
                false.able (configViewRoot);
                true.able (ctlBtn01, ctlBtn02);
                statusOfLiveView.Content = Constant.WaitingText;
            });
        }

        public void onStreaming () {
            Dispatcher.invokeSafely (() => {
                ctlBtn01.Content = Constant.StopText;
                ctlBtn02.Content = startedSymbol;
                true.able (ctlBtn01, ctlBtn02);
                statusOfLiveView.Content = Constant.RecordingText;
            });
        }

        public void onStopped () {
            Dispatcher.invokeSafely (() => {
                ctlBtn01.Content = Constant.StartText;
                ctlBtn02.Content = stoppedSymbol;
                true.able (ctlBtn01, ctlBtn02, configViewRoot);
                statusOfLiveView.Content = Constant.RecordStopText;
            });
        }
        #endregion ---------------------------------------------

        //Part for showing some data in UIElements which named end with view
        #region ------------- implement interfaces -------------
        
        public void onLog(Level level, string message) {
            string info = $"[{level}] {message}\n";
            System.Diagnostics.Debug.WriteLine (message, level.ToString());
            Dispatcher.invokeSafely (() => {
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
                    debugView.AppendText (info); 
                }
            });
        }

        public void onStatusUpdate(bool on) {
            string tips = on ? Constant.LiveOnText : Constant.LiveOffText;
            fetcher.Logger.log(Level.Info, "Now status is " + tips);
            Dispatcher.invokeSafely(() => { statusOfLiveView.Content = tips; });
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

        public void onMissionComplete(IMission mission) {
        }

        #endregion ---------------------------------------------

    }

    static class Utils {

        public static void forEachSafely<T>(this List<T> list, Action<T> action) {
            list.ForEach(o => {
                try {
                    action.Invoke(o);
                } catch (Exception e) {
                    e.printStackTrace();
                }
            });
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

        public static TResult invokeSafely<TResult>(this Dispatcher dispatcher, Func<TResult> func)
        {
            if (Thread.CurrentThread == dispatcher.Thread)
            {
                return func();
            }
            else return dispatcher.Invoke<TResult>(func, DispatcherPriority.Normal);
        }
        
    }

}