namespace LiveRoku.Base {
    public delegate void DanmakuResolver(DanmakuModel danmaku);
    public interface ILiveDownloader : IDownloader{
        LowList<ILiveDataResolver> LiveDataResolvers { get; }
        LowList<IStatusBinder> StatusBinders { get; }
        LowList<DanmakuResolver> DanmakuResolvers { get; }
        LowList<ILogger> Loggers { get; }
    }
}