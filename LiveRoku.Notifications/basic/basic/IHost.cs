namespace LiveRoku.Notifications {
    interface IFloatingHost {
        void show ();
        void close ();
        void putSettingsTo (Base.ISettings settings);
        void addMessage (string tag, string msg);
        void updateTips (TipsType level, string tips);
        void updateSizeText (string text);
        void updateStatus (bool isOn);
        void onClick (System.Action onClick);
    }
}