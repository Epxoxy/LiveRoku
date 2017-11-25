using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using LiveRoku.Notifications.helpers;

namespace LiveRoku.Notifications {
    /// <summary>
    /// Interaction logic for FloatingBox.xaml
    /// </summary>
    public partial class FloatingBox : Window, IFloatingHost {
        public string EasyAccessFolder { get; set; }
        public FloatingBox (Base.ISettings settings) {
            InitializeComponent ();
            timer = TimerWrapper.wrap (() => {
                Dispatcher.Invoke (() => viewMsgBoxState (false));
            }, 5000);
            RoutedEventHandler onLoaded = null;
            onLoaded = (sender, e) => {
                this.Loaded -= onLoaded;
                onLoadedInternal(settings);
            };
            this.Loaded += onLoaded;
        }

        #region EllipseBtn Drag&Click support

        private void onPreMouseLButtonDown (object sender, MouseButtonEventArgs e) {
            isMouseDown = true;
            e.Handled = true;
        }

        private void onPreMouseLButtonUp (object sender, MouseButtonEventArgs e) {
            if (isMouseDown) {
                floatingBtnClick ();
                isMouseDown = false;
            }
            e.Handled = true;
        }

        private void onPreMouseRButtonUp (object sender, MouseButtonEventArgs e) {
            //rightMenuPopup.IsOpen = rightMenuPopup.IsOpen ? false : true;
            e.Handled = true;
        }

        private void onPreMouseMove (object sender, MouseEventArgs e) {
            if (isMouseDown && e.LeftButton == MouseButtonState.Pressed) {
                PopupHelper.dragPopup ();
                isMouseDown = false;
            }
            e.Handled = true;
        }

        private bool isMouseDown;
        #endregion

        public void onStoringSettings (Base.ISettings settings) {
            if (settings == null) return;
            var boxSettings = Dispatcher.Invoke(() => PopupHelper.getUpdatedLocation (mybox));
            var popMsgSettings = Dispatcher.Invoke(() => getNewerSettings (popMsg));
            boxSettings.relativeTo(SystemParameters.WorkArea);
            popMsgSettings.relativeTo(SystemParameters.WorkArea);
            settings.put(Constant.FloatingboxKey, boxSettings);
            settings.put(Constant.FloatingPopMsgKey, popMsgSettings);
        }

        private WidgetSettings getNewerSettings (System.Windows.Controls.Primitives.Popup obj) {
            if (PopupHelper.GetVisible (obj)) {
                return PopupHelper.getUpdatedLocation (obj);
            }
            return PopupHelper.GetSettings (popMsg);
        }

        #region Event subscribe

        private void onLoadedInternal (Base.ISettings settings) {
            this.fadeMsg = FindResource ("fadeMsg") as Storyboard;
            this.fadeMsg.Completed += showNextMessage;
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Dispatcher.ShutdownStarted += saveConfig;
            if (Owner != null)
                Owner.Closing += onOwnerClosing;
            this.mybox.Closed += onUnexpectedlyClosed;
            this.Left = SystemParameters.WorkArea.Width;
            this.Top = 0;
            this.Hide ();
            if (settings != null) {
                var boxSettings = settings.get(Constant.FloatingboxKey, new WidgetSettings());
                var popMsgSettings = settings.get(Constant.FloatingPopMsgKey, new WidgetSettings());
                boxSettings = WidgetSettings.match (boxSettings, SystemParameters.WorkArea);
                popMsgSettings = WidgetSettings.match (popMsgSettings, SystemParameters.WorkArea);
                System.Diagnostics.Debug.WriteLine ($"Loading location {boxSettings.XOffset},{boxSettings.YOffset}");
                System.Diagnostics.Debug.WriteLine ($"Loading location {popMsgSettings.XOffset},{popMsgSettings.YOffset}");
                PopupHelper.SetSettings (mybox, boxSettings);
                PopupHelper.SetSettings (popMsg, popMsgSettings);
            }
            PopupHelper.SetVisible (mybox, true);
        }

        private void onOwnerClosing (object sender, System.ComponentModel.CancelEventArgs e) {
            if(Owner != null)
                Owner.Closing -= onOwnerClosing;
            this.mybox.Closed -= onUnexpectedlyClosed;
            timer?.Dispose ();
        }

        private void onUnexpectedlyClosed (object sender, EventArgs e) {
            if (!this.IsEnabled) return;
            //Keep open on unexpectedly closed
            Task.Run (() => {
                mybox.Dispatcher.Invoke (() => {
                    mybox.IsOpen = true;
                });
            });
        }
        private void saveConfig (object sender, EventArgs e) {
            this.Dispatcher.ShutdownStarted -= saveConfig;
            this.onFloatingBtnClick = null;
        }

        private void backBtnClick (object sender, RoutedEventArgs e) {
            timer.stop ();
            viewMsgBoxState (false);
        }

        private void floatingBtnClick () {
            onFloatingBtnClick?.Invoke ();
        }

        private void dragPopMsgOnMouseLButtonDown (object sender, MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) {
                PopupHelper.dragPopup ();
            }
        }

        #endregion

        #region State control

        private void viewMsgBoxState (bool view) {
            Dispatcher.Invoke (() => {
                System.Diagnostics.Debug.WriteLine ($"UpdateMsgBoxStatus {(view ? Constant.Expand : Constant.Collapsed)}");
                VisualStateManager.GoToElementState (msgBox, view ? Constant.Expand : Constant.Collapsed, true);
                isExpanded = view;
            });
        }

        #endregion

        #region IFloating

        public void setOnClick (Action action) {
            this.onFloatingBtnClick = action;
        }

        public void addMessage (string tag, string msg) {
            timer.restart ();
            Dispatcher.Invoke (() => {
                if (!isExpanded) {
                    tipBlock.Text = msg;
                    viewMsgBoxState (true);
                } else {
                    tipsMsgs.Enqueue (msg);
                    if (tipsMsgs.Count == 1) {
                        showNextMessage (null, null);
                    }
                }
            });
        }

        private void showNextMessage (object sender, EventArgs e) {
            if (sender != null) tipsMsgs.Dequeue ();
            if (tipsMsgs.Count <= 0) return;
            //Prepare next tips
            timer.restart ();
            var remain = tipsMsgs.Count > 1 ? $"{(tipsMsgs.Count - 1)}" : string.Empty;
            msgCountText.Text = remain;
            tipBlock.Text = tipsMsgs.Peek ();
            fadeMsg.Begin ();
        }

        public void updateSizeText (string text) {
            Dispatcher.Invoke (() => {
                floatingBlock.Text = text;
            });
        }

        public void updateLiveStatus (bool isOn) {
            Dispatcher.Invoke (() => floattingBox.BorderBrush = isOn ? Brushes.DeepSkyBlue : Brushes.Orange);
        }

        public void updateTips (TipsType level, string tips = "Notice") {
            Dispatcher.Invoke (() => {
                statusHole.Text = tips;
                //Update visibility
                statusTips.Visibility = level == TipsType.Normal ?
                    Visibility.Collapsed : Visibility.Visible;
                //Get next state
                string stateName = Constant.getText (level);
                if (string.IsNullOrEmpty (stateName)) return;
                VisualStateManager.GoToElementState (statusTips, stateName, true);
            });
        }

        public void show () {
            Dispatcher.Invoke(() => Show ());
        }

        public void close () {
            Dispatcher.Invoke(() => Close());
        }

        public void updateHot(string hotText) {
            //TODO
        }

        public void updateIsRunning(bool isRunning) {
        }

        public void danmakuOnlyModeSetTo(bool isDanmakuOnlyMode) {
        }

        #endregion

        private Storyboard fadeMsg;
        private readonly TimerWrapper timer;
        private bool isExpanded = false;
        private Action onFloatingBtnClick;
        private TextBlock tipBlock => msgText;
        private FrameworkElement box => msgBox;
        private Queue<string> tipsMsgs = new Queue<string> ();

    }
}