namespace LiveRoku.Floating {
    interface IHost {
        void show ();
        void close ();
        void saveSettings ();
        void addMessage (string tag, string msg);
        void updateTips (TipsType level, string tips);
        void updateSizeText (string text);
        void updateStatus (bool isOn);
        void onClick (System.Action onClick);
    }
}