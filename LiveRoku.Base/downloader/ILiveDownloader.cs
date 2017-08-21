namespace LiveRoku.Base {
    public delegate void DanmakuResolver(DanmakuModel danmaku);
    //To implements ILiveDownloader must have a default constructor as
    //public Constructor (IRequestModel model, string userAgent, int requestTimeout);
    public interface ILiveDownloader : IDownloader{
        LowList<ILiveDataResolver> LiveDataResolvers { get; }
        LowList<IStatusBinder> StatusBinders { get; }
        LowList<DanmakuResolver> DanmakuResolvers { get; }
        LowList<ILogger> Loggers { get; }
    }
}