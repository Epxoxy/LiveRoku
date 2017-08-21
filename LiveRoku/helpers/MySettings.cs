using System;
using System.Collections.Generic;

namespace LiveRoku {
    public class MySettings {
        public List<int> RoomIds { get; set; } = new List<int> ();
        public int LastRoomId { get; set; }
        public string DownloadFolder { get; set; } = Environment.GetFolderPath (Environment.SpecialFolder.Desktop);
        public string DownloadFileFormat { get; set; } = "{roomId}-{Y}-{M}-{d}-{H}-{m}-{s}.flv";
        public bool AutoStart { get; set; } = true;
        public bool DownloadDanmaku { get; set; } = true;

        public void addLastRoomId (int roomId) {
            LastRoomId = roomId;
            if (RoomIds == null)
                RoomIds = new List<int> ();
            if (!RoomIds.Contains (roomId))
                RoomIds.Add (roomId);
        }
    }
}