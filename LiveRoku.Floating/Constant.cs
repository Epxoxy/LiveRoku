using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveRoku.Notifications {
    public class Constant {
        public const string MessageFlowBoxKey = "msgflowbox";
        public const string MessageFlowBoxExtraKey = "msgflowbox-extra";
        public const string FloatingboxKey = "floatingbox";
        public const string PopMsgKey = "popMsg";
        public const string MsgFlowCheckedKey = "msgFlowChecked";
        public const string MaxRecentKey = "maxRecent";
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