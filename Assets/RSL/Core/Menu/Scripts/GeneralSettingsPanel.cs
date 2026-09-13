// UI Toolkit controller for GeneralSettings.uxml -- the UI Toolkit port of the
// uGUI palmmenu's "Settings" sub-menu (palmmenu > Settings > SettingsMenu),
//
// Use Sim Time, Enable Passthrough, Clear Settings and Toggle TF are NOT here.
// All four were siblings under palmmenu > Debug Menus > TFDebug, so they live
// on DebugSettings.uxml / DebugSettingsPanel. Do not add them back without
// removing them there -- two panels driving one toggle is the drift this port
// exists to avoid.
//
// Connected Gloves and the Pose Publish Decimator are deliberately NOT here:
// tracing the palmmenu prefab puts both under palmmenu > Wifi > WifiMenu, so
// they belong to ConnectionSettings.uxml / ConnectionSettingsPanel. Do not add
// them back without moving them there too -- two panels writing the same
// decimator is exactly the drift this port is trying to avoid.
//
// This panel OWNS NO STATE. Every control reads its initial value from the
// manager that actually owns it and writes back through that manager's existing
// public method -- the same methods the palmmenu's UnityEvents called. That is
// deliberate: during the migration both UIs exist, and a panel that cached its
// own copy of "is the pose locked" would drift the moment the other UI, an
// inspector button (SettingsManagerEditor), or a joystick binding changed it.
//
// Sections whose backing manager is absent from the scene are dimmed and
// disabled rather than hidden -- see BindSection. MenuTestScene deliberately
// contains none of them, so without this the panel would either throw on load
// or silently look fully functional while wired to nothing.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using RSL.Core.Robots;
using RSL.Telemetry.Headset;
using RSL.Telemetry.Pose;

namespace RSL.Core.Menu
{
    public class GeneralSettingsPanel : UIToolkitPanel
    {
        // Interaction modes, in the order SettingsManager.ChangeMode switches on
        // them (0 = all off, 1 = pose publisher, 2 = joystick). The palmmenu
        // held these strings in its LocomotionDropdown options.
        private static readonly List<string> InteractionModes = new List<string>
        {
            "Disabled", "Pose Publisher", "Joystick"
        };

        private SettingsManager _settings;
        private ModelManager _models;
        private HeadsetPublisher _headset;
        // Owns the Settings menu's Pose Topic field (it even holds the
        // TMP_InputField reference, poseTopicInput). NOT HeadsetPublisher --
        // that publishes /quest/pose for the headset, while this panel's
        // Pose Topic is the teleop target topic (/target_pose).
        private PosePublisher _posePublisher;

        private Button _lockPose;
        private Button _lockRobot;
        private Button _toggleModel;

        // Managers are looked up rather than serialized so the same prefab drops
        // into any scene: Main.unity has all of them, MenuTestScene has none.
        // Singletons are preferred where they exist, since a scene-wide search
        // costs more and is order-dependent during Awake.
        private void ResolveManagers()
        {
            _settings = FindFirstObjectByType<SettingsManager>();
            _models = ModelManager.instance != null ? ModelManager.instance : FindFirstObjectByType<ModelManager>();
            _headset = FindFirstObjectByType<HeadsetPublisher>();
            _posePublisher = FindFirstObjectByType<PosePublisher>();
        }

        protected override void Bind(VisualElement root)
        {
            ResolveManagers();
            BindRobot(root);
            BindPose(root);
            BindSystem(root);
        }

        // ---- Robot ----------------------------------------------------------

