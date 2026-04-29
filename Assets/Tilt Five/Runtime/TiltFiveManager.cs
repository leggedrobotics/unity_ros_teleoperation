/*
 * Copyright (C) 2020-2023 Tilt Five, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections;
using UnityEngine;

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
#endif

using TiltFive;
using TiltFive.Logging;

namespace TiltFive
{

    /// <summary>
    /// The Tilt Five manager.
    /// </summary>
    [DisallowMultipleComponent]
#if !UNITY_2019_1_OR_NEWER || !INPUTSYSTEM_AVAILABLE
    // Workaround to enable inputs to be collected before other scripts execute their Update() functions.
    // This is unnecessary if we're using the Input System's OnBeforeUpdate() to collect fresh inputs.
    [DefaultExecutionOrder(-500)]
#else
    // If the Input System's OnBeforeUpdate is available, set TiltFiveManager's execution order to be very late.
    // This is desirable in two similar scenarios:
    // - Our Update() executes last, providing the freshest pose data possible to any scripts using LateUpdate().
    // - Our LateUpdate() executes last, providing the freshest pose data possible before we render to the glasses.
    [DefaultExecutionOrder(500)]
#endif
    public class TiltFiveManager : TiltFive.SingletonComponent<TiltFiveManager>, ISceneInfo
    {
        /// <summary>
        /// The scale conversion runtime configuration data.
        /// </summary>
        public ScaleSettings scaleSettings;

        /// <summary>
        /// The game board runtime configuration data.
        /// </summary>
        public GameBoardSettings gameBoardSettings;

        /// <summary>
        /// The glasses runtime configuration data.
        /// </summary>
        public GlassesSettings glassesSettings;

        // TODO: Make {left,right}WandSettings into the members actually holding the data. These are
        // kept for prefab compatibility reasons, but will eventually switch.  Please start using
        // the new names from your own code.
        [Obsolete("primaryWandSettings is deprecated, please update to use left/right based on user preference instead.")]
        public WandSettings primaryWandSettings;
        [Obsolete("secondaryWandSettings is deprecated, please update to use left/right based on user preference instead.")]
        public WandSettings secondaryWandSettings;

        /// <summary>
        /// The wand runtime configuration data for the left hand wand.
        /// </summary>
        public WandSettings leftWandSettings {
            #pragma warning disable 618 // this is for compatibility; disable obsolete warning
            get => secondaryWandSettings;
            set => secondaryWandSettings = value;
            #pragma warning restore 618
        }

        /// <summary>
        /// The wand runtime configuration data for the right hand wand.
        /// </summary>
        public WandSettings rightWandSettings {
            #pragma warning disable 618 // this is for compatibility; disable obsolete warning
            get => primaryWandSettings;
            set => primaryWandSettings = value;
            #pragma warning restore 618
        }

        /// <summary>
        /// The spectator camera's runtime configuration data.
        /// </summary>
        public SpectatorSettings spectatorSettings = new SpectatorSettings();

        /// <summary>
        /// Project-wide graphics settings related to Tilt Five.
        /// </summary>
        public GraphicsSettings graphicsSettings = new GraphicsSettings();

        /// <summary>
        /// The log settings.
        /// </summary>
        public LogSettings logSettings = new LogSettings();

#if UNITY_EDITOR
        /// <summary>
        /// <b>EDITOR-ONLY</b> The editor settings.
        /// </summary>
        public EditorSettings editorSettings = new EditorSettings();

#endif

        private bool needsDriverUpdateNotifiedOnce = false;
        private bool needsDriverUpdateErroredOnce = false;

        internal PlayerSettings playerSettings = new PlayerSettings();

        /// <summary>
        /// Awake this instance.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            // Apply log settings
            Log.LogLevel = logSettings.level;
            Log.TAG = logSettings.TAG;

            // Store graphics settings
            graphicsSettings.applicationTargetFramerate = Application.targetFrameRate;
            graphicsSettings.applicationVSyncCount = QualitySettings.vSyncCount;

            if (!SystemControl.SetPlatformContext())
            {
                Log.Warn("Failed to set application context.");
                enabled = false;
            }

            if (!SystemControl.SetApplicationInfo())
            {
                Log.Warn("Failed to send application info to the T5 Service.");
                enabled = false;
            }

            RefreshPlayerSettings();

            spectatorSettings.spectatorCamera = glassesSettings.cameraTemplate;
            spectatorSettings.glassesMirrorMode = glassesSettings.glassesMirrorMode;
            spectatorSettings.spectatedPlayer = playerSettings.PlayerIndex;
        }

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
        /// <summary>
        /// Prepares for 3rd party scripts' Update() calls
        /// </summary>
        private void OnBeforeUpdate()
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPaused)
            {
                return;
            }
#endif
            if (Player.scanningForPlayers)
            {
                return;
            }
            NeedsDriverUpdate();
            Player.ScanForNewPlayers();
            Wand.GetLatestInputs();     // Should only be executed once per frame

            // OnBeforeUpdate can get called multiple times per frame. Unity seems to not properly utilize the camera positions for rendering 
            // if they are updated after Late Update and before render, causing a disparity between render pose and camera position leading to 
            // shaky displays in the headset. To avoid this, we prevent updating the camera positions during the BeforeRender Input State.
            if (UnityEngine.InputSystem.LowLevel.InputState.currentUpdateType != UnityEngine.InputSystem.LowLevel.InputUpdateType.BeforeRender)
            {
                Update();
            }
        }
#endif

        /// <summary>
        /// Update this instance.
        /// </summary>
        void Update()
        {
#if !UNITY_2019_1_OR_NEWER || !INPUTSYSTEM_AVAILABLE
            NeedsDriverUpdate();
            Player.ScanForNewPlayers();
            Wand.GetLatestInputs();     // Should only be executed once per frame
#endif
            RefreshPlayerSettings();
            RefreshSpectatorSettings();
            Display.ApplyGraphicsSettings(graphicsSettings);
            Player.Update(playerSettings, spectatorSettings);

            var spectatedPlayer = spectatorSettings.spectatedPlayer;
            if (Glasses.TryGetPreviewPose(spectatedPlayer, out var spectatedPlayerPose))
            {
                spectatorSettings.spectatorCamera?.transform.SetPositionAndRotation(
                    spectatedPlayerPose.position,
                    spectatedPlayerPose.rotation);
            }

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
            var devices = InputUser.GetUnpairedInputDevices();
            if (devices.Count > 0)
            {
                foreach (InputDevice dev in devices)
                {
                    if (dev is WandDevice)
                    {
                        var headPoseRoot = Glasses.GetPoseRoot(((WandDevice)dev).playerIndex);

                        if (headPoseRoot != null)
                        {
                            var playerInput = headPoseRoot.GetComponentInChildren<PlayerInput>();

                            if (playerInput != null && playerInput.user.valid)
                            {
                                Log.Warn($"Unpaired Wand Device [{((WandDevice)dev).ControllerIndex}] found and paired to Player [{((WandDevice)dev).playerIndex}].");
                                InputUser.PerformPairingWithDevice(dev, playerInput.user);
                                playerInput.user.ActivateControlScheme("XR");
                            }
                        }
                    }
                }
            }
#endif
        }


        /// <summary>
        /// Update this instance after all components have finished executing their Update() functions.
        /// </summary>
        void LateUpdate()
        {
            // Trackables should be updated just before rendering occurs,
            // after all Update() calls are completed.
            // This allows any Game Board movements to be finished before we base the
            // Glasses/Wand poses off of its pose, preventing perceived jittering.
            Player.Update(playerSettings, spectatorSettings);
        }

        /// <summary>
        /// Obtains the latest pose for all trackable objects.
        /// </summary>
        private void GetLatestPoseData()
        {
            Glasses.UpdateAllGlassesCores(glassesSettings, scaleSettings, gameBoardSettings);
            Wand.Update(PlayerIndex.One, leftWandSettings, scaleSettings, gameBoardSettings);
            Wand.Update(PlayerIndex.One, rightWandSettings, scaleSettings, gameBoardSettings);
        }

        /// <summary>
        /// Check if a driver update is needed.
        ///
        /// Note that this can also return false if this has not yet been able to connect to the
        /// Tilt Five driver service (compatibility state unknown), so this may need to be called
        /// multiple times in that case.  This only returns true if we can confirm that the driver
        /// is incompatible.
        ///
        /// If it is necessary to distinguish between unknown and compatible, use
        /// GetServiceCompatibility directly.
        /// </summary>
        public bool NeedsDriverUpdate()
        {
            if (!needsDriverUpdateErroredOnce)
            {
                try
                {
                    ServiceCompatibility compatibility = SystemControl.GetServiceCompatibility();
                    bool needsUpdate = compatibility == ServiceCompatibility.Incompatible;

                    if (needsUpdate)
                    {
                        if (!needsDriverUpdateNotifiedOnce)
                        {
                            Log.Warn("Incompatible Tilt Five service. Please update driver package.");
                            needsDriverUpdateNotifiedOnce = true;
                        }
                    }
                    else
                    {
                        // Not incompatible.  Reset the incompatibility warning.
                        needsDriverUpdateNotifiedOnce = false;
                    }
                    return needsUpdate;
                }
                catch (System.DllNotFoundException e)
                {
                    Log.Info(
                        "Could not connect to Tilt Five plugin for compatibility check: {0}",
                        e.Message);
                    needsDriverUpdateErroredOnce = true;
                }
                catch (System.Exception e)
                {
                    Log.Error(e.Message);
                    needsDriverUpdateErroredOnce = true;
                }
            }

            // Failed to communicate with Tilt Five plugin at some point, so don't know whether
            // an update is needed or not.  Just say no.
            return false;
        }

        /// <summary>
        /// Called when the GameObject is enabled.
        /// </summary>
        private void OnEnable()
        {
            try
            {
                NativePlugin.SetMaxDesiredGlasses((byte)GetSupportedPlayerCount());
            }
            catch (System.DllNotFoundException e)
            {
                Log.Info(
                    "Could not connect to Tilt Five plugin for setting max glasses: {0}",
                    e.Message);
            }
            catch (System.Exception e)
            {
                Log.Error(e.Message);
            }

            Glasses.Reset(PlayerIndex.None, glassesSettings);

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
            InputSystem.onBeforeUpdate += OnBeforeUpdate;
#endif
        }

        private void OnDisable()
        {
#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
            InputSystem.onBeforeUpdate -= OnBeforeUpdate;
#endif
            Player.OnDisable();
        }

        private void OnDestroy()
        {
            Player.OnDisable();
        }

        private void OnApplicationQuit()
        {
            OnDisable();
        }

        // There's a longstanding bug where UnityPluginUnload isn't called.
        // - https://forum.unity.com/threads/unitypluginunload-never-called.414066/
        // - https://gamedev.stackexchange.com/questions/200118/unity-native-plugin-unitypluginload-is-called-but-unitypluginunload-is-not
        // - https://issuetracker.unity3d.com/issues/unitypluginunload-is-never-called-in-a-standalone-build
        // Work around this by invoking it via Application.quitting.
        private static void Quit()
        {
            try
            {
                NativePlugin.UnloadWorkaround();
            }
            catch (System.DllNotFoundException)
            {
                // Nothing to report on quit if the plugin isn't present
            }
            catch (System.Exception e)
            {
                Log.Error(e.Message);
            }
        }

        [RuntimeInitializeOnLoadMethod]
        private static void RunOnStart()
        {
            Application.quitting += Quit;
        }

        private void RefreshSpectatorSettings()
        {
            // Warn developers if they've left the glassesSettings camera template field empty, since it's still required for the original TiltFiveManager
            if(glassesSettings.cameraTemplate == null)
            {
                Log.Warn("No camera template detected in TiltFiveManager's glassesSettings. A camera template is required.");
            }

            // We don't expose any global settings like SpectatorSettings in TiltFiveManager's custom inspector,
            // though they're still accessible from scripts.
            // Just synchronize spectatorSettings from TiltFiveManager's glassesSettings as needed.
            spectatorSettings.spectatorCamera = glassesSettings.cameraTemplate;
            spectatorSettings.glassesMirrorMode = glassesSettings.glassesMirrorMode;

            // Make sure that the spectated player isn't set to a player index higher than what TiltFiveManager supports
            var highestSupportedPlayer = (PlayerIndex)GetSupportedPlayerCount();
            if (spectatorSettings.spectatedPlayer > highestSupportedPlayer)
            {
                Log.Warn($"Invalid spectatorSettings.spectatedPlayer [{spectatorSettings.spectatedPlayer}]. TiltFiveManager only supports one player.");
                spectatorSettings.spectatedPlayer = highestSupportedPlayer;
            }
        }

        private void RefreshPlayerSettings()
        {
            /* In an initial implementation of TiltFiveManager's internal PlayerSettings object, we initialized
            * a new PlayerSettings in Awake() and set its internal settings objects to TiltFiveManager's internal settings objects.
            *
            * However, this introduced a bug. The settings values in TiltFiveManager's custom inspector couldn't be
            * modified when the editor was in play mode, which would be a fairly significant quality of life issue for developers.
            *
            * I'm a bit fuzzy on the underlying mechanism, but the issue seemed to be that in the TiltFiveManager's
            * custom inspector code, the SerializedProperty API (for GlassesSettings, WandSettings, ScaleSettings, etc)
            * couldn't apply edits to the underlying settings objects if they were owned/shared by multiple parent classes
            * (e.g. the same GlassesSettings can't be owned by both a TiltFiveManager and a PlayerSettings without
            * breaking SerializedProperty's ability to modify the GlassesSettings).
            *
            * So the fix was to stop sharing.
            * Instead of building a PlayerSettings internally on Awake() that uses TiltFiveManager's existing settings objects,
            * we build one that has its own unique internal settings objects, and any time an edit gets made,
            * OnValidate() does a shallow copy to those objects using RefreshPlayerSettings(). */

            if(playerSettings == null)
            {
                return;
            }
            playerSettings.PlayerIndex = PlayerIndex.One;

            if (glassesSettings != null)
            {
                playerSettings.glassesSettings = glassesSettings.Copy();
            }
            if (rightWandSettings != null)
            {
                playerSettings.rightWandSettings = rightWandSettings.Copy();
            }
            if (leftWandSettings != null)
            {
                playerSettings.leftWandSettings = leftWandSettings.Copy();
            }
            if (gameBoardSettings != null)
            {
                playerSettings.gameboardSettings = gameBoardSettings.Copy();
            }
            if (scaleSettings != null)
            {
                playerSettings.scaleSettings = scaleSettings.Copy();
            }
        }

#if UNITY_EDITOR

        /// <summary>
        /// <b>EDITOR-ONLY</b>
        /// </summary>
        void OnValidate()
        {
            Log.LogLevel = logSettings.level;
            Log.TAG = logSettings.TAG;

            if (scaleSettings != null)
            {
                scaleSettings.contentScaleRatio = Mathf.Clamp(scaleSettings.contentScaleRatio, ScaleSettings.MIN_CONTENT_SCALE_RATIO, float.MaxValue);
            }

            if (leftWandSettings != null)
            {
                leftWandSettings.controllerIndex = ControllerIndex.Left;
            }
            if (rightWandSettings != null)
            {
                rightWandSettings.controllerIndex = ControllerIndex.Right;
            }

            if (playerSettings != null)
            {
                RefreshPlayerSettings();
            }

            if (spectatorSettings != null)
            {
                RefreshSpectatorSettings();
            }
        }

        /// <summary>
        /// Draws Gizmos in the Editor Scene view.
        /// </summary>
        void OnDrawGizmos()
        {
            if (enabled && gameBoardSettings.currentGameBoard != null)
            {
                gameBoardSettings.currentGameBoard.DrawGizmo(scaleSettings, gameBoardSettings);
            }
        }

#endif

        #region ISceneInfo Implementation

        public float GetScaleToUWRLD_UGBD()
        {
            return scaleSettings.GetScaleToUWRLD_UGBD(gameBoardSettings.gameBoardScale);
        }

        public Pose GetGameboardPose()
        {
            return new Pose(gameBoardSettings.gameBoardCenter, Quaternion.Euler(gameBoardSettings.gameBoardRotation));
        }

        public Camera GetEyeCamera()
        {
            return Glasses.GetLeftEye(PlayerIndex.One);
        }

        public uint GetSupportedPlayerCount()
        {
            return 1;   // TODO: Change this if we decide to include spectators with TiltFiveManager
        }

        public bool IsActiveAndEnabled()
        {
            return isActiveAndEnabled;
        }

        #endregion ISceneInfo Implementation
    }

}
