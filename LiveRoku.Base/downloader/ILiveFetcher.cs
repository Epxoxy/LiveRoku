namespace LiveRoku.Base {
    //To implements ILiveFetcher must have a default constructor as
    //public Constructor (IRequestModel model, string userAgent, int requestTimeout);
    public interface ILiveFetcher : IDownloader, System.IDisposable {
        LowList<ILiveProgressBinder> LiveProgressBinders { get; }
        LowList<IStatusBinder> StatusBinders { get; }
        LowList<DanmakuResolver> DanmakuHandlers { get; }
        ILogger Logger { get; }
        bool IsStreaming { get; }

        RoomInfo fetchRoomInfo(bool refresh);
        //For extension
        void setExtra(string key, object value);
        object getExtra(string key);
    }
}