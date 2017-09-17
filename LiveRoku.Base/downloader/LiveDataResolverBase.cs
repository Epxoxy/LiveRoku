namespace LiveRoku.Base {
    public class LiveDataResolverBase : ILiveDataResolver {
        public virtual void onBitRateUpdate (long bitRate, string bitRateText) { }

        public virtual void onDownloadSizeUpdate (long totalSize, string sizeText) { }

        public virtual void onDurationUpdate (long duration, string timeText) { }

        public virtual void onHotUpdate (long popularity) { }

        public virtual void onStatusUpdate (bool on) { }
    }
}