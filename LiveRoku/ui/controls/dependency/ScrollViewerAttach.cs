using System.Windows;

namespace LiveRoku.UI.controls {
    public class ScrollViewerAttach {
        public static double GetHorizontalBarLength (DependencyObject obj) {
            return (double) obj.GetValue (HorizontalBarLengthProperty);
        }
        public static void SetHorizontalBarLength (DependencyObject obj, double value) {
            obj.SetValue (HorizontalBarLengthProperty, value);
        }
        public static readonly DependencyProperty HorizontalBarLengthProperty =
            DependencyProperty.RegisterAttached ("HorizontalBarLength", typeof (double), typeof (ScrollViewerAttach), new PropertyMetadata (10d));

        public static double GetVerticalBarLength (DependencyObject obj) {
            return (double) obj.GetValue (VerticalBarLengthProperty);
        }
        public static void SetVerticalBarLength (DependencyObject obj, double value) {
            obj.SetValue (VerticalBarLengthProperty, value);
        }
        public static readonly DependencyProperty VerticalBarLengthProperty =
            DependencyProperty.RegisterAttached ("VerticalBarLength", typeof (double), typeof (ScrollViewerAttach), new PropertyMetadata (10d));

    }
}