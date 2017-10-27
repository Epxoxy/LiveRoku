using System;
using System.Collections.Generic;

namespace LiveRoku.Notifications {
    internal class MessageWrapper<T> {
        public bool RecentEnabled { get; private set; } = true;
        public bool FlowEnabled { get; private set; } = true;
        public int MaxRecentSize { get; private set; }
        public IList<T> FlowMsgs => flowMsgs;
        public IList<T> RecentMsgs => recentMsgs;

        private readonly Action doNothing = () => { };
        private Action msgAdded;
        private Action recentAdded;
        private Func<T, T> formatRecent;

        private IList<T> flowMsgs;
        private IList<T> recentMsgs;
        public MessageWrapper (IList<T> flowMsgs, IList<T> recentMsgs, Func<T, T> formatRecent, int maxRecentSize) {
            this.flowMsgs = flowMsgs;
            this.recentMsgs = recentMsgs;
            this.formatRecent = formatRecent;
            this.MaxRecentSize = maxRecentSize;
            this.recentAdded = doNothing;
            this.msgAdded = doNothing;
        }

        public void onMessageAddedDo (Action action) {
            msgAdded = action ?? doNothing;
        }
        public void onRecentAddedDo (Action action) {
            recentAdded = action ?? doNothing;
        }

        public void addMessage (T msg) {
            if (FlowEnabled) {
                flowMsgs.Add (msg);
                msgAdded.Invoke ();
            }
            if (!RecentEnabled) return;
            recentMsgs.Add (formatRecent.Invoke (msg));
            if (recentMsgs.Count > MaxRecentSize) {
                recentMsgs.RemoveAt (0);
            }
            recentAdded.Invoke ();
        }


        public void setEnable (bool enabled, bool clearOnDisabled = true) {
            this.FlowEnabled = enabled;
            if (!enabled && clearOnDisabled) {
                flowMsgs.Clear ();
            }
        }
    }
}