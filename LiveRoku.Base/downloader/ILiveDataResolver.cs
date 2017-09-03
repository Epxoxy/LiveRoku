namespace LiveRoku.Base{
    public interface ILiveDataResolver {
        void onLiveStatusUpdate (LiveStatus status);
        void onDurationUpdate (long duration, string timeText);
        void onDownloadSizeUpdate (long totalSize, string sizeText);
        void onBitRateUpdate (long bitRate, string bitRateText);
        void onHotUpdate (long popularity);
    }
}
