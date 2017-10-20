using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace LiveRoku.Floating {
    public static class Extends {
        public static bool IsScrolledToBottom (this IScrollInfo scrollInfo, double offset = 0) {
            return scrollInfo.VerticalOffset + scrollInfo.ViewportHeight + offset >= scrollInfo.ExtentHeight;
        }

        public static bool IsScrolledToBottom (this ScrollViewer viewer, double offset = 0) {
            return viewer.VerticalOffset + viewer.ViewportHeight + offset >= viewer.ExtentHeight;
        }

        public static bool IsScrolledToBottom (this TextBoxBase textbox, double offset = 0) {
            return textbox.VerticalOffset + textbox.ViewportHeight + offset >= textbox.ExtentHeight;
        }

    }
}