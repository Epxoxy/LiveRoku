using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace LiveRoku.UI.controls {
    public class PlaceHolder : DependencyObject {
        public static object GetContent (DependencyObject obj) {
            return (object) obj.GetValue (ContentProperty);
        }
        public static void SetContent (DependencyObject obj, object value) {
            obj.SetValue (ContentProperty, value);
        }

        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.RegisterAttached ("Content", typeof (object), typeof (PlaceHolder), new PropertyMetadata (null));

        public static Brush GetBrush (DependencyObject obj) {
            return (Brush) obj.GetValue (BrushProperty);
        }
        public static void SetBrush (DependencyObject obj, Brush value) {
            obj.SetValue (BrushProperty, value);
        }

        public static readonly DependencyProperty BrushProperty =
            DependencyProperty.RegisterAttached ("Brush", typeof (Brush), typeof (PlaceHolder), new PropertyMetadata (null));

    }
}