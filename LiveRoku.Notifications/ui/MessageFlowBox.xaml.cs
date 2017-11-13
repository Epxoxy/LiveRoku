using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LiveRoku.Notifications.helpers;
using System.Windows.Threading;
using System.Threading;

namespace LiveRoku.Notifications {
    /// <summary>
    /// Interaction logic for MessageFlowBox.xaml
    /// </summary>
    public partial class MessageFlowBox : Window, IFloatingHost {
        private FlowAnimationWrapper animator;
        private ScrollViewer recentHost;
        private MessageWrapper<MessageBean> msgHost;

        public MessageFlowBox (Base.ISettings settings) {
            InitializeComponent ();
            subscribeEvent(settings);
        }

        public void putSettingsTo (Base.ISettings settings) {
            if (settings == null) return;
            var location = Dispatcher.Invoke(() => {
                return PopupHelper.getUpdatedLocation(popbox);
            });
            var flowChecked = Dispatcher.Invoke(() => flowToggle.IsChecked == true);
            location.relativeTo(SystemParameters.WorkArea);
            settings.put(Constant.MessageFlowBoxKey, location);
            settings.put(Constant.MsgFlowCheckedKey, flowChecked);
            settings.put(Constant.MaxRecentKey, msgHost.MaxRecentSize);
        }

        private void subscribeEvent(Base.ISettings settings) {
            RoutedEventHandler onLoaded = null;
            onLoaded = (sender, e) => {
                this.Loaded -= onLoaded;
                if (Owner != null) Owner.Closing += onOwnerClosing;
                this.Top = 0;
                this.Hide();
                setPopup(settings);
                setFlow(settings);
            };
            this.Loaded += onLoaded;
        }

        private void setPopup(Base.ISettings settings) {
            popbox.Closed += reopenOnUnexpectedlyClosed;
            //get settings from storage
            //get extra from storage
            var location = settings.get(Constant.MessageFlowBoxKey, new WidgetSettings());
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
            var maxRecent = settings.get(Constant.MaxRecentKey, 60);
            msgHost = new MessageWrapper<MessageBean> (msgFlowList, recentMsgList, msg => {
                return new MessageBean (msg.Tag, msg.Content) { Extra = $"[{DateTime.Now.ToString("HH:mm:ss fff")}]" };
            }, Math.Max(maxRecent, 60));
            msgHost.onMessageAddedDo (() => animator.raiseAnimated ());
            //must set it after because of flowToggle control flowItems's visibility
            //which may case flowItems.GetVisualChild<T> return null
            var enabled = settings.get(Constant.MsgFlowCheckedKey, true);
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

        //TODO REMOVE IT
        //test function,remove both from xaml
        private int count = 0;
        private void addClick (object sender, RoutedEventArgs e) {
            addMessage ("Test", "am " + count++);
        }

        //TODO REMOVE IT
        //test function,remove both from xaml
        private void removeClick (object sender, RoutedEventArgs e) {
            if (msgHost.FlowMsgs.Count > 0) {
                msgHost.FlowMsgs.RemoveAt (0);
            }
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

        public void updateStatus (bool isOn) {
            invokeSafely(Dispatcher, () => this.statusBlock.Text = isOn ? "ON" : "OFF");
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

        public void onClick (Action onClick) { }

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

        private void copyToClipboard(object sender, MouseButtonEventArgs e) {
            if(e.ClickCount >= 2) {
                System.Windows.Documents.Run item = null;
                if ((item = sender as System.Windows.Documents.Run) != null) {
                    Clipboard.SetText(item.Text);
                }
            }
        }
    }
}