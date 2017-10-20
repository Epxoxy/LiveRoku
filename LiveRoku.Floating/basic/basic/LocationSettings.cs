using System;

namespace LiveRoku.Floating {
    [Serializable]
    public class LocationSettings {
        public double WorkAreaWidth { get; set; }
        public double WorkAreaHeight { get; set; }
        public double XOffset { get; set; }
        public double YOffset { get; set; }
        public bool OnTop { get; set; }
        public bool Lock { get; set; }

        public LocationSettings () { }

        public LocationSettings (double x, double y, bool onTop) {
            XOffset = x;
            YOffset = y;
            OnTop = onTop;
        }

        public static LocationSettings getValid (LocationSettings settings, System.Windows.Rect range) {
            if (settings.WorkAreaWidth != range.Width) {
                if (settings.WorkAreaWidth <= 0)
                    settings.WorkAreaWidth = range.Width;
                settings.XOffset = settings.XOffset * range.Width / settings.WorkAreaWidth;
                settings.WorkAreaWidth = range.Width;
            }
            if (settings.WorkAreaHeight != range.Height) {
                if (settings.WorkAreaHeight <= 0)
                    settings.WorkAreaHeight = range.Height;
                settings.YOffset = settings.YOffset * range.Height / settings.WorkAreaHeight;
                settings.WorkAreaHeight = range.Height;
            }
            if (settings.XOffset > settings.WorkAreaWidth) {
                settings.XOffset = settings.WorkAreaWidth;
            }
            if (settings.YOffset > settings.WorkAreaHeight) {
                settings.YOffset = settings.WorkAreaHeight;
            }
            return settings;
        }
    }
}