using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LiveRoku.Floating.helpers;

namespace LiveRoku.Floating {
    /// <summary>
    /// Interaction logic for MessageFlowBox.xaml
    /// </summary>
    public partial class MessageFlowBox : Window, IHost {
        private Base.IStorage storage;
        private FlowAnimationWrapper animator;
        private ScrollViewer recentHost;
        private MessageWrapper<MessageBean> msgHost;
        private Dictionary<string, object> extra;

        public MessageFlowBox (Base.IStorage storage) {
            this.storage = storage;
            InitializeComponent ();
            this.Loaded += onLoaded;
        }

        private void onLoaded (object sender, RoutedEventArgs e) {
            this.Loaded -= onLoaded;
            this.Top = 0;
            this.Hide ();
            this.Owner.Closing += onOwnerClosing;
            this.popbox.Closed += reopenOnUnexpectedlyClosed;
            //get settings from storage
            //get extra from storage
            LocationSettings settings = null;
            storage?.tryGet (Constant.MessageFlowBoxKey, out settings);
            storage?.tryGet (Constant.MessageFlowBoxExtraKey, out extra);
            settings = LocationSettings.getValid (settings ?? new LocationSettings (), SystemParameters.WorkArea);
            extra = extra ?? new Dictionary<string, object> ();
            System.Diagnostics.Debug.WriteLine ($"Loading location {settings.XOffset},{settings.YOffset}");
            //set popup
            PopupHelper.SetSettings (popbox, settings);
            PopupHelper.SetVisible (popbox, true);
            //set flow message source & recent message source
            var flowMsgs = new BindingList<MessageBean> ();
            var recentMsgs = new BindingList<MessageBean> ();
            flowItems.ItemsSource = flowMsgs;
            recentItems.ItemsSource = recentMsgs;
            animator = new FlowAnimationWrapper (flowItems.GetVisualChild<ScrollViewer> (), flowMsgs);
            //set message host
            msgHost = new MessageWrapper<MessageBean> (flowMsgs, recentMsgs, msg => {
                return new MessageBean (msg.Tag, msg.Content) { Extra = $"[{DateTime.Now.ToString("HH:mm:ss fff")}]" };
            }, 60);
            msgHost.onMessageAddedDo (() => animator.raiseAnimated ());
            //must set it after because of flowToggle control flowItems's visibility
            //which may case flowItems.GetVisualChild<T> return null
            if (extra.TryGetValue (Constant.MsgFlowCheckedKey, out object enabled) &&
                enabled != null && enabled is bool) {
                flowToggle.IsChecked = (bool) enabled;
                onFlowEnableChanged ((bool) enabled);
            } else extra.Add (Constant.MsgFlowCheckedKey, true);
        }

        private void reopenOnUnexpectedlyClosed (object sender, EventArgs e) {
            if (!this.IsEnabled) return;
            //Keep open on unexpectedly closed
            Task.Run (() => {
                popbox.Dispatcher.Invoke (() => {
                    popbox.IsOpen = true;
                });
            });
        }

        private void onOwnerClosing (object sender, CancelEventArgs e) {
            this.Owner.Closing -= onOwnerClosing;
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
            this.Show ();
        }

        public void close () {
            this.Close ();
        }

        public void saveSettings () {
            if (storage == null) return;
            var settings = PopupHelper.getUpdatedLocation (popbox);
            extra[Constant.MsgFlowCheckedKey] = flowToggle.IsChecked == true;
            storage.add (Constant.MessageFlowBoxKey, settings);
            storage.add (Constant.MessageFlowBoxExtraKey, extra);
            storage.save ();
        }

        public void addMessage (string tag, string msg) {
            Dispatcher.Invoke (() => {
                var alreayBottom = false;
                if (msgHost.RecentEnabled) {
                    recentHost = recentHost ?? recentItems.GetVisualChild<ScrollViewer> ();
                    alreayBottom = recentHost != null && recentHost.IsScrolledToBottom (2);
                }
                msgHost.addMessage (new MessageBean (tag, msg));
                if (alreayBottom) {
                    recentHost.ScrollToBottom ();
                }
            });
        }

        public void updateStatus (bool isOn) {
            Dispatcher.Invoke (() => this.statusEle.Text = isOn ? "ON" : "OFF");
        }

        public void updateTips (TipsType level, string tips) {
            Dispatcher.Invoke (() => {
                //Get next state
                string stateName = Constant.getText (level);
                if (string.IsNullOrEmpty (stateName)) return;
                VisualStateManager.GoToElementState (statusFill, stateName, true);
            });
        }

        public void updateSizeText (string text) {
            Dispatcher.Invoke (() => { sizeTb.Text = text; });
        }

        public void onClick (Action onClick) { }

        //event handlers start
        private void dragMoveClick (object sender, MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) {
                PopupHelper.dragPopup ();
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
    }
}