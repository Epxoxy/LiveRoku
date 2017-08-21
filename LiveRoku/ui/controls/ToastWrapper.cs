using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace LiveRoku.UI {

    internal class ToastWrapper {
        public ItemsControl Host { get; private set; }
        private BindingList<ToastItem> source;
        private Remover<ToastItem> remover;
        public ToastWrapper (ItemsControl host, int interval) {
            source = new BindingList<ToastItem> ();
            Host = host;
            host.ItemsSource = source;
            //Basic init
            remover = new Remover<ToastItem> (item => {
                source.Add (item);
            }, item => {
                host.Dispatcher.Invoke (() => { source.Remove (item); });
            }, interval);
        }

        public void add (string tag, string log) {
            this.add (new ToastItem (tag, log));
        }
        public void add (ToastItem item) {
            remover.addItem (item);
        }
    }

    public class ToastItem {
        public string Tag { get; set; }
        public string Text { get; set; }
        public ToastItem () { }
        public ToastItem (string tag, string text) {
            this.Tag = tag;
            this.Text = text;
        }
    }

    public class Remover<T> {
        public bool IsRunning { get; private set; }
        private Queue<T> items = new Queue<T> ();
        private Action<T> remove;
        private Action<T> add;
        private readonly int interval;

        public Remover (Action<T> add, Action<T> remove, int interval) {
            this.remove = remove;
            this.add = add;
            this.interval = interval;
        }

        public void addItem (T value) {
            add.Invoke (value);
            items.Enqueue (value);
            ensureTask ();
        }

        private void ensureTask () {
            if (IsRunning) return;
            IsRunning = true;
            Task.Run (async () => {
                while (items.Count > 0) {
                    T value = items.Dequeue ();
                    await Task.Delay (interval);
                    remove.Invoke (value);
                }
                IsRunning = false;
            });
        }

    }

}