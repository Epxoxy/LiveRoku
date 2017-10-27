using System;

namespace LiveRoku.Notifications {
    [Serializable]
    public class WidgetSettings {
        public double XOffset { get; set; }
        public double YOffset { get; set; }
        public double XRelative { get; set; }
        public double YRelative { get; set; }
        public bool OnTop { get; set; }
        public bool Lock { get; set; }
        public bool RelativeFirst { get; set; } = true;

        public WidgetSettings () { }

        public WidgetSettings (double x, double y, bool onTop) {
            XOffset = x;
            YOffset = y;
            OnTop = onTop;
        }
        
        public void relativeTo(double width, double height) {
            this.XRelative = XOffset / width;
            this.YRelative = YOffset / height;
        }

        public void relativeTo(System.Windows.Rect range) => relativeTo(range.Width, range.Height);

        public static WidgetSettings match (WidgetSettings settings, System.Windows.Rect range) {
            var xRelativeOffset = range.Width * settings.XRelative;
            var yRelativeOffset = range.Height * settings.YRelative;
            settings.XOffset = Math.Abs(settings.XOffset - xRelativeOffset) < 2 ? settings.XOffset : xRelativeOffset;
            settings.YOffset = Math.Abs(settings.YOffset - yRelativeOffset) < 2 ? settings.YOffset : yRelativeOffset;
            return settings;
        }
    }
}