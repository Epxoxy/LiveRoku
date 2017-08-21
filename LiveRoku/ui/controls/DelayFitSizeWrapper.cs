using System;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;

namespace LiveRoku.UI.controls {
    [ContentProperty ("Content")]
    public class DelayFitSizePane : System.Windows.Controls.ContentControl {
        public bool IsNarrow {
            get { return (bool) GetValue (IsNarrowProperty); }
            private set { SetValue (IsNarrowProperty, value); }
        }
        public static readonly DependencyProperty IsNarrowProperty =
            DependencyProperty.Register ("IsNarrow", typeof (bool), typeof (DelayFitSizePane), new PropertyMetadata (true));

        public double NarrowWidth {
            get { return (double) GetValue (NarrowWidthProperty); }
            set { SetValue (NarrowWidthProperty, value); }
        }
        public static readonly DependencyProperty NarrowWidthProperty =
            DependencyProperty.Register ("NarrowWidth", typeof (double), typeof (DelayFitSizePane),
                new PropertyMetadata (480d, onNarrowWidthChanged));

        public event RoutedEventHandler ToNarrow {
            add { this.AddHandler (ToNarrowEvent, value); }
            remove { this.RemoveHandler (ToNarrowEvent, value); }
        }
        public static readonly RoutedEvent ToNarrowEvent = EventManager.RegisterRoutedEvent ("ToNarrow",
            RoutingStrategy.Bubble, typeof (RoutedEventHandler), typeof (DelayFitSizePane));

        public event RoutedEventHandler ToWide {
            add { this.AddHandler (ToWideEvent, value); }
            remove { this.RemoveHandler (ToWideEvent, value); }
        }
        public static readonly RoutedEvent ToWideEvent = EventManager.RegisterRoutedEvent ("ToWide",
            RoutingStrategy.Bubble, typeof (RoutedEventHandler), typeof (DelayFitSizePane));

        private static void onNarrowWidthChanged (DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var view = d as DelayFitSizePane;
            view?.updateNarrowWidth ((double) e.NewValue);
        }

        static DelayFitSizePane () {
            DefaultStyleKeyProperty.OverrideMetadata (typeof (DelayFitSizePane),
                new FrameworkPropertyMetadata (typeof (DelayFitSizePane)));
        }

        public DelayFitSizePane () {
            this.Loaded += OnThisLoaded;
        }

        private void OnThisLoaded (object sender, RoutedEventArgs e) {
            this.Loaded -= OnThisLoaded;
            this.Unloaded += OnThisUnloaded;
        }

        private void OnThisUnloaded (object sender, RoutedEventArgs e) {
            this.Unloaded -= OnThisUnloaded;
            EnsureReleaseTimer ();
        }

        public FrameworkElement wrapper;
        public FrameworkElement wrappingElement;
        public override void OnApplyTemplate () {
            EnsureReleaseTimer ();
            if (wrapper != null) wrapper.SizeChanged -= OnWrapperSizeChanged;
            base.OnApplyTemplate ();
            wrapper = GetTemplateChild ("PART_WRAPPER") as FrameworkElement;
            wrappingElement = GetTemplateChild ("PART_CONTENT") as FrameworkElement;
            if (wrapper != null && wrappingElement != null) {
                wrapper.SizeChanged += OnWrapperSizeChanged;
            }
        }

        private void updateNarrowWidth (double newer) {
            narrowWidth = newer;
            updateMode (newerSize.Width <= narrowWidth);
        }

        private void updateMode (bool newerIsNarrow) {
            if (newerIsNarrow == IsNarrow) return;
            IsNarrow = newerIsNarrow;
            RaiseEvent (new RoutedEventArgs (IsNarrow ? ToNarrowEvent : ToWideEvent));
        }

        private void OnWrapperSizeChanged (object sender, SizeChangedEventArgs e) {
            newerSize = e.NewSize;
            if (delayTimer == null) {
                delayTimer = new DispatcherTimer ();
                delayTimer.Interval = TimeSpan.FromMilliseconds (Delay);
                delayTimer.Tick += OnTimerTick;
            } else delayTimer.Stop ();
            delayTimer.Start ();
        }

        private void OnTimerTick (object sender, EventArgs e) {
            if (delayTimer != null) delayTimer.Stop ();
            wrappingElement.Height = newerSize.Height;
            wrappingElement.Width = newerSize.Width;
            updateMode (newerSize.Width <= narrowWidth);
        }

        private void EnsureReleaseTimer () {
            if (delayTimer != null) {
                var temp = delayTimer;
                delayTimer = null;
                temp.Tick -= OnTimerTick;
                temp.Stop ();
            }
        }

        private DispatcherTimer delayTimer { get; set; }
        public int Delay {
            get { return delay; }
            set {
                if (delay != value) {
                    delay = value;
                }
            }
        }
        private int delay = 30;
        private double narrowWidth = 480;
        public Size newerSize = new Size ();
    }
}