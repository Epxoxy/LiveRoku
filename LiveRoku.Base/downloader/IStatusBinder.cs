namespace LiveRoku.Base {
    public interface IStatusBinder {
        string Name { get; }
        void onPreparing ();
        void onStreaming ();
        void onWaiting();
        void onStopped ();
    }
}