        private void BindRobot(VisualElement root)
        {
            var dropdown = Require<DropdownField>(root, "RobotModel");
            _toggleModel = Require<Button>(root, "ToggleModel");
            var rootFrame = Require<TextField>(root, "RootFrame");

            bool available = BindSection(_models != null, Require<VisualElement>(root, "RobotRow"), Require<VisualElement>(root, "RootFrameRow"));
            if (!available) return;

            if (dropdown != null && _models.robotDatabase != null)
            {
                var names = new List<string>();
                // var, not RSL.Robots.RobotEntry: pulling that namespace into
                // scope here collides with RSL.Core.Robots (ModelManager's own).
                foreach (var robot in _models.robotDatabase.robots)
                    names.Add(robot.name);

                dropdown.choices = names;
                int current = Mathf.Clamp(_models.startRobotIndex, 0, Mathf.Max(0, names.Count - 1));
                // SetValueWithoutNotify, NOT `index = ...`. Assigning the index
                // raises a ChangeEvent, and Bind runs every time this document is
                // enabled -- so merely OPENING the Settings menu fired the
                // callback below, called ChangeModel, and reloaded the scene. The
                // reload then left TFSystem (a plain static, so it survives a
                // scene load) holding destroyed frame GameObjects, and every
                // TFAttachment.Start threw MissingReferenceException.
                //
                // Name rather than index because that is what the notify-free
                // setter takes; the callback still reads back .index, since
                // ChangeModel wants an index and two database entries are free to
                // share a display name.
                if (names.Count > 0 && current < names.Count)
                    dropdown.SetValueWithoutNotify(names[current]);

                dropdown.RegisterValueChangedCallback(_ =>
                {
                    // Guarded as well as set quietly. Belt and braces is warranted
                    // here specifically because the cost of a spurious call is a
                    // whole scene reload, not a redundant setter.
                    if (dropdown.index < 0 || dropdown.index == _models.startRobotIndex) return;
                    // ChangeModel reloads the active scene once ModelManager has
                    // finished initialising, which tears this panel down and
                    // rebuilds it -- so do not touch any UI after this call.
                    _models.ChangeModel(dropdown.index);
                });
            }

            if (_toggleModel != null)
            {
                UpdateModelVisibilityIcon();
                _toggleModel.clicked += () =>
                {
                    _models.ToggleModel();
                    UpdateModelVisibilityIcon();
                };
            }

            if (rootFrame != null)
            {
                // ModelManager resolves the same pref in Awake; read it rather
                // than the live _root name so the field shows what will be used
                // even before a robot has spawned.
                rootFrame.SetValueWithoutNotify(PlayerPrefs.GetString("rootFrame", "odom"));
                // Commit on blur/Enter only. A per-keystroke callback would
                // rename the TF root once per character typed.
                rootFrame.RegisterCallback<FocusOutEvent>(_ => CommitRootFrame(rootFrame));
                rootFrame.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                        CommitRootFrame(rootFrame);
                });
            }
        }

        private void CommitRootFrame(TextField field)
        {
            string frame = field.value?.Trim();
            if (string.IsNullOrEmpty(frame))
            {
                // An empty frame id would leave the TF root unnamed; put the
                // effective value back in the field instead of accepting it.
                field.SetValueWithoutNotify(PlayerPrefs.GetString("rootFrame", "odom"));
                return;
            }
            _models.ChangeRootFrame(frame);
        }

        private void UpdateModelVisibilityIcon()
        {
            // ModelManager._enabled is true when the model is showing, so the
            // "hidden" icon is the inverse.
            _toggleModel?.EnableInClassList("is-hidden", !_models._enabled);
        }

        // ---- Pose -----------------------------------------------------------

        private void BindPose(VisualElement root)
        {
            var mode = Require<DropdownField>(root, "InteractionMode");
            _lockPose = Require<Button>(root, "LockPose");
            _lockRobot = Require<Button>(root, "LockRobot");
            var recenter = Require<Button>(root, "Recenter");
            var poseTopic = Require<TextField>(root, "PoseTopic");

            // Pose topic comes off PosePublisher, everything else off
            // SettingsManager -- they are independent, so gate them separately.
            if (BindSection(_posePublisher != null, Require<VisualElement>(root, "PoseTopicRow")) && poseTopic != null)
            {
                poseTopic.SetValueWithoutNotify(_posePublisher.poseTopic);
                poseTopic.RegisterCallback<FocusOutEvent>(_ =>
                {
                    string topic = poseTopic.value?.Trim();
                    if (string.IsNullOrEmpty(topic))
                    {
                        poseTopic.SetValueWithoutNotify(_posePublisher.poseTopic);
                        return;
                    }
                    // OnPoseTopic, not a bare field write: it re-registers the
                    // publisher on the new topic and persists "poseTopic", which a
                    // field assignment would skip entirely.
                    _posePublisher.OnPoseTopic(topic);
                });
            }

            if (!BindSection(_settings != null, Require<VisualElement>(root, "InteractionRow"), Require<VisualElement>(root, "PoseButtons"))) return;

            if (mode != null)
            {
                mode.choices = InteractionModes;
                // Same hazard as the robot dropdown above: assigning the index
                // notifies, and Bind runs on every open, so this re-applied the
                // interaction mode each time the menu was shown.
                int current = Mathf.Clamp(_settings.Mode, 0, InteractionModes.Count - 1);
                mode.SetValueWithoutNotify(InteractionModes[current]);
                mode.RegisterValueChangedCallback(_ =>
                {
                    if (mode.index < 0 || mode.index == _settings.Mode) return;
                    _settings.ChangeMode(mode.index);
                });
            }

            if (_lockPose != null)
            {
                UpdateLockIcons();
                _lockPose.clicked += () =>
                {
                    _settings.TogglePoseLock();
                    UpdateLockIcons();
                };
            }

            if (_lockRobot != null)
            {
                _lockRobot.clicked += () =>
                {
                    _settings.ToggleCenterLock();
                    UpdateLockIcons();
                };
            }

            if (recenter != null)
                recenter.clicked += () => _settings.Recenter();
        }

        private void UpdateLockIcons()
        {
            _lockPose?.EnableInClassList("is-locked", _settings.PoseLocked);
            _lockRobot?.EnableInClassList("is-locked", _settings.RobotLocked);
        }


        // ---- System ---------------------------------------------------------

        private void BindSystem(VisualElement root)
        {
            BindNvblox(root);
            BindOpacity(root);
        }

        private void BindNvblox(VisualElement root)
        {
            var toggle = Require<Toggle>(root, "Nvblox");
            // SettingsManager.ToggleNvblox is a no-op without a mesh assigned,
            // so require the mesh itself, not just the manager.
            bool available = _settings != null && _settings.nvbloxMesh != null;
            if (!BindSection(available, Require<VisualElement>(root, "NvbloxRow")) || toggle == null) return;

            toggle.SetValueWithoutNotify(_settings.nvbloxMesh._enabled);
            toggle.RegisterValueChangedCallback(evt =>
            {
                // ToggleNvblox flips rather than sets, so only call it when the
                // mesh is not already in the requested state (a programmatic
                // SetValueWithoutNotify elsewhere could otherwise invert it).
                if (_settings.nvbloxMesh._enabled != evt.newValue)
                    _settings.ToggleNvblox();
            });
        }

        // palmmenu's Opacity slider, wired to NvbloxMesh.ChangeOpactity.
        private void BindOpacity(VisualElement root)
        {
            var slider = Require<Slider>(root, "OpacitySlider");
            bool available = _settings != null && _settings.nvbloxMesh != null;
            if (!BindSection(available, Require<VisualElement>(root, "OpacityRow")) || slider == null)
                return;

            // ChangeOpactity pushes straight to the material and does NOT write
            // back to the mesh's `opacity` field, so that field is the only
            // reliable starting value.
            slider.SetValueWithoutNotify(
                Mathf.Clamp(_settings.nvbloxMesh.opacity, slider.lowValue, slider.highValue));
            slider.RegisterValueChangedCallback(evt => _settings.nvbloxMesh.ChangeOpactity(evt.newValue));
        }
    }
}
