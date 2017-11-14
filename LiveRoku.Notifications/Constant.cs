namespace LiveRoku.Notifications {
    public class Constant {
        //FlowBox
        public const string FlowBoxKey = "msgflowbox";
        public const string FlowBoxExtraKey = "msgflowbox-extra";
        public const string FlowBoxModeKey = "msgflowbox-in-bottom";
        public const string FlowBoxOpenKey = "msgFlowChecked";
        public const string FlowBoxMaxRecentKey = "maxRecent";
        //Floating
        public const string FloatingboxKey = "floatingbox";
        public const string FloatingPopMsgKey = "popMsg";
        public const string Expand = "Expand";
        public const string Collapsed = "Collapsed";
        public const string Blue = "Blue";
        public const string Yellow = "Yellow";
        public const string Normal = "Normal";

        public static string getText (TipsType type) {
            switch (type) {
                case TipsType.Normal:
                    return Constant.Normal;
                case TipsType.Yellow:
                    return Constant.Yellow;
                case TipsType.Blue:
                    return Constant.Blue;
                default:
                    return string.Empty;
            }
        }
    }
}