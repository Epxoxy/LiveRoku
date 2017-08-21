namespace LiveRoku.Base {
    public interface IPlugin {
        void onInitialize(IStorage storage);
        void onAttach (ILiveDownloader downloader);
        void onDetach ();
    }
}