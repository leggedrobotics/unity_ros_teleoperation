// UI Toolkit controller for ConnectionSettings.uxml -- the port of the uGUI
// palmmenu's Wifi sub-menu (palmmenu > Wifi > WifiMenu). Tracing that prefab
// puts all of these under it: the IP field, the port field, the connection
// dropdown, "Connected Gloves", "Enable Pose Publishing", and the
// "Pose Publish Decimator" slider (a MinMaxSlider prefab instance). They live
// here and NOT on GeneralSettings for that reason.
//
// DEVIATIONS FROM THE uGUI VERSION, all requested:
//  * IP and port are no longer edited in place on the panel. They belong to a
//    named profile, edited in the New/Edit panel that grows out of the right
//    edge (WifiEntry.uxml). Saving a profile makes it active and connects it.
//  * The title-row control is a refresh BUTTON, not a status dot: it still
//    reports connection state through its icon tint, but clicking it
//    reconnects. The old dot only reported.
//
// TEXT INPUT: the IP, port and name fields are driven by the platform system
// keyboard (the Quest keyboard on device) via SystemKeyboardField -- read that
// file's header for why the panel drives it explicitly rather than relying on
// UI Toolkit's implicit mobile-keyboard path.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Robotics.ROSTCPConnector;
using RSL.Core.Haptics;
using RSL.Telemetry.Headset;
using RSL.Telemetry.Pose;

namespace RSL.Core.Menu
{
    public class ConnectionSettingsPanel : UIToolkitPanel
    {
        [Tooltip("The New/Edit profile panel (WifiEntry.uxml). Instantiated into " +
                 "EntryHost; Add and Edit are disabled without it.")]
        public VisualTreeAsset profileEntryTemplate;

        [Tooltip("How often (seconds) to re-read glove connection state and ROS " +
                 "connection status. Both are polled because neither exposes a " +
                 "change event.")]
        public float statusRefreshInterval = 0.5f;

        // -1 means the open entry panel is creating a new profile rather than
        // editing an existing index.
        private const int NewProfileIndex = -1;

        private ROSConnection _ros;
        // The menu's actual connection manager. palmmenu wired its IP field to
        // ROSManager.OnIPDone, its port field to OnPortDone, and its Add/Delete
        // buttons to SaveIP/DeleteIP -- so everything here goes through it rather
        // than poking ROSConnection directly. It also raises OnConnectionColor /
        // OnConnectionStagnant, which StatusIndicator and MenuManager listen to;
        // bypassing it left those consumers to notice changes only by polling.
        // Optional: it is absent from a UI-Toolkit-only scene (its Start()
        // dereferences uGUI fields), so every use falls back to ROSConnection.
        private ROSManager _rosManager;
        private HandManager _hands;
        private HeadsetPublisher _headset;
        private PosePublisher _posePublisher;

        private DropdownField _profileDropdown;
        private Button _refresh;
        private Button _editButton;
        private Button _deleteButton;
        private Label _statusText;
        private VisualElement _entryHost;
        private VisualElement _leftGlove;
        private VisualElement _rightGlove;

        private readonly List<SystemKeyboardField> _entryFields = new List<SystemKeyboardField>();
        private List<ConnectionProfile> _profiles = new List<ConnectionProfile>();
        private int _editingIndex = NewProfileIndex;
        private float _nextStatusRefresh;

        protected override void Bind(VisualElement root)
        {
            // GetOrCreateInstance rather than a scene lookup: ROSConnection is a
            // singleton the connector creates on demand, so this works in a bare
            // scene as well as in Main.unity.
            _ros = ROSConnection.GetOrCreateInstance();
            _rosManager = FindFirstObjectByType<ROSManager>();
            _hands = HandManager.Instance != null ? HandManager.Instance : FindFirstObjectByType<HandManager>();
            _headset = FindFirstObjectByType<HeadsetPublisher>();
            _posePublisher = FindFirstObjectByType<PosePublisher>();

            _profiles = ConnectionProfiles.Load();

            BindTitleRow(root);
            BindProfiles(root);
            BindPosePublishing(root);
            BindDecimator(root);
            BindGloves(root);
        }

        // ---- Title row / status --------------------------------------------

        private void BindTitleRow(VisualElement root)
        {
            _refresh = Require<Button>(root, "Refresh");
            _statusText = Require<Label>(root, "StatusText");

            if (_refresh != null) _refresh.clicked += Reconnect;
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            // HasConnectionError is the same signal palmmenu's RosStatus.Update
            // tinted its RawImage with. It reads false before the first attempt
            // resolves, so this means "no error", not "confirmed up".
            bool ok = !_ros.HasConnectionError;
            _refresh?.EnableInClassList("is-connected", ok);

            if (_statusText == null) return;
            int active = ConnectionProfiles.ActiveIndex;
            string who = active >= 0 && active < _profiles.Count
                ? _profiles[active].name
                : "No profile";
            _statusText.text = $"{who} - {_ros.RosIPAddress}:{_ros.RosPort} " +
                               (ok ? "(connected)" : "(no connection)");
        }

        private void Reconnect()
        {
            Debug.Log($"[ConnectionSettingsPanel] Reconnecting to {_ros.RosIPAddress}:{_ros.RosPort}");
            if (_rosManager != null)
                // ROSManager has no Reconnect of its own, but OnIPDone IS
                // disconnect-set-persist-connect. Re-applying the current address
                // through it keeps the manager's own _ip in step and lets its
                // connection events fire, which a raw Connect() would not.
                _rosManager.OnIPDone(_ros.RosIPAddress);
            else
                ReconnectDirect();
            RefreshStatus();
        }

        private void ReconnectDirect()
        {
            _ros.Disconnect();
            _ros.Connect();
        }

        // ---- Profiles -------------------------------------------------------

        private void BindProfiles(VisualElement root)
        {
            _profileDropdown = Require<DropdownField>(root, "ConnectionProfile");
            var add = Require<Button>(root, "AddButton");
            _editButton = Require<Button>(root, "EditButton");
            _deleteButton = Require<Button>(root, "DeleteButton");
            _entryHost = Require<VisualElement>(root, "EntryHost");

            CloseEntryPanel();
            RefreshProfileDropdown();

            if (_profileDropdown != null)
                _profileDropdown.RegisterValueChangedCallback(_ =>
                {
                    int i = _profileDropdown.index;
                    if (i < 0 || i >= _profiles.Count) return;
                    ActivateProfile(i);
                });

            // Add and Edit both need the template; without it they would open
            // nothing, so gate them visibly rather than silently doing nothing.
            bool canEdit = BindSection(profileEntryTemplate != null && _entryHost != null,
                                       Require<VisualElement>(root, "Profile"));

            if (canEdit && add != null) add.clicked += () => OpenEntryPanel(NewProfileIndex);
            if (canEdit && _editButton != null) _editButton.clicked += EditSelectedProfile;
            if (_deleteButton != null) _deleteButton.clicked += DeleteSelectedProfile;
        }

        private void RefreshProfileDropdown()
        {
            if (_profileDropdown != null)
            {
                // Just the name -- the endpoint is shown in the status line, and a
                // dropdown of "name (ip:port)" truncates badly at this width.
                var names = new List<string>();
                foreach (ConnectionProfile p in _profiles) names.Add(p.name);
                _profileDropdown.choices = names;

                int active = ConnectionProfiles.ActiveIndex;
                // SetValueWithoutNotify, not `index = ...`: assigning the index
                // fires the change callback, which would reconnect on every open.
                _profileDropdown.SetValueWithoutNotify(
                    active >= 0 && active < names.Count ? names[active] : string.Empty);
            }

            // Edit and Delete are meaningless with nothing selected or nothing
            // saved. Requested behaviour is "do nothing" -- disabling says why.
            bool hasSelection = HasSelection();
            _editButton?.SetEnabled(hasSelection && profileEntryTemplate != null);
            _deleteButton?.SetEnabled(hasSelection);

            RefreshStatus();
        }

        private bool HasSelection()
        {
            if (_profiles.Count == 0) return false;
            int i = _profileDropdown != null ? _profileDropdown.index : ConnectionProfiles.ActiveIndex;
            return i >= 0 && i < _profiles.Count;
        }

        private int SelectedIndex()
        {
            int i = _profileDropdown != null ? _profileDropdown.index : ConnectionProfiles.ActiveIndex;
            return i >= 0 && i < _profiles.Count ? i : -1;
        }

        /// <summary>
        /// Makes a stored profile the active one and hands it to the connection:
        /// writes the "ip"/"port" prefs everything else reads, pushes them into
        /// ROSConnection, then reconnects so the change actually takes effect.
        /// </summary>
        private void ActivateProfile(int index)
        {
            if (index < 0 || index >= _profiles.Count) return;
            ConnectionProfile profile = _profiles[index];

            ConnectionProfiles.ApplyAsActive(profile);
            ConnectionProfiles.ActiveIndex = index;

            if (_rosManager != null)
            {
                // Port first: OnIPDone is what reconnects, so setting the port
                // after it would not take effect until the next reconnect.
                _rosManager.OnPortDone(profile.port.ToString());
                _rosManager.OnIPDone(profile.ip);
            }
            else
            {
                _ros.RosIPAddress = profile.ip;
                _ros.RosPort = profile.port;
                ReconnectDirect();
            }

            RefreshProfileDropdown();
            RefreshStatus();
        }

        private void EditSelectedProfile()
        {
            int i = SelectedIndex();
            if (i < 0)
            {
                Debug.Log("[ConnectionSettingsPanel] Edit ignored -- no profile selected.");
                return;
            }
            OpenEntryPanel(i);
        }

        private void DeleteSelectedProfile()
        {
            int i = SelectedIndex();
            if (i < 0)
            {
                // Requested: do nothing when there is no selection or no entries.
                Debug.Log("[ConnectionSettingsPanel] Delete ignored -- no profile selected.");
                return;
            }

            ConnectionProfile removed = _profiles[i];
            _profiles.RemoveAt(i);
            ConnectionProfiles.Save(_profiles);

            // palmmenu's Delete button called ROSManager.DeleteIP, which removes
            // ROSManager's CURRENT _ip from the "ips" list -- so it is only the
            // right call when the host being forgotten is the one in use, and
            // only when no surviving profile still points at it (several
            // profiles may share a host on different ports).
            bool ipStillUsed = _profiles.Exists(pr => pr.ip == removed.ip);
            if (!ipStillUsed && _rosManager != null && _ros.RosIPAddress == removed.ip)
                _rosManager.DeleteIP();

            // Deleting does not disconnect: the live IP/port stay as they are,
            // only the stored selection moves. Yanking the connection out from
            // under the operator because a bookmark was tidied up would be worse.
            int active = ConnectionProfiles.ActiveIndex;
            if (active == i) ConnectionProfiles.ActiveIndex = -1;
            else if (active > i) ConnectionProfiles.ActiveIndex = active - 1;

            // Editing the entry that was just deleted would write to a stale index.
            if (_editingIndex == i) CloseEntryPanel();
            else if (_editingIndex > i) _editingIndex--;

            RefreshProfileDropdown();
            Debug.Log($"[ConnectionSettingsPanel] Deleted profile {removed.Describe()}");
        }

        // ---- New / Edit profile panel --------------------------------------

        /// <summary>
        /// Opens the New Profile panel, as the Add button does. Public so the
        /// panel can be opened from outside the UI -- another menu, a shortcut,
        /// or ClaudeBridge's INVOKE for a screenshot.
        /// </summary>
        public void OpenNewProfilePanel()
        {
            if (profileEntryTemplate == null || _entryHost == null)
            {
                Debug.LogWarning("[ConnectionSettingsPanel] Cannot open the profile panel -- " +
                                 "profileEntryTemplate is not assigned.", this);
                return;
            }
            OpenEntryPanel(NewProfileIndex);
        }

        private void OpenEntryPanel(int editIndex)
        {
            _entryHost.Clear();
            _entryFields.Clear();
            _editingIndex = editIndex;

            VisualElement entry = profileEntryTemplate.Instantiate();
            // Instantiate() wraps the tree in a TemplateContainer that does not
            // inherit the template root's sizing; let it fill the host.
            entry.style.flexGrow = 1;
            _entryHost.Add(entry);

            var title = entry.Q<Label>("Title");
            var nameField = entry.Q<TextField>("ProfileName");
            var ipField = entry.Q<TextField>("IP");
            var portField = entry.Q<TextField>("Port");
            var error = entry.Q<Label>("Error");
            var save = entry.Q<Button>("Save");
            var cancel = entry.Q<Button>("Cancel");

            bool editing = editIndex >= 0 && editIndex < _profiles.Count;
            if (title != null) title.text = editing ? "Edit Profile" : "New Profile";
            if (error != null) error.text = string.Empty;

            if (editing)
            {
                ConnectionProfile p = _profiles[editIndex];
                nameField?.SetValueWithoutNotify(p.name);
                ipField?.SetValueWithoutNotify(p.ip);
                portField?.SetValueWithoutNotify(p.port.ToString());
            }
            else
            {
                // Prefill from the live connection: the common case for Add is
                // "save what I am connected to right now, under a name".
                nameField?.SetValueWithoutNotify(string.Empty);
                ipField?.SetValueWithoutNotify(_ros.RosIPAddress);
                portField?.SetValueWithoutNotify(_ros.RosPort.ToString());
            }

            // System keyboard on every field. No commit callbacks -- nothing is
            // applied until Save, so an abandoned edit changes nothing.
            _entryFields.Add(SystemKeyboardField.Attach(nameField, TouchScreenKeyboardType.ASCIICapable, null, null));
            _entryFields.Add(SystemKeyboardField.Attach(ipField, TouchScreenKeyboardType.ASCIICapable,
                                                        ConnectionProfiles.IsValidHost, null));
            _entryFields.Add(SystemKeyboardField.Attach(portField, TouchScreenKeyboardType.NumberPad,
                                                        text => ConnectionProfiles.TryParsePort(text, out _), null));
            _entryFields.RemoveAll(f => f == null);

            if (cancel != null) cancel.clicked += CloseEntryPanel;
            if (save != null) save.clicked += () => SaveEntry(nameField, ipField, portField, error);

            // Picking back on before the grow, so the panel is usable as soon as
            // it is visible.
            _entryHost.pickingMode = PickingMode.Position;
            // Added immediately, NOT via schedule.Execute. The transition still
            // plays because EntryHost itself is not new -- it comes from the UXML
            // and already has a computed scale of 0 to animate from; only its
            // children were just built. Deferring by a frame instead made the
            // panel depend on the element scheduler, which does not tick for a
            // panel rendering into a texture, leaving it stuck invisible at
            // scale 0 (found exactly that while screenshotting it).
            _entryHost.AddToClassList("is-open");
        }

        private void SaveEntry(TextField nameField, TextField ipField, TextField portField, Label error)
        {
            string host = ipField?.value?.Trim() ?? string.Empty;
            string portText = portField?.value?.Trim() ?? string.Empty;

            void Fail(string message)
            {
                if (error != null) error.text = message;
                Debug.LogWarning($"[ConnectionSettingsPanel] {message}");
            }

            if (!ConnectionProfiles.IsValidHost(host))
            {
                Fail("Server IP must be an address or hostname (letters, digits, dots, hyphens).");
                return;
            }
            if (!ConnectionProfiles.TryParsePort(portText, out int port))
            {
                Fail($"Port must be a whole number between {ConnectionProfiles.MinPort} and {ConnectionProfiles.MaxPort}.");
                return;
            }

            bool editing = _editingIndex >= 0 && _editingIndex < _profiles.Count;
            string typedName = nameField?.value?.Trim();
            // Fall back to the host as the name so a profile is never nameless.
            string desired = string.IsNullOrWhiteSpace(typedName) ? host : typedName;

            int index;
            if (editing)
            {
                // Exclude the entry being edited from the uniqueness check, or
                // keeping its own name would rename it to "Name 2".
                var others = new List<ConnectionProfile>(_profiles);
                others.RemoveAt(_editingIndex);
                _profiles[_editingIndex] = new ConnectionProfile(
                    ConnectionProfiles.UniqueName(desired, others), host, port);
                index = _editingIndex;
            }
            else
            {
                _profiles.Add(new ConnectionProfile(
                    ConnectionProfiles.UniqueName(desired, _profiles), host, port));
                index = _profiles.Count - 1;
            }

            ConnectionProfiles.Save(_profiles);
            CloseEntryPanel();
            RefreshProfileDropdown();
            // Saving selects and connects it -- it was typed in order to be used.
            ActivateProfile(index);

            // Mirror the IP into ROSManager's own list (PlayerPrefs "ips"), which
            // is what palmmenu's Add button did via SaveIP. Kept in sync so the
            // uGUI menu's dropdown still offers the same hosts while both UIs
            // exist. Must run AFTER ActivateProfile: SaveIP stores ROSManager's
            // _ip, which OnIPDone has only just set to this profile's host.
            _rosManager?.SaveIP();
        }

        private void CloseEntryPanel()
        {
            _entryFields.Clear();
            _editingIndex = NewProfileIndex;
            if (_entryHost == null) return;

            _entryHost.RemoveFromClassList("is-open");
            // A zero-scaled element still occupies its layout rect for
            // hit-testing, so without this the collapsed panel keeps eating
            // clicks in the empty space to the right of the menu.
            _entryHost.pickingMode = PickingMode.Ignore;
            // Children are deliberately LEFT in place: they are invisible at
            // scale 0, and OpenEntryPanel clears the host before rebuilding. A
            // delayed Clear would have to run on the element scheduler, which is
            // not dependable here (see OpenEntryPanel), and clearing immediately
            // would make the panel vanish instead of shrinking away.
        }

        // ---- Pose publishing / decimator / gloves ---------------------------

        private void BindPosePublishing(VisualElement root)
        {
            var toggle = Require<Toggle>(root, "PosePublishing");
            if (!BindSection(_posePublisher != null, Require<VisualElement>(root, "PosePubRow")) || toggle == null)
                return;

            toggle.SetValueWithoutNotify(_posePublisher._enabled);
            toggle.RegisterValueChangedCallback(evt => _posePublisher.SetEnabled(evt.newValue));
        }

        private void BindDecimator(VisualElement root)
        {
            var slider = Require<SliderInt>(root, "DecimatorSlider");
            if (!BindSection(_headset != null, Require<VisualElement>(root, "Decimator")) || slider == null)
                return;

            slider.SetValueWithoutNotify(Mathf.Clamp(_headset.Decimator, slider.lowValue, slider.highValue));
            slider.RegisterValueChangedCallback(evt => _headset.OnDecimatorChange(evt.newValue));
        }

        private void BindGloves(VisualElement root)
        {
            _leftGlove = Require<VisualElement>(root, "LeftHand");
            _rightGlove = Require<VisualElement>(root, "RightHand");

            if (!BindSection(_hands != null, Require<VisualElement>(root, "Haptics"))) return;
            RefreshGloves();
        }

        private void RefreshGloves()
        {
            _leftGlove?.EnableInClassList("is-connected", _hands.HasLeftGlove);
            _rightGlove?.EnableInClassList("is-connected", _hands.HasRightGlove);
        }

        // ---- Update ---------------------------------------------------------

        private void Update()
        {
            // The system keyboard must be pumped every frame while open -- that
            // is how typed text reaches the field.
            for (int i = 0; i < _entryFields.Count; i++) _entryFields[i].Poll();

            if (Time.time < _nextStatusRefresh) return;
            _nextStatusRefresh = Time.time + statusRefreshInterval;

            RefreshStatus();
            if (_hands != null) RefreshGloves();
        }
    }
}
