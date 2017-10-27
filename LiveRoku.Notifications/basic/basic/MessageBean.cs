namespace LiveRoku.Notifications {
    internal class MessageBean {
        public string Tag { get; set; }
        public string Content { get; set; }
        public string Extra { get; set; }

        public MessageBean () { }
        public MessageBean (string tag, string content) {
            this.Tag = tag;
            this.Content = content;
        }
        public override string ToString () {
            return Tag + Content;
        }
    }
}