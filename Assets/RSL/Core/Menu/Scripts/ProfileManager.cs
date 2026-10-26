using System;
using System.Collections.Generic;
using RosMessageTypes.Std;
using RSL.Core.Robots;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UvgRos;
using UvgRos.TF2;

namespace RSL.Core.Menu
{
    #if UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(ProfileManager))]
    public class ProfileManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ProfileManager myScript = (ProfileManager)target;
            if (GUILayout.Button("Save Profile"))
            {
                myScript.SaveProfile();
            }
            if (GUILayout.Button("Clear Settings"))
            {
                myScript.ClearSettings();
            }
        }
    }
    #endif

    /// <summary>
    /// Palm-menu controller for named connection profiles: Save captures the
    /// current IP/port, viewer layout, robot and link settings under a name;
    /// picking one from the dropdown reconnects and restores all of it.
    /// Storage is ConnectionProfileStore.
    /// </summary>
    public class ProfileManager : MonoBehaviour
    {
        public ROSManager rosManager;
        public Dropdown profileDropdown;
        public TMP_InputField nameInput;
        /// <summary>Optional: the server-side uvgROS profile (see ServerProfiles).</summary>
        public Dropdown serverProfileDropdown;
        /// <summary>Optional: one line of feedback ("saved ...", "loaded ...").</summary>
        public TextMeshProUGUI statusText;

        // Index 0 of profileDropdown is a prompt, not a profile, so opening the
        // list and closing it again never reconnects anywhere.
        const string LoadPrompt = "Load profile...";
        // Mirrors ros/profiles/*.yaml on the server. "" leaves the server alone.
        static readonly string[] ServerProfiles = { "", "lossless", "reliable", "balanced", "lowlatency", "compressed" };
        const string ServerProfileLabelPrefix = "link: ";
        const string ServerProfileDefaultLabel = "server default";
        const string TuneTopic = "/uvgrtp_ros/tune";
        // The tune topic is fire-and-forget over UDP, so a request goes out
        // more than once; applying the same global profile again is a no-op.
        const int TuneRepeats = 2;
        const float TuneInterval = 0.5f;

        List<ConnectionProfile> _profiles = new List<ConnectionProfile>();
        string _serverProfile = "";
        UvgRosConnection _ros;

        string _tuneJson;
        int _tuneSendsLeft;
        float _nextTuneAt;
        UvgRosPublisher<StringMsg> _tunePublisher;

        void Start()
        {
            _ros = UvgRosConnection.GetOrCreateInstance();
            // From prefs, not rosManager.CurrentPort: its Start may not have run yet.
            if (rosManager != null && int.TryParse(rosManager.defaultPort, out int defaultPort))
                ConnectionProfileStore.MigrateLegacyIps(PlayerPrefs.GetInt("port", defaultPort));

            if (serverProfileDropdown != null)
            {
                var labels = new List<string>();
                foreach (string p in ServerProfiles) labels.Add(ServerProfileLabelPrefix + (p.Length == 0 ? ServerProfileDefaultLabel : p));
                serverProfileDropdown.ClearOptions();
                serverProfileDropdown.AddOptions(labels);
                serverProfileDropdown.onValueChanged.AddListener(i => SetServerProfile(ServerProfiles[i]));
            }
            if (profileDropdown != null)
                profileDropdown.onValueChanged.AddListener(OnProfileSelected);

            RefreshList();
            if (nameInput != null) nameInput.text = ConnectionProfileStore.LastUsed;
        }

        void Update()
        {
            // Sent from here rather than from RunWhenConnected: that can run on
            // the reconnect thread, and Publish/Send belong on the main thread.
            if (_tuneSendsLeft <= 0 || _ros.HasConnectionError || Time.time < _nextTuneAt) return;
            try
            {
                if (_tunePublisher == null) _tunePublisher = _ros.Publish<StringMsg>(TuneTopic);
                _tunePublisher.Send(new StringMsg(_tuneJson));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Profiles] could not send {_tuneJson} on {TuneTopic}: {e.Message}");
                _tunePublisher = null;
            }
            _tuneSendsLeft--;
            _nextTuneAt = Time.time + TuneInterval;
        }

        /// <summary>Re-reads the profile file into the dropdown.</summary>
        public void RefreshList()
        {
            _profiles = ConnectionProfileStore.LoadAll();
            if (profileDropdown == null) return;
            var options = new List<string> { LoadPrompt };
            foreach (var p in _profiles) options.Add(p.Describe());
            profileDropdown.ClearOptions();
            profileDropdown.AddOptions(options);
            profileDropdown.SetValueWithoutNotify(0);
        }

        void OnProfileSelected(int index)
        {
            if (index <= 0 || index > _profiles.Count) return;
            LoadProfile(_profiles[index - 1].name);
            profileDropdown.SetValueWithoutNotify(0);
        }

        /// <summary>Saves the current setup under the name field, or "ip:port" when it is empty.</summary>
        public void SaveProfile()
        {
            string name = nameInput != null ? nameInput.text.Trim() : "";
            if (name.Length == 0) name = $"{CurrentIp()}:{CurrentPort()}";
            var profile = Capture(name);
            try
            {
                ConnectionProfileStore.Upsert(profile);
            }
            catch (Exception e)
            {
                SetStatus($"save failed: {e.Message}");
                Debug.LogError($"[Profiles] could not write {ConnectionProfileStore.FilePath}: {e}");
                return;
            }
            ConnectionProfileStore.LastUsed = name;
            if (nameInput != null) nameInput.text = name;
            RefreshList();
            SetStatus($"saved {name}");
            Debug.Log($"[Profiles] saved '{name}' ({profile.layout.Count} viewer group(s)) to {ConnectionProfileStore.FilePath}");
        }

        /// <summary>Deletes the profile named in the name field.</summary>
        public void DeleteProfile()
        {
            string name = nameInput != null ? nameInput.text.Trim() : "";
            if (name.Length == 0) return;
            bool removed = ConnectionProfileStore.Delete(name);
            RefreshList();
            SetStatus(removed ? $"deleted {name}" : $"no profile {name}");
        }

        /// <summary>Reconnects and restores a saved profile by name.</summary>
        public void LoadProfile(string name)
        {
            var profile = ConnectionProfileStore.Find(ConnectionProfileStore.LoadAll(), name);
            if (profile == null)
            {
                SetStatus($"no profile {name}");
                return;
            }
            Apply(profile);
            ConnectionProfileStore.LastUsed = profile.name;
            if (nameInput != null) nameInput.text = profile.name;
            SetStatus($"loaded {profile.name}");
            Debug.Log($"[Profiles] loaded '{profile.name}' -> {profile.ip}:{profile.port}");
        }

        /// <summary>Loads the profile named in the name field (also reachable without the dropdown).</summary>
        public void LoadNamedProfile()
        {
            if (nameInput != null && nameInput.text.Trim().Length > 0) LoadProfile(nameInput.text.Trim());
        }

        /// <summary>
        /// Switches every route on the server to a ros/profiles/*.yaml profile
        /// (the tune topic's {"global_profile": name}). Server-wide, so it
        /// changes other clients' links too; "" keeps the server as it is.
        /// </summary>
        public void SetServerProfile(string profileName)
        {
            _serverProfile = profileName ?? "";
            if (_serverProfile.Length == 0) return;
            _tuneJson = $"{{\"global_profile\": \"{_serverProfile}\"}}";
            // A fresh route every time: an explicit reconnect (loading a profile
            // does one) clears the old handle and Send then drops silently.
            _tunePublisher = null;
            _tuneSendsLeft = TuneRepeats;
            _nextTuneAt = Time.time + TuneInterval;
        }

        public void ClearSettings()
        {
            PlayerPrefs.DeleteAll();
        }

        ConnectionProfile Capture(string name)
        {
            var profile = new ConnectionProfile
            {
                name = name,
                ip = CurrentIp(),
                port = CurrentPort(),
                savedAt = DateTime.Now.ToString("o"),
            };

            foreach (var manager in FindObjectsByType<SensorManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                // A manager whose menu was never opened has not run Awake: its
                // layout is still the one sitting in its prefs key.
                string json = manager.IsAwake ? manager.Serialize() : PlayerPrefs.GetString(manager.LayoutPrefsKey, "");
                if (json.Length > 0) profile.layout.Add(new LayoutEntry { manager = manager.name, json = json });
            }

            var models = ModelManager.instance;
            if (models != null)
            {
                profile.robot.modelIndex = models.robotDatabase.robots.IndexOf(models.currentRobot);
                profile.robot.rootFrame = models.rootFrame != null ? models.rootFrame.text : "";
            }

            profile.network.localHost = _ros.LocalHost ?? "";
            profile.network.autoReconnect = _ros.AutoReconnect;
            profile.network.useSimTime = TF2Stream.UseSimTime;
            profile.network.serverProfile = _serverProfile;
            return profile;
        }

        void Apply(ConnectionProfile profile)
        {
            // Link settings first: LocalHost and AutoReconnect are read by Connect().
            var net = profile.network ?? new NetworkSettings();
            // Already live on this server: keep the connection. A reconnect
            // here only costs a gap, and the server tears down the old
            // connection's sessions after the new one has opened routes under
            // the same peer id, which leaves the restored viewers without data.
            bool sameServer = !_ros.HasConnectionError && CurrentIp() == profile.ip &&
                              CurrentPort() == profile.port && (_ros.LocalHost ?? "") == (net.localHost ?? "");
            _ros.LocalHost = net.localHost ?? "";
            _ros.AutoReconnect = net.autoReconnect;
            TF2Stream.UseSimTime = net.useSimTime;
            PlayerPrefs.SetInt("use_sim_time", net.useSimTime ? 1 : 0);
            foreach (var label in FindObjectsByType<UseSimTime>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                label.SyncLabel();

            if (!sameServer)
            {
                if (rosManager != null) rosManager.SetConnection(profile.ip, profile.port);
                else _ros.Connect(profile.ip, profile.port);
            }

            ApplyRobot(profile.robot);

            // Every manager is set, not only the ones in the profile: a viewer
            // group the profile did not have should end up empty, not keep
            // whatever the previous profile left open.
            foreach (var manager in FindObjectsByType<SensorManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var entry = profile.layout.Find(l => l.manager == manager.name);
                bool hasLayout = entry != null && !string.IsNullOrEmpty(entry.json);
                // Written to the prefs key too, so a manager that wakes up later
                // (or the next launch) starts from this profile's layout.
                if (hasLayout) PlayerPrefs.SetString(manager.LayoutPrefsKey, entry.json);
                else PlayerPrefs.DeleteKey(manager.LayoutPrefsKey);
                if (!manager.IsAwake) continue;
                if (hasLayout) manager.Deserialize(entry.json);
                else manager.ClearAll();
            }

            if (serverProfileDropdown != null)
                serverProfileDropdown.SetValueWithoutNotify(Math.Max(0, Array.IndexOf(ServerProfiles, net.serverProfile ?? "")));
            SetServerProfile(net.serverProfile);
            PlayerPrefs.Save();
        }

        static void ApplyRobot(RobotSettings robot)
        {
            var models = ModelManager.instance;
            if (models == null || robot == null) return;
            var robots = models.robotDatabase.robots;
            if (robot.modelIndex >= 0 && robot.modelIndex < robots.Count &&
                robot.modelIndex != robots.IndexOf(models.currentRobot))
            {
                // Through the dropdown so its label follows; its listener calls ChangeModel.
                if (models.robotDropdown != null) models.robotDropdown.value = robot.modelIndex;
                else models.ChangeModel(robot.modelIndex);
            }
            // After ChangeModel, which resets the root frame to the model's default.
            if (!string.IsNullOrEmpty(robot.rootFrame))
            {
                if (models.rootFrame != null) models.rootFrame.SetTextWithoutNotify(robot.rootFrame);
                models.ChangeRootFrame(robot.rootFrame);
            }
        }

        string CurrentIp() => rosManager != null ? rosManager.CurrentIP : _ros.RosIPAddress;
        int CurrentPort() => rosManager != null ? rosManager.CurrentPort : _ros.RosPort;

        void SetStatus(string text)
        {
            if (statusText != null) statusText.text = text;
        }
    }
}
