using System.Windows;
using System.Windows.Controls;

namespace LiveRoku.Notifications {
    public class ScrollBinder {

        public static ScrollViewer GetTarget (DependencyObject obj) {
            return (ScrollViewer) obj.GetValue (TargetProperty);
        }
        public static void SetTarget (DependencyObject obj, ScrollViewer value) {
            obj.SetValue (TargetProperty, value);
        }

        public static double GetOffset (DependencyObject obj) {
            return (double) obj.GetValue (OffsetProperty);
        }
        public static void SetOffset (DependencyObject obj, double value) {
            obj.SetValue (OffsetProperty, value);
        }

        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.RegisterAttached ("Target", typeof (ScrollViewer), typeof (ScrollBinder), new PropertyMetadata (null));
        public static readonly DependencyProperty OffsetProperty =
            DependencyProperty.RegisterAttached ("Offset", typeof (double), typeof (ScrollBinder), new PropertyMetadata (0d, onOffsetChanged));

        private static void onOffsetChanged (DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var target = GetTarget (d);
            if (target != null) {
                var newv = (double) e.NewValue;
                if (target.VerticalOffset == newv) return;
                target.ScrollToVerticalOffset (newv);
            }
        }
    }
}