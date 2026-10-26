// One saved setup: where to connect, what was on screen, and the link settings
// that go with that network. Plain [Serializable] fields only, so JsonUtility
// can write the whole list to a single file (see ConnectionProfileStore).
using System;
using System.Collections.Generic;

namespace RSL.Core.Menu
{
    /// <summary>One SensorManager's saved viewers, in its own Serialize() format.</summary>
    [Serializable]
    public class LayoutEntry
    {
        public string manager;
        public string json;
    }

    /// <summary>Which robot model is shown and the frame it hangs from.</summary>
    [Serializable]
    public class RobotSettings
    {
        public int modelIndex = -1;
        public string rootFrame = "";
    }

    /// <summary>
    /// Link settings that belong to a network rather than to a layout: the
    /// local interface to bind (empty = detect), whether to reconnect on loss,
    /// sim time, and the server-side uvgROS profile (empty = leave the server
    /// as it is, since that one changes every client's routes).
    /// </summary>
    [Serializable]
    public class NetworkSettings
    {
        public string localHost = "";
        public bool autoReconnect = true;
        public bool useSimTime;
        public string serverProfile = "";
    }

    [Serializable]
    public class ConnectionProfile
    {
        public string name;
        public string ip;
        public int port;
        public List<LayoutEntry> layout = new List<LayoutEntry>();
        public RobotSettings robot = new RobotSettings();
        public NetworkSettings network = new NetworkSettings();
        // Round-trip "o" format, so the file stays readable and sortable.
        public string savedAt = "";

        public string Describe() => $"{name} ({ip}:{port})";
    }
}
