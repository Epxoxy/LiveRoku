namespace LiveRoku.Base{
    public interface ILiveDataResolver {
        void onStatusUpdate (bool on);
        void onDurationUpdate (long duration, string timeText);
        void onDownloadSizeUpdate (long totalSize, string sizeText);
        void onBitRateUpdate (long bitRate, string bitRateText);
        void onHotUpdate (long popularity);
    }
}
