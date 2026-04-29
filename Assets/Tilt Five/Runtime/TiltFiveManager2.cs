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
using System.Collections.Generic;
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
    public class TiltFiveManager2 : TiltFive.SingletonComponent<TiltFiveManager2>, ISceneInfo
    {
        /// <summary>
        /// The first player's runtime configuration data.
        /// </summary>
        public PlayerSettings playerOneSettings
        {
            get
            {
                if(allPlayerSettings[0] == null)
                {
                    allPlayerSettings[0] = new PlayerSettings() { PlayerIndex = PlayerIndex.One };
                }
                return allPlayerSettings[0];
            }
        }

        /// <summary>
        /// The second player's runtime configuration data.
        /// </summary>
        public PlayerSettings playerTwoSettings
        {
            get
            {
                if (allPlayerSettings[1] == null)
                {
                    allPlayerSettings[1] = new PlayerSettings() { PlayerIndex = PlayerIndex.Two };
                }
                return allPlayerSettings[1];
            }
        }

        /// <summary>
        /// The third player's runtime configuration data.
        /// </summary>
        public PlayerSettings playerThreeSettings
        {
            get
            {
                if (allPlayerSettings[2] == null)
                {
                    allPlayerSettings[2] = new PlayerSettings() { PlayerIndex = PlayerIndex.Three };
                }
                return allPlayerSettings[2];
            }
        }

        /// <summary>
        /// The fourth player's runtime configuration data.
        /// </summary>
        public PlayerSettings playerFourSettings
        {
            get
            {
                if (allPlayerSettings[3] == null)
                {
                    allPlayerSettings[3] = new PlayerSettings() { PlayerIndex = PlayerIndex.Four };
                }
                return allPlayerSettings[3];
            }
        }

        public PlayerSettings[] allPlayerSettings = new PlayerSettings[PlayerSettings.MAX_SUPPORTED_PLAYERS];

        public uint supportedPlayerCount = 3;

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
        public EditorSettings2 editorSettings = new EditorSettings2();
        public PlayerIndex selectedPlayer => editorSettings.selectedPlayer;

        private HashSet<GameBoard> renderedGameboards = new HashSet<GameBoard>();
#endif

        private bool needsDriverUpdateNotifiedOnce = false;
        private bool needsDriverUpdateErroredOnce = false;

        private static bool upgradeInProgress = false;

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
        /// <summary>
        /// This mapping correlates TiltFive Player Index values (as playerIndexMapping indices) to Unity Player Index
        /// values (as playerIndexMapping values).
        /// i.e. unityPlayerIndex = playerIndexMapping[t5PlayerIndex];
        /// </summary>
        private int[] playerIndexMapping = {0,1,2,3};
#endif

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

            // Initialize the player settings if necessary
            for (int i = 0; i < allPlayerSettings.Length; i++)
            {
                var currentPlayerSettings = allPlayerSettings[i];
                if(currentPlayerSettings == null)
                {
                    allPlayerSettings[i] = new PlayerSettings() { PlayerIndex = (PlayerIndex) i + 1 };
                }
            }
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
            Player.ScanForNewPlayers(); // Should only be executed once per frame
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
            Player.ScanForNewPlayers(); // Should only be executed once per frame
            Wand.GetLatestInputs();     // Should only be executed once per frame
#endif
            RefreshSpectatorSettings();
            Display.ApplyGraphicsSettings(graphicsSettings);

            for (int i = 0; i < supportedPlayerCount; i++)
            {
                var playerSettings = allPlayerSettings[i];
                if (playerSettings != null)
                {
                    Player.Update(playerSettings, spectatorSettings);
                }
            }

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
            for (int i = 0; i < supportedPlayerCount; i++)
            {
                var playerSettings = allPlayerSettings[i];
                if (playerSettings != null)
                {
                    Player.Update(playerSettings, spectatorSettings);
                }
            }
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
                        // Not incompatible. Reset the incompatibility warning.
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
        /// Gets the player settings for the specified player.
        /// </summary>
        /// <param name="playerIndex"></param>
        /// <param name="playerSettings"></param>
        /// <returns>Returns false and sets <paramref name="playerSettings"/> to null if an invalid player index is provided.</returns>
        public bool TryGetPlayerSettings(PlayerIndex playerIndex, out PlayerSettings playerSettings)
        {
            switch (playerIndex)
            {
                case PlayerIndex.One:
                    playerSettings = playerOneSettings;
                    return true;
                case PlayerIndex.Two:
                    playerSettings = playerTwoSettings;
                    return true;
                case PlayerIndex.Three:
                    playerSettings = playerThreeSettings;
                    return true;
                case PlayerIndex.Four:
                    playerSettings = playerFourSettings;
                    return true;
                default:
                    playerSettings = null;
                    return false;
            }
        }

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
        internal void RefreshInputDevicePairings()
        {
            foreach (WandDevice wand in Input.wandDevices)
            {
                PlayerInput playerInput = null;
                if (wand != null)
                {
                    playerInput = PlayerInput.GetPlayerByIndex(playerIndexMapping[(int)wand.playerIndex - 1]);
                    if (playerInput != null)
                    {
                        InputUser.PerformPairingWithDevice(wand, playerInput.user);
                    }
                }
            }
            foreach (GlassesDevice glasses in Input.glassesDevices)
            {
                PlayerInput playerInput = null;
                if(glasses != null)
                {
                    playerInput = PlayerInput.GetPlayerByIndex(playerIndexMapping[(int)glasses.PlayerIndex - 1]);
                    if (playerInput != null)
                    {
                        InputUser.PerformPairingWithDevice(glasses, playerInput.user);
                    }
                }
            }
        }

        internal void ReassignPlayerIndexMapping(int[] mapping)
        {
            if(mapping.Length != 4)
            {
                throw new System.ArgumentException("Invalid player index mapping argument - mapping should be 4 values long");
            }
            for (var i = 0; i < mapping.Length; i++)
            {
                if(mapping[i] < 0)
                {
                    throw new System.ArgumentException("Invalid player index mapping argument - mapping should contain positive values only");
                }
                for (var j = 0; j < i; j++)
                {
                    if (mapping[i] == mapping[j])
                    {
                        throw new System.ArgumentException("Invalid player index mapping argument - mapping should contain no duplicates");
                    }
                }
            }
            playerIndexMapping = mapping;
            RefreshInputDevicePairings();
        }
#endif //UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE



        /// <summary>
        /// Called when the GameObject is enabled.
        /// </summary>
        private void OnEnable()
        {
            try
            {
                // TODO: change this to something in the settings drawer once that exists
                NativePlugin.SetMaxDesiredGlasses((byte)GlassesSettings.MAX_SUPPORTED_GLASSES_COUNT);
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

            // Initialize the player settings if necessary
            for (int i = 0; i < allPlayerSettings.Length; i++)
            {
                var currentPlayerSettings = allPlayerSettings[i];
                if (currentPlayerSettings == null)
                {
                    allPlayerSettings[i] = new PlayerSettings() { PlayerIndex = (PlayerIndex)i + 1 };
                }
            }

            for (int i = 0; i < supportedPlayerCount; i++)
            {
                var playerSettings = allPlayerSettings[i];
                if (playerSettings != null)
                {
                    Player.Reset(playerSettings, spectatorSettings);
                }
            }

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
            InputSystem.onBeforeUpdate += OnBeforeUpdate;
#endif
        }

        private void OnDisable()
        {
#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
            InputSystem.onBeforeUpdate -= OnBeforeUpdate;
            //Input.OnDisable();
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
                // nothing to report on quit if the plugin isn't present
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
            // Warn developers if they've left the spectatorCamera field empty
            // TiltFiveManager2's custom inspector should already warn them in the editor, but this warns them again at runtime.
            if (spectatorSettings.spectatorCamera == null)
            {
                Log.Warn("No spectator camera detected in TiltFiveManager2's spectator settings. A spectator camera is required.");
            }

            // Make sure that the spectated player isn't set to a player index higher than what TiltFiveManager2 supports
            var highestSupportedPlayer = (PlayerIndex)supportedPlayerCount;
            if (spectatorSettings.spectatedPlayer > highestSupportedPlayer)
            {
                Log.Warn($"Invalid spectatorSettings.spectatedPlayer [{spectatorSettings.spectatedPlayer}]. TiltFiveManager2 currently only supports up to Player {highestSupportedPlayer}.");
                spectatorSettings.spectatedPlayer = highestSupportedPlayer;
            }
        }

#if UNITY_EDITOR

        /// <summary>
        /// <b>EDITOR-ONLY</b>
        /// </summary>
        void OnValidate()
        {
            // Don't do any validation if we're in the middle of copying settings.
            if(upgradeInProgress)
            {
                return;
            }

            Log.LogLevel = logSettings.level;
            Log.TAG = logSettings.TAG;

            playerOneSettings.PlayerIndex = PlayerIndex.One;
            playerTwoSettings.PlayerIndex = PlayerIndex.Two;
            playerThreeSettings.PlayerIndex = PlayerIndex.Three;
            playerFourSettings.PlayerIndex = PlayerIndex.Four;

            supportedPlayerCount = (uint) Mathf.Clamp(supportedPlayerCount, 1, PlayerSettings.MAX_SUPPORTED_PLAYERS);

            for (int i = 0; i < allPlayerSettings.Length; i++)
            {
                var playerSettings = allPlayerSettings[i];
                if (playerSettings != null)
                {
                    Player.Validate(playerSettings);
                    playerSettings.glassesSettings.glassesMirrorMode = spectatorSettings.glassesMirrorMode;
                }
            }
            RefreshSpectatorSettings();
        }

        /// <summary>
        /// Draws Gizmos in the Editor Scene view.
        /// </summary>
        void OnDrawGizmos()
        {
            if (!enabled)
            {
                return;
            }

            renderedGameboards.Clear();

            for (int i = 0; i < supportedPlayerCount; i++)
            {
                var playerSettings = allPlayerSettings[i];
                if (playerSettings != null)
                {
                    var currentGameboard = playerSettings.gameboardSettings.currentGameBoard;
                    if (!renderedGameboards.Contains(currentGameboard))
                    {
                        renderedGameboards.Add(currentGameboard);
                        Player.DrawGizmos(playerSettings);
                    }
                }
            }
        }

        public static void CreateFromTiltFiveManager(TiltFiveManager tiltFiveManager)
        {
            var parentGameObject = tiltFiveManager.gameObject;

            // Ideally, we only want one TiltFiveManager2 in the scene.
            // If the developer clicks the upgrade button repeatedly, we don't want to keep creating more of them.
            // In this scenario, ask the developer whether they'd like to overwrite the settings on the existing
            // TiltFiveManager2 with the TiltFiveManager's settings.
            var isTiltFiveManager2AlreadyPresent = parentGameObject.TryGetComponent<TiltFiveManager2>(out var existingTiltFiveManager2);
            var confirmationDialogTitle = "Existing TiltFiveManager2 detected";
            var confirmationDialogText = $"The GameObject \"{parentGameObject.name}\" already has a TiltFiveManager2 component." +
                System.Environment.NewLine + System.Environment.NewLine +
                "Overwrite the existing TiltFiveManager2 component values?" +
                System.Environment.NewLine + System.Environment.NewLine +
                "Warning: This cannot be undone via Edit > Undo (Ctrl+Z)";
            var confirmButtonLabel = "Overwrite";
            var cancelButtonLabel = "Cancel";
            var overwriteExistingTiltFiveManager2 = isTiltFiveManager2AlreadyPresent
                && UnityEditor.EditorUtility.DisplayDialog(confirmationDialogTitle, confirmationDialogText, confirmButtonLabel, cancelButtonLabel);

            if(isTiltFiveManager2AlreadyPresent && !overwriteExistingTiltFiveManager2)
            {
                Debug.Log($"Aborted attempt to upgrade TiltFiveManager.");
                return;
            }

            upgradeInProgress = true;

            TiltFiveManager2 tiltFiveManager2 = overwriteExistingTiltFiveManager2
                ? existingTiltFiveManager2
                : parentGameObject.AddComponent<TiltFiveManager2>();

            // Disable the old TiltFiveManager.
            tiltFiveManager.enabled = false;

            // Default to supporting a single player, just like TiltFiveManager did.
            tiltFiveManager2.supportedPlayerCount = 1;

            // Copy the various settings objects from TiltFiveManager to playerOneSettings.
            tiltFiveManager2.playerOneSettings.glassesSettings = tiltFiveManager.glassesSettings.Copy();

            tiltFiveManager2.playerOneSettings.scaleSettings = tiltFiveManager.scaleSettings.Copy();

            tiltFiveManager2.playerOneSettings.gameboardSettings = tiltFiveManager.gameBoardSettings.Copy();

            tiltFiveManager2.playerOneSettings.leftWandSettings = tiltFiveManager.leftWandSettings.Copy();
            tiltFiveManager2.playerOneSettings.rightWandSettings = tiltFiveManager.rightWandSettings.Copy();

            // Emulate TiltFiveManager, which used a single camera internally for eye camera cloning and onscreen previews
            tiltFiveManager2.spectatorSettings.spectatorCamera = tiltFiveManager.glassesSettings.cameraTemplate;

            // Copy TiltFiveManager's GlassesSettings' mirror mode, which has moved to SpectatorSettings for TiltFiveManager2
            tiltFiveManager2.spectatorSettings.glassesMirrorMode = tiltFiveManager.glassesSettings.glassesMirrorMode;

            // For the sake of thoroughness, let's copy the old log settings, too.
            tiltFiveManager2.logSettings = tiltFiveManager.logSettings.Copy();

            upgradeInProgress = false;

            var resultText = overwriteExistingTiltFiveManager2
                ? $"Successfully overwrote component values on the existing TiltFiveManager2 component attached to \"{parentGameObject.name}\" using the old TiltFiveManager component values."
                : $"Successfully attached a new TiltFiveManager2 component to \"{parentGameObject.name}\" and imported the old TiltFiveManager component values.";
            Debug.Log($"{resultText}{System.Environment.NewLine}The old TiltFiveManager has been disabled - it can safely be removed.");
        }

#endif

        #region ISceneInfo Implementation

        public float GetScaleToUWRLD_UGBD()
        {
            return playerOneSettings.scaleSettings.GetScaleToUWRLD_UGBD(playerOneSettings.gameboardSettings.gameBoardScale);
        }

        public Pose GetGameboardPose()
        {
            return new Pose(playerOneSettings.gameboardSettings.gameBoardCenter, Quaternion.Euler(playerOneSettings.gameboardSettings.gameBoardRotation));
        }

        public Camera GetEyeCamera()
        {
            return Glasses.GetLeftEye(PlayerIndex.One);
        }

        public uint GetSupportedPlayerCount()
        {
            return supportedPlayerCount;
        }

        public bool IsActiveAndEnabled()
        {
            return isActiveAndEnabled;
        }

        #endregion ISceneInfo Implementation
    }

}
