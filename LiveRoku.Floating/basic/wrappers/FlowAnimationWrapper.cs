using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace LiveRoku.Notifications {
    internal class FlowAnimationWrapper {
        private CancellationTokenSource cts;
        private volatile bool animating;
        private volatile bool removing;
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
        }

        public void raiseAnimated () {
            if (!isEnabled) return;
            if (src.Count > maxVisible) {
                lock (locker) {
                    if (animating) return;
                    removing = false;
                    animating = true;
                }
                var scrollable = host.ScrollableHeight - host.VerticalOffset;
                var times = (scrollable / 40);
                var offset = (times < maxVisible ? Math.Max (1, times) : 7) * 40;
                fromFrame.Value = this.host.VerticalOffset;
                toFrame.Value = fromFrame.Value + offset;
                //toFrame.KeyTime = times < maxVisible ? slowTime : normalTime;
                animated.Begin ();
            } else {
                if (removing) return;
                removing = true;
                if (cts != null && cts.Token.CanBeCanceled) {
                    cts.Cancel ();
                }
                cts = new CancellationTokenSource ();
                Task.Run (async () => {
                    while (!animating && removing && src.Count > 0) {
                        await Task.Delay (5000, cts.Token);
                        if (animating || src.Count <= 0) break;
                        host.Dispatcher.Invoke (() => this.src.RemoveAt (0));
                    }
                    removing = false;
                }, cts.Token);
            }
        }

        public void setIsEnabled (bool isEnabled) {
            this.isEnabled = isEnabled;
            if (!isEnabled && cts != null && cts.Token.CanBeCanceled) {
                cts.Cancel ();
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