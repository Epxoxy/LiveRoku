using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace LiveRoku.Notifications {
    internal class FlowAnimationWrapper {
        private volatile bool animating;
        private ScrollViewer host;
        private Storyboard animated;
        private DoubleKeyFrame fromFrame;
        private DoubleKeyFrame toFrame;
        private TimeSpan slowTime;
        private TimeSpan normalTime;
        private IList src;
        private object locker = new object ();
        private bool isEnabled = true;
        private int maxVisible = 7;
        private System.Timers.Timer timer;

        public FlowAnimationWrapper (ScrollViewer host, IList src) {
            this.host = host;
            this.src = src;
            setup ();
        }

        private void setup () {
            this.animated = new Storyboard ();
            this.slowTime = TimeSpan.FromSeconds (0.5);
            this.normalTime = TimeSpan.FromSeconds (0.25);
            this.fromFrame = new DiscreteDoubleKeyFrame { KeyTime = TimeSpan.Zero };
            this.toFrame = new EasingDoubleKeyFrame {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                KeyTime = slowTime
            };
            var timeline = new DoubleAnimationUsingKeyFrames ();
            timeline.KeyFrames.Add (fromFrame); 
            timeline.KeyFrames.Add(toFrame);
            animated.Children.Add (timeline);
            animated.Completed += animatedGoOn;
            ScrollBinder.SetTarget (host, host);
            Storyboard.SetTarget (animated, this.host);
            Storyboard.SetTargetProperty (animated, new PropertyPath (ScrollBinder.OffsetProperty));
            timer = new System.Timers.Timer(5000) {
                AutoReset = true,
            };
            timer.Elapsed += isTimeToRemove;
        }


        public void raiseAnimated () {
            if (!isEnabled) return;
            if (src.Count > maxVisible) {
                lock (locker) {
                    if (animating) return;
                    animating = true;
                    timer.Enabled = false;
                }
                var scrollable = host.ScrollableHeight - host.VerticalOffset;
                var times = (scrollable / 40);
                var offset = (times < maxVisible ? Math.Max (1, times) : 7) * 40;
                fromFrame.Value = this.host.VerticalOffset;
                toFrame.Value = fromFrame.Value + offset;
                //toFrame.KeyTime = times < maxVisible ? slowTime : normalTime;
                animated.Begin ();
            } else {
                if (!animating && src.Count > 0) {
                    timer.Stop();
                    timer.Start();
                }
            }
        }

        private void isTimeToRemove(object sender, System.Timers.ElapsedEventArgs e) {
            if (animating || src.Count <= 0)
                return;
            host.Dispatcher.Invoke(() => this.src.RemoveAt(0));
        }

        public void setIsEnabled (bool isEnabled) {
            this.isEnabled = isEnabled;
            if (!isEnabled) {
                timer.Stop();
            }
        }

        private void animatedGoOn (object sender, EventArgs e) {
            lock (locker) {
                animating = false;
                if (this.host.IsScrolledToBottom (5)) {
                    removeRange (this.src, this.src.Count - maxVisible);
                }
            }
            raiseAnimated ();
        }

        private void fastRemovePartOf (IList list) {
            if (list.Count > 1000) {
                removeRange (list, list.Count / 2);
            } else if (list.Count > 1000) {
                removeRange (list, list.Count / 3);
            } else if (list.Count > 100) {
                removeRange (list, list.Count / 4);
            } else if (list.Count > 20) {
                list.RemoveAt (0);
            }
        }

        private void removeRange (IList list, int size) {
            for (int i = 0; i < size; i++) {
                list.RemoveAt (0);
            }
        }
    }

}