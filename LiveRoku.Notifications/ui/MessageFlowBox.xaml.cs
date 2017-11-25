using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LiveRoku.Notifications.helpers;
using System.Windows.Threading;
using System.Threading;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows.Controls.Primitives;

namespace LiveRoku.Notifications {
    /// <summary>
    /// Interaction logic for MessageFlowBox.xaml
    /// </summary>
    public partial class MessageFlowBox : Window, IFloatingHost {
        public string EasyAccessFolder { get; set; }
        private FlowAnimationWrapper animator;
        private ScrollViewer recentHost;
        private MessageWrapper<MessageBean> msgHost;
        private PathGeometry tvNormal;
        private PathGeometry tvDisable;
        //private List<ToggleButton> mutexToggle;

        public MessageFlowBox (Base.ISettings settings) {
            InitializeComponent ();
            subscribeEvent(settings);
        }

        public void onStoringSettings (Base.ISettings settings) {
            if (settings == null) return;
            var location = Dispatcher.Invoke(() => {
                return PopupHelper.getUpdatedLocation(popbox);
            });
            var flowChecked = Dispatcher.Invoke(() => flowToggle.IsChecked == true);
            location.relativeTo(SystemParameters.WorkArea);
            settings.put(Constant.FlowBoxKey, location);
            settings.put(Constant.FlowBoxOpenKey, flowChecked);
            settings.put(Constant.FlowBoxMaxRecentKey, msgHost.MaxRecentSize);
        }

        private void subscribeEvent(Base.ISettings settings) {
            RoutedEventHandler onLoaded = null;
            onLoaded = (sender, e) => {
                this.Loaded -= onLoaded;
                if (Owner != null) Owner.Closing += onOwnerClosing;
                this.Top = 0;
                this.Hide();
                tvNormal = TryFindResource("tv-normal") as PathGeometry;
                tvDisable = TryFindResource("tv-disable") as PathGeometry;
                var mode = settings.get(Constant.FlowBoxModeKey, false);
                toDisplayMode(mode);
                settings.put(Constant.FlowBoxModeKey, mode);
                setPopup(settings);
                setFlow(settings);
                /*mutexToggle = new List<ToggleButton>(new ToggleButton[] {
                    dloadDetailToggle,
                    recentToggle,
                    prefToggle
                });
                foreach(var toggle in mutexToggle) {
                    toggle.Checked += onWhoChecked;
                }*/
            };
            this.Loaded += onLoaded;
        }

        /*private void onWhoChecked(object sender, RoutedEventArgs e) {
            foreach(var toggle in mutexToggle) {
                if (toggle == sender)
                    continue;
                toggle.IsChecked = false;
            }
        }*/

        private void setPopup(Base.ISettings settings) {
            popbox.Closed += reopenOnUnexpectedlyClosed;
            //get settings from storage
            //get extra from storage
            var location = settings.get(Constant.FlowBoxKey, new WidgetSettings());
            location = WidgetSettings.match(location, SystemParameters.WorkArea);
            System.Diagnostics.Debug.WriteLine($"Loading location {location.XOffset},{location.YOffset}");
            //set popup
            PopupHelper.SetSettings(popbox, location);
            PopupHelper.SetVisible(popbox, true);
        }
        
        private void setFlow (Base.ISettings settings) {
            //set flow message source & recent message source
            var msgFlowList = new BindingList<MessageBean> ();
            var recentMsgList = new BindingList<MessageBean> ();
            flowItems.ItemsSource = msgFlowList; 
            recentItems.ItemsSource = recentMsgList;
            animator = new FlowAnimationWrapper (flowItems.GetVisualChild<ScrollViewer> (), msgFlowList);
            //set message host
            var maxRecent = settings.get(Constant.FlowBoxMaxRecentKey, 60);
            msgHost = new MessageWrapper<MessageBean> (msgFlowList, recentMsgList, msg => {
                return new MessageBean (msg.Tag, msg.Content) { Extra = $"[{DateTime.Now.ToString("HH:mm:ss fff")}]" };
            }, Math.Max(maxRecent, 60));
            msgHost.onMessageAddedDo (() => animator.raiseAnimated ());
            //must set it after because of flowToggle control flowItems's visibility
            //which may case flowItems.GetVisualChild<T> return null
            var enabled = settings.get(Constant.FlowBoxOpenKey, true);
            flowToggle.IsChecked = (bool)enabled;
            onFlowEnableChanged((bool)enabled);
        }

        private void reopenOnUnexpectedlyClosed (object sender, EventArgs e) {
            if (!this.IsEnabled) return;
            //Keep open on unexpectedly closed
            Task.Run (() => {
                invokeSafely(popbox.Dispatcher, () => {
                    var location = PopupHelper.GetSettings(popbox);
                    popbox.IsOpen = true;
                    PopupHelper.setPopupLocation(popbox, (int)location.XOffset, (int)location.YOffset);
                    PopupHelper.SetSettings(popbox, location);
                });
            });
        }

        private void onOwnerClosing (object sender, CancelEventArgs e) {
            if(Owner!= null)
                Owner.Closing -= onOwnerClosing;
            this.popbox.Closed -= reopenOnUnexpectedlyClosed;
        }
        
        //interface part
        public void show () {
            invokeSafely(Dispatcher, () => Show());
        }

        public void close () {
            invokeSafely(Dispatcher, ()=> Close ());
        }
        
        public void addMessage (string tag, string msg) {
            invokeSafely(Dispatcher, () => {
                var alreayBottom = false;
                if (msgHost.RecentEnabled) {
                    recentHost = recentHost ?? recentItems.GetVisualChild<ScrollViewer>();
                    alreayBottom = recentHost != null && recentHost.IsScrolledToBottom(2);
                }
                msgHost.addMessage(new MessageBean(tag, msg));
                if (alreayBottom) {
                    recentHost.ScrollToBottom();
                }
            });
        }

        public void updateLiveStatus (bool isOn) {
            invokeSafely(Dispatcher, () => {
                this.tvSymbol.Stroke = isOn ? Brushes.Orange : Brushes.LightGray;
            });
        }

        public void updateIsRunning(bool isRunning) {
            invokeSafely(Dispatcher, () => {
                this.tvSymbol.Data = isRunning ? tvNormal : tvDisable;
                if (!isRunning) {
                    this.tvSymbol.Stroke = Brushes.PaleVioletRed;
                }
            });
        }

        public void updateHot(string hotText) {
            invokeSafely(Dispatcher, () => hotBlock.Text = hotText);
        }

        public void updateTips (TipsType level, string tips) {
            invokeSafely(Dispatcher, () => {
                //Get next state
                string stateName = Constant.getText (level);
                if (string.IsNullOrEmpty (stateName)) return;
                VisualStateManager.GoToElementState (statusFill, stateName, true);
            });
        }

        public void updateSizeText (string text) {
            invokeSafely(Dispatcher, () => { sizeBlock.Text = text; });
        }

        public void danmakuOnlyModeSetTo(bool isDanmakuOnlyMode) {
            invokeSafely(Dispatcher, () => {
                dloadPane.Visibility = isDanmakuOnlyMode ? Visibility.Collapsed : Visibility.Visible;
            });
        }

        public void setOnClick (Action onClick) { }

        //set layout
        private void toDisplayMode(bool isHeadBottom) {
            VerticalAlignment align = VerticalAlignment.Bottom;
            if (isHeadBottom) {
                DockPanel.SetDock(head, Dock.Bottom);
                DockPanel.SetDock(extend, Dock.Top);
                extend.Height = flowItems.MaxHeight;
            } else {
                DockPanel.SetDock(head, Dock.Top);
                DockPanel.SetDock(extend, Dock.Bottom);
                align = VerticalAlignment.Top;
                extend.Height = Double.NaN;
            }
            flowItems.VerticalAlignment = align;
            recentItems.VerticalAlignment = align;
        }

        //event handlers start
        private void dragMoveClick (object sender, MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) {
                PopupHelper.dragPopup ();
                PopupHelper.getUpdatedLocation(popbox);
                e.Handled = true;
            }
        }
        
        private void clearFlowMessage (object sender, RoutedEventArgs e) {
            msgHost.FlowMsgs.Clear ();
        }

        private void showHideOwnerClick (object sender, RoutedEventArgs e) {
            reverseVisibility (this.Owner);
        }

        private void reverseVisibility (Window win) {
            if (win == null) return;
            var toVisible = win.Visibility != Visibility.Visible;
            win.Visibility = toVisible ? Visibility.Visible : Visibility.Hidden;
            if (toVisible) win.Activate ();
        }

        private void disableFlowMessage (object sender, RoutedEventArgs e) {
            onFlowEnableChanged (false);
        }

        private void enableFlowMessage (object sender, RoutedEventArgs e) {
            onFlowEnableChanged (true);
        }

        private void onFlowEnableChanged (bool enabled) {
            msgHost?.setEnable (enabled);
            animator?.setIsEnabled (enabled);
        }

        private void invokeSafely (Dispatcher dispatcher, Action action) {
            if (Thread.CurrentThread == dispatcher.Thread) {
                action.Invoke();
            } else dispatcher.BeginInvoke (DispatcherPriority.Normal, action);
        }

        private void copyTagToClipboard(object sender, MouseButtonEventArgs e) {
            if(e.ClickCount >= 2) {
                FrameworkContentElement item = null;
                if ((item = sender as FrameworkContentElement) != null
                    && item.Tag != null && item.Tag is string) {
                    Clipboard.SetText((string)item.Tag);
                }
            }
        }

        private void openAppOrStoreFolder(object sender, RoutedEventArgs e) {
            if (string.IsNullOrEmpty(EasyAccessFolder) || !System.IO.Directory.Exists(EasyAccessFolder)) {
                EasyAccessFolder = AppDomain.CurrentDomain.BaseDirectory;
            }
            try {
                System.Diagnostics.Process.Start(EasyAccessFolder);
            } catch { }
        }

    }
}