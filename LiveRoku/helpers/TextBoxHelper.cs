using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LiveRoku {
    public class TextBoxHelper {
        public static readonly DependencyProperty IsClearButtonProperty =
            DependencyProperty.RegisterAttached ("IsClearButton", typeof (bool), typeof (TextBoxHelper), new PropertyMetadata (false, setEventHandler));

        public static bool GetIsClearButton (DependencyObject obj) {
            return (bool) obj.GetValue (IsClearButtonProperty);
        }
        public static void SetIsClearButton (DependencyObject obj, bool value) {
            obj.SetValue (IsClearButtonProperty, value);
        }

        public static TextBox GetClearTarget (DependencyObject obj) {
            return (TextBox) obj.GetValue (ClearTargetProperty);
        }

        public static void SetClearTarget (DependencyObject obj, TextBox value) {
            obj.SetValue (ClearTargetProperty, value);
        }

        public static readonly DependencyProperty ClearTargetProperty =
            DependencyProperty.RegisterAttached ("ClearTarget", typeof (TextBox), typeof (TextBoxHelper), new PropertyMetadata (null));

        private static void setEventHandler (DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var btn = d as Button;
            if (d != null && e.OldValue != e.NewValue) {
                btn.Click -= clearText;
                if ((bool) e.NewValue) {
                    btn.Click += clearText;
                }
            }
        }

        private static void clearText (object sender, RoutedEventArgs e) {
            var btn = sender as Button;
            if (btn != null) {
                TextBox box = GetClearTarget (btn);
                if (box != null) {
                    box.Clear ();
                } else {
                    var parent = VisualTreeHelper.GetParent (btn);
                    while (parent != null && (box = parent as TextBox) == null) {
                        parent = VisualTreeHelper.GetParent (parent);
                    }
                    if (box != null) box.Clear ();
                }
            }
        }
    }
}