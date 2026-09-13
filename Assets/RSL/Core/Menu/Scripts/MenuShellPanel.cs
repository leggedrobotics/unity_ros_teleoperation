// UI Toolkit controller for MenuShell.uxml -- the port of palmmenu's StatusImg:
// the connection lamp, the version and local IP it could reveal, and the press
// that shows the debug overlays.
//
// The four menu buttons around it are NOT here. Each is its own document with
// its own PalmTabPanel, because the palm is curved and every slot carries a
// different rotation that a single shared panel cannot express -- see the note
// at the top of PalmTab.uxml.
//
// STATUS SOURCE: ROSManager's OnConnectionColor / OnConnectionStagnant events --
// the very same two the uGUI StatusIndicator listened to. Subscribed rather than
// polled so the shell updates on the same edge the old indicator did.
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Robotics.ROSTCPConnector;
using RSL.Core.Haptics;

namespace RSL.Core.Menu
{
    public class MenuShellPanel : UIToolkitPanel
    {
        [Tooltip("Show version and local IP without needing the debug reveal press. " +
                 "Off by default, matching StatusImg: on the palm these two lines sit " +
                 "over the menu below and are only wanted while debugging.")]
        public bool showStatusTextByDefault = false;

        private ROSManager _rosManager;
        private ROSConnection _ros;
        private GetIpAddress _ipSource;
        private HandManager _hands;
        private MenuManager _menuManager;
        private ToggleActive _debugMenus;

        private Button _statusButton;
        private VisualElement _statusText;
        private Label _version;
        private Label _ip;

        private bool _connected;

        // Kept so the listeners added in Bind can be removed again -- these are
        // UnityEvents on a component that outlives this panel.
        private UnityEngine.Events.UnityAction<bool> _onConnection;
        private UnityEngine.Events.UnityAction<bool> _onStagnant;

        protected override void Bind(VisualElement root)
        {
            _ros = ROSConnection.GetOrCreateInstance();
            _rosManager = FindFirstObjectByType<ROSManager>();
            _ipSource = FindFirstObjectByType<GetIpAddress>();
            _hands = HandManager.Instance != null ? HandManager.Instance : FindFirstObjectByType<HandManager>();
            _menuManager = FindFirstObjectByType<MenuManager>();
            _debugMenus = FindFirstObjectByType<ToggleActive>();

            BindStatus(root);
            BindDebugReveal(root);
        }

        private void OnDisable()
        {
            if (_rosManager == null) return;
            if (_onConnection != null) _rosManager.OnConnectionColor.RemoveListener(_onConnection);
            if (_onStagnant != null) _rosManager.OnConnectionStagnant.RemoveListener(_onStagnant);
            _onConnection = null;
            _onStagnant = null;
        }

        // ---- Status header --------------------------------------------------

        private void BindStatus(VisualElement root)
        {
            // The status BUTTON is the lamp: one element doing both jobs, the
            // same way the uGUI StatusImg was an Image with a Button on it.
            _statusButton = Require<Button>(root, "DebugReveal");
            _statusText = Require<VisualElement>(root, "StatusText");
            _version = Require<Label>(root, "Version");
            _ip = Require<Label>(root, "IpAddress");

            // VersionIndicator writes this into a TMP label it owns, so there is
            // nothing to route through -- the value itself is just the app version.
            if (_version != null) _version.text = "v" + Application.version;
            RefreshIp();
            SetStatusTextVisible(showStatusTextByDefault);

            // Gate only the ICON on ROSManager: version and IP come from elsewhere
            // and stay readable without it.
            if (BindSection(_rosManager != null, Require<VisualElement>(root, "Status")))
            {
                _onConnection = OnRosConnection;
                _onStagnant = OnDelay;
                _rosManager.OnConnectionColor.AddListener(_onConnection);
                _rosManager.OnConnectionStagnant.AddListener(_onStagnant);
            }

            // Seed from the live connection so the icon is right before the first
            // event fires (ROSManager only raises these on a CHANGE).
            OnRosConnection(!_ros.HasConnectionError);
        }

        // Mirrors StatusIndicator.OnRosConnection.
        private void OnRosConnection(bool connected)
        {
            _connected = connected;
            _statusButton?.EnableInClassList("is-connected", connected);
            _statusButton?.EnableInClassList("is-stagnant", false);
        }

        // Mirrors StatusIndicator.OnDelay: only meaningful while connected -- a
        // stalled stream on a live socket is a different state from being down.
        private void OnDelay(bool stagnant)
        {
            if (!_connected) return;
            _statusButton?.EnableInClassList("is-stagnant", stagnant);
        }

        private void RefreshIp()
        {
            if (_ip == null) return;
            // GetLocalIPv4 handles its own resolver failures now, so there is
            // nothing to guard here. With no GetIpAddress in the scene there is no
            // address to show at all -- report just the ROS version rather than
            // inventing a placeholder that reads like a real value.
            _ip.text = _ipSource != null
                ? $"{_ros.rosVersion} : {_ipSource.GetLocalIPv4()}"
                : _ros.rosVersion.ToString();
        }

        private void SetStatusTextVisible(bool visible)
        {
            _statusText?.EnableInClassList("is-hidden", !visible);
        }

        // ---- Debug reveal ---------------------------------------------------

        private void BindDebugReveal(VisualElement root)
        {
            var button = Require<Button>(root, "DebugReveal");
            if (button == null) return;

            // palmmenu's StatusImg button fanned out to five targets. Four of them
            // still exist as components and are called here; the fifth toggled the
            // visibility of the uGUI version/IP labels, which this panel now owns.
            button.clicked += () =>
            {
                bool nowVisible = _statusText != null && _statusText.ClassListContains("is-hidden");
                SetStatusTextVisible(nowVisible);
                if (nowVisible) RefreshIp();

                _hands?.ToggleSkeleton();
                _menuManager?.ToggleLoggers();
                _debugMenus?.Toggle();
            };
        }
    }
}
