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
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Windows.Data;
using System.Globalization;
using LiveRoku.UI;
using LiveRoku.Loader;

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
            bridge.setupContext(coreLoaded => {
                Dispatcher.Invoke(() => {
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
            bridge.switchStartAndStop();
        }

        private void setLocation (object sender, RoutedEventArgs e) {
            var dialog = new LocationSettings () {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Folder = bridge.StoreFolder,
                FileName = bridge.StoreFileNameFormat
            };
            if (dialog.ShowDialog () == true) {
                bridge.StoreFolder = dialog.Folder;
                bridge.StoreFileNameFormat = dialog.FileName;
                bridge.VideoFullPathFormat = System.IO.Path.Combine (bridge.StoreFolder, bridge.StoreFileNameFormat);
            }
        }

        private void explore (object sender, MouseButtonEventArgs e) {
            System.Diagnostics.Process.Start (bridge.StoreFolder);
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
    
    public class ProcessStateToValueConverter : IValueConverter {
        public object StoppedValue { get; set; }
        public object PreparingValue { get; set; }
        public object WaitingValue { get; set; }
        public object StreamingValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is ProcessState) {
                switch ((ProcessState)value) {
                    case ProcessState.Stopped:
                        return StoppedValue;
                    case ProcessState.Preparing:
                        return PreparingValue;
                    case ProcessState.Waiting:
                        return WaitingValue;
                    case ProcessState.Streaming:
                        return StreamingValue;
                }
            }
            return StoppedValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
    
}