namespace LiveRoku.Notifications {
    interface IFloatingHost {
        string EasyAccessFolder { get; set; }
        void show ();
        void close ();
        void onStoringSettings (Base.ISettings settings);
        void addMessage (string tag, string msg);
        void updateTips (TipsType level, string tips);
        void updateSizeText (string text);
        void updateLiveStatus (bool isOn);
        void updateHot(string hotText);
        void setOnClick (System.Action onClick);
        void updateIsRunning(bool isRunning);
        void danmakuOnlyModeSetTo(bool isDanmakuOnlyMode);
    }
}