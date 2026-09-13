// UI Toolkit controller for DebugSettings.uxml -- the port of the uGUI
// palmmenu's debug sub-menu (palmmenu > Debug Menus > TFDebug, whose visible
// header read "TF Settings").
//
// Its controls were all siblings under that one menu, and all live here:
//   Point size          -> TFViz.Resize
//   Toggle TF (may lag) -> TFViz.VizTFs
//   Use Sim Time        -> UseSimTime.ToggleSimTime
//   Enable Passthrough  -> PassthroughToggle.SetPassthroughEnabled
//   Clear Settings      -> ClearSettings.ClearAllSettings
// Use Sim Time, Enable Passthrough and Clear Settings were briefly on
// GeneralSettings; they moved here once the debug menu existed, so that the
// grouping matches the prefab it is ported from. Do not put them back on
// General without moving them here too -- two panels driving the same toggle
// is the drift this port exists to avoid.
//
// Like the other panels this OWNS NO STATE: each control reads from the
// component that owns the behaviour and writes back through the same public
// method palmmenu's UnityEvents called, so the uGUI menu's own labels stay
// correct while both UIs exist.
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.ARFoundation;
using RSL.Core.TF;

namespace RSL.Core.Menu
{
    public class DebugSettingsPanel : UIToolkitPanel
    {
        private TFViz _tfViz;
        private UseSimTime _useSimTime;
        private PassthroughToggle _passthrough;
        private ClearSettings _clearSettings;
        private ARCameraManager _arCamera;

        // Mirrors ClearSettings.ClearAllSettings for the case where that
        // component is not in the scene. It reads the comma-joined key registry
        // under "PlayerPrefsKeys" -- which the camera/sensor code populates -- and
        // skips the connection keys, because clearing settings must not forget how
        // to reach the robot.
        //
        // Deliberately NOT a hardcoded key list: that would both miss whatever the
        // cameras registered and delete keys the real implementation preserves.
        private const string KeyRegistryPref = "PlayerPrefsKeys";
        private static readonly string[] PreservedKeys = { "ip", "port", "ips" };
        protected override void Bind(VisualElement root)
        {
            _tfViz = FindFirstObjectByType<TFViz>();
            _useSimTime = FindFirstObjectByType<UseSimTime>();
            _passthrough = FindFirstObjectByType<PassthroughToggle>();
            _clearSettings = FindFirstObjectByType<ClearSettings>();
            _arCamera = FindFirstObjectByType<ARCameraManager>();

            BindPointSize(root);
            BindTfViz(root);
            BindSimTime(root);
            BindPassthrough(root);
            BindClearSettings(root);
        }

        // palmmenu's "Point size" slider, wired to TFViz.Resize -- it scales the
        // spawned TF frame labels.
        private void BindPointSize(VisualElement root)
        {
            var slider = Require<Slider>(root, "PointSizeSlider");
            if (!BindSection(_tfViz != null, Require<VisualElement>(root, "PointSizeRow")) || slider == null)
                return;

            // Resize stores nothing to read back (it writes localScale straight
            // onto the label objects), so the authored default is the only
            // starting value available -- left as-is rather than faked.
            slider.RegisterValueChangedCallback(evt => _tfViz.Resize(evt.newValue));
        }

        private void BindTfViz(VisualElement root)
        {
            var button = Require<Button>(root, "ToggleTf");
            if (!BindSection(_tfViz != null, Require<VisualElement>(root, "TfVizRow")) || button == null)
                return;

            // VizTFs is a one-shot that rebuilds or tears down the frame labels
            // and exposes no state to read back, so this stays a button rather
            // than a toggle that could show the wrong position.
            button.clicked += () => _tfViz.VizTFs();
        }

        private void BindSimTime(VisualElement root)
        {
            var toggle = Require<Toggle>(root, "SimTime");
            if (toggle == null) return;

            // TFStream.UseSimTime is a static on the ROS-TCP-Connector, so the
            // row works with no manager present -- never gated.
            toggle.SetValueWithoutNotify(TFStream.UseSimTime);
            toggle.RegisterValueChangedCallback(evt =>
            {
                if (_useSimTime != null)
                {
                    // ToggleSimTime FLIPS rather than sets, so only call it when
                    // the state actually differs; going through it also keeps its
                    // own TMP label correct.
                    if (TFStream.UseSimTime != evt.newValue) _useSimTime.ToggleSimTime();
                }
                else
                {
                    TFStream.UseSimTime = evt.newValue;
                    PlayerPrefs.SetInt("use_sim_time", evt.newValue ? 1 : 0);
                    PlayerPrefs.Save();
                }
            });
        }

        private void BindPassthrough(VisualElement root)
        {
            var toggle = Require<Toggle>(root, "Passthrough");
            // Needs something to report the current state from, even when
            // PassthroughToggle is the thing being driven.
            if (!BindSection(_arCamera != null, Require<VisualElement>(root, "PassthroughRow")) || toggle == null)
                return;

            toggle.SetValueWithoutNotify(_arCamera.enabled);
            toggle.RegisterValueChangedCallback(evt =>
            {
                // Note the inverted argument: SetPassthroughEnabled takes
                // "disabled", so 0 means passthrough ON.
                if (_passthrough != null) _passthrough.SetPassthroughEnabled(evt.newValue ? 0 : 1);
                else _arCamera.enabled = evt.newValue;
            });
        }

        private void BindClearSettings(VisualElement root)
        {
            var button = Require<Button>(root, "ClearSettings");
            if (button == null) return;

            button.clicked += () =>
            {
                if (_clearSettings != null)
                {
                    _clearSettings.ClearAllSettings();
                }
                else
                {
                    int cleared = 0;
                    foreach (string key in PlayerPrefs.GetString(KeyRegistryPref, "").Split(','))
                    {
                        if (string.IsNullOrWhiteSpace(key)) continue;
                        if (System.Array.IndexOf(PreservedKeys, key) >= 0) continue;
                        PlayerPrefs.DeleteKey(key);
                        cleared++;
                    }
                    PlayerPrefs.Save();
                    Debug.Log($"[DebugSettingsPanel] Cleared {cleared} saved setting(s) " +
                              "(connection ip/port/ips preserved).");
                }
            };
        }
    }
}
