using System;
using System.Collections.Generic;

namespace LiveRoku {
    public class MySettings {
        public List<int> RoomIds { get; set; } = new List<int> ();
        public int LastRoomId { get; set; }
        public string DownloadFolder { get; set; } = Environment.GetFolderPath (Environment.SpecialFolder.Desktop);
        public string DownloadFileFormat { get; set; } = "{roomId}-{Y}-{M}-{d}-{H}-{m}-{s}.flv";
        public bool AutoStart { get; set; } = true;
        public bool DanmakuRequire { get; set; } = true;
        public bool VideoRequire { get; set; } = true;
        public Dictionary<string, object> Extras { get; set; } = new Dictionary<string, object> ();
        public Dictionary<string, bool> Plugins { get; set; } = new Dictionary<string, bool> ();

        public void addLastRoomId (int roomId) {
            LastRoomId = roomId;
            if (RoomIds == null)
                RoomIds = new List<int> ();
            if (!RoomIds.Contains (roomId))
                RoomIds.Add (roomId);
        }
    }
}