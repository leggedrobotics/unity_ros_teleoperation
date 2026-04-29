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
using System.Collections.Generic;
using UnityEngine;

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

using TiltFive;
using TiltFive.Logging;

namespace TiltFive
{
    /// <summary>
    /// The Glasses API and runtime.
    /// </summary>
    public sealed class Glasses : Singleton<Glasses>
    {
        #region Private Fields

        /// <summary>
        /// The glasses core runtimes.
        /// </summary>
        private Dictionary<GlassesHandle, GlassesCore> glassesCores = new Dictionary<GlassesHandle, GlassesCore>();

        private HashSet<GlassesHandle> incomingHandles = new HashSet<GlassesHandle>();
        private HashSet<GlassesHandle> lostHandles = new HashSet<GlassesHandle>();

        /// <summary>
        /// The identifier for the first detected pair of glasses.
        /// </summary>
        private GlassesHandle? defaultGlassesHandle;

        #endregion


        #region public Enums

        public enum AREyes
        {
            EYE_LEFT = 0,
            EYE_RIGHT,
            EYE_MAX,
        }

        #endregion


        #region Public Fields

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:TiltFive.Glasses"/> is updated.
        /// </summary>
        /// <value><c>true</c> if player 1's glasses are connected and their glasses pose has been updated;
        /// otherwise, <c>false</c>.</value>
        [Obsolete("Glasses.updated is deprecated. Please use Glasses.IsTracked() instead.")]
        public static bool updated => headPoseUpdated(PlayerIndex.One);
        /// <summary>
        /// Gets a value indicating whether this <see cref="T:TiltFive.Glasses"/> is configured.
        /// </summary>
        /// <value><c>true</c> if player 1's glasses are connected and they've been configured;
        /// otherwise, <c>false</c>.</value>
        [Obsolete("Glasses.configured is deprecated. Please use Player.IsConnected() instead.")]
        public static bool configured => GetPlayerOneGlassesCore()?.configured ?? false;
        /// <summary>
        /// Gets the head pose position.
        /// </summary>
        /// <value>The position of player 1's glasses (if connected, otherwise a zero vector)</value>
        [Obsolete("Glasses.position is deprecated. Please use Glasses.TryGetPose() and Pose.position instead.")]
        public static Vector3 position => GetPlayerOneGlassesCore()?.Pose_UnityWorldSpace.position ?? Vector3.zero;
        /// <summary>
        /// Gets the head pose rotation.
        /// </summary>
        /// <value>The rotation of player 1's glasses (if connected, otherwise the identity quaternion)</value>
        [Obsolete("Glasses.rotation is deprecated. Please use Glasses.TryGetPose() and Pose.rotation instead.")]
        public static Quaternion rotation => GetPlayerOneGlassesCore()?.Pose_UnityWorldSpace.rotation ?? Quaternion.identity;
        /// <summary>
        /// Gets the head orientation's forward vector.
        /// </summary>
        /// <value>The forward vector of player 1's glasses (if connected, otherwise the default forward vector)</value>
        [Obsolete("Glasses.forward is deprecated. Please use Glasses.TryGetPose() and Pose.forward instead.")]
        public static Vector3 forward => GetPlayerOneGlassesCore()?.Pose_UnityWorldSpace.forward ?? Vector3.forward;
        /// <summary>
        /// Gets the head orientation's right vector.
        /// </summary>
        /// <value>The right vector of player 1's glasses (if connected, otherwise the default right vector)</value>
        [Obsolete("Glasses.right is deprecated. Please use Glasses.TryGetPose() and Pose.right instead.")]
        public static Vector3 right => GetPlayerOneGlassesCore()?.Pose_UnityWorldSpace.right ?? Vector3.right;
        /// <summary>
        /// Gets the head orientation's up vector.
        /// </summary>
        /// <value>The up vector of player 1's glasses (if connected, otherwise the default up vector)</value>
        [Obsolete("Glasses.up is deprecated. Please use Glasses.TryGetPose() and Pose.up instead.")]
        public static Vector3 up => GetPlayerOneGlassesCore()?.Pose_UnityWorldSpace.up ?? Vector3.up;

        /// <summary>
        /// Gets the left eye position.
        /// </summary>
        /// <value>The left eye position of player 1's glasses (if connected, otherwise the zero vector)</value>
        [Obsolete("Glasses.leftEyePosition is deprecated. Please use Glasses.GetLeftEye() and Transform.position")]
        public static Vector3 leftEyePosition => GetPlayerOneGlassesCore()?.eyePositions[AREyes.EYE_LEFT] ?? Vector3.zero;
        /// <summary>
        /// Gets the right eye position.
        /// </summary>
        /// <value>The right eye position of player 1's glasses (if connected, otherwise the zero vector)</value>
        [Obsolete("Glasses.rightEyePosition is deprecated. Please use Glasses.GetRightEye() and Transform.position")]
        public static Vector3 rightEyePosition => GetPlayerOneGlassesCore()?.eyePositions[AREyes.EYE_RIGHT] ?? Vector3.zero;

        /// <summary>
        /// Indicates whether the glasses are plugged in and functioning.
        /// </summary>
        [Obsolete("Glasses.glassesAvailable is deprecated. Please use Player.IsConnected() instead.")]
        public static bool glassesAvailable { get; private set; }

        #endregion Public Fields


        #region Public Functions

        /// <summary>
        /// Indicate if the specified glasses are tracked.
        /// </summary>
        /// <returns><c>true</c> if the glasses are tracked, <c>false</c> otherwise.</returns>
        /// <param name="playerIndex">If not provided, the Player 1's glasses are checked.</param>
        public static bool IsTracked(PlayerIndex playerIndex = PlayerIndex.One)
        {
            return Player.TryGetGlassesHandle(playerIndex, out var glassesHandle)
                && IsTracked(glassesHandle);
        }

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
        /// <summary>
        /// Attempts to get the GlassesDevice corresponding to the specfied player.
        /// </summary>
        /// <param name="playerIndex"></param>
        /// <param name="glassesDevice"></param>
        /// <returns>
        /// Returns <c>false</c> along with a null <paramref name="glassesDevice"/> if the GlassesDevice could not be obtained for the specified player
        /// (e.g. if the specified player is not connected).
        /// </returns>
        public static bool TryGetGlassesDevice(PlayerIndex playerIndex, out GlassesDevice glassesDevice)
        {
            if(!Player.TryGetGlassesHandle(playerIndex, out var glassesHandle))
            {
                glassesDevice = null;
                return false;
            }

            return TryGetGlassesDevice(glassesHandle, out glassesDevice);
        }
#endif // UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE

        /// <summary>
        /// Gets the friendly name associated with the specified player's glasses in the Tilt Five Control Panel.
        /// </summary>
        /// <param name="playerIndex"></param>
        /// <param name="friendlyName"></param>
        /// <returns>Returns <c>false</c> and sets <paramref name="friendlyName"/> to null if there was a problem
        /// getting the friendly name for this player; otherwise true. This can happen if the provided PlayerIndex is invalid,
        /// the player is not connected, or if the user hasn't yet set a friendly name for this particular set of glasses.</returns>
        public static bool TryGetFriendlyName(PlayerIndex playerIndex, out string friendlyName)
        {
            if(!Player.TryGetGlassesHandle(playerIndex, out var glassesHandle))
            {
                friendlyName = null;
                return false;
            }

            return TryGetFriendlyName(glassesHandle, out friendlyName);
        }

#if TILT_FIVE_ENABLE_PROJECTOR_EXTRINSICS_ADJUSTMENTS
        public static bool TrySetProjectorExtrinsicsAdjustment(PlayerIndex playerIndex, float[] args)
        {
            if (!Player.TryGetGlassesHandle(playerIndex, out var glassesHandle))
            {
                return false;
            }
            return TrySetProjectorExtrinsicsAdjustment(glassesHandle, args);
        }

        public static bool SetSingleEyeMode(PlayerIndex playerIndex, AREyes eye)
        {
            if (!Player.TryGetGlassesHandle(playerIndex, out var glassesHandle))
            {
                return false;
            }
            return SetSingleEyeMode(glassesHandle, eye);
        }
#endif // TILT_FIVE_ENABLE_PROJECTOR_EXTRINSICS_ADJUSTMENTS

        /// <summary>
        /// Attempts to get the position and orientation of the specified player's glasses.
        /// </summary>
        /// <param name="playerIndex"></param>
        /// <param name="pose"></param>
        /// <returns>Returns <c>false</c> along with an empty pose if something goes wrong.</returns>
        public static bool TryGetPose(PlayerIndex playerIndex, out Pose pose)
        {
            if(Player.TryGetGlassesHandle(playerIndex, out var glassesHandle) && Instance.glassesCores.TryGetValue(glassesHandle, out var glassesCore))
            {
                pose = glassesCore.Pose_UnityWorldSpace;
                return true;
            }
            pose = new Pose();
            return false;
        }

        /// <summary>
        /// Attempts to get the position and orientation of the specified player's glasses, smoothed
        /// for on-screen preview.
        /// </summary>
        /// <param name="playerIndex"></param>
        /// <param name="pose"></param>
        /// <returns>Returns <c>false</c> along with an empty pose if something goes wrong.</returns>
        public static bool TryGetPreviewPose(PlayerIndex playerIndex, out Pose pose)
        {
            if (Player.TryGetGlassesHandle(playerIndex, out var glassesHandle) && Instance.glassesCores.TryGetValue(glassesHandle, out var glassesCore))
            {
                pose = glassesCore.previewCore.Pose_UnityWorldSpace;
                return true;
            }
            pose = new Pose();
            return false;
        }

        /// <summary>
        /// Gets the pose root GameObject for the specified player.
        /// </summary>
        /// <remarks>
        /// This GameObject's pose is driven by the player's head position over the gameboard.
        /// Parented underneath this GameObject are the two eye cameras and an instance of
        /// the developer-provided prefab in the player's Glasses settings.
        /// </remarks>
        /// <param name="playerIndex"></param>
        /// <returns>Returns <c>null</c> if the specified player is not connected.</returns>
        public static GameObject GetPoseRoot(PlayerIndex playerIndex)
        {
            if(!Player.TryGetGlassesHandle(playerIndex, out var glassesHandle))
            {
                return null;
            }
            return GetPoseRoot(glassesHandle);
        }

        /// <summary>
        /// Gets the left eye camera for the specified player's glasses.
        /// </summary>
        /// <param name="playerIndex"></param>
        /// <returns>Returns <c>null</c> if the specified player is not connected.</returns>
        public static Camera GetLeftEye(PlayerIndex playerIndex)
        {
            if (!Player.TryGetGlassesHandle(playerIndex, out var glassesHandle))
            {
                return null;
            }
            return GetLeftEye(glassesHandle);
        }

        /// <summary>
        /// Gets the right eye camera for the specified player's glasses.
        /// </summary>
        /// <param name="playerIndex"></param>
        /// <returns>Returns <c>null</c> if the specified player is not connected.</returns>
        public static Camera GetRightEye(PlayerIndex playerIndex)
        {
            if (!Player.TryGetGlassesHandle(playerIndex, out var glassesHandle))
            {
                return null;
            }
            return GetRightEye(glassesHandle);
        }

        #region Deprecated Public Functions

        /// <summary>
        /// Returns a boolean indication that the head pose was successfully
        /// updated.
        /// </summary>
        /// <returns><c>true</c>, if the head pose was updated, <c>false</c> otherwise.</returns>
        /// <param name="glassesHandle">The specified glasses. If null is provided, this uses the default glasses.</param>
        [Obsolete("Glasses.headPoseUpdated is deprecated. Please use Glasses.IsTracked() instead")]
        public static bool headPoseUpdated(PlayerIndex playerIndex = PlayerIndex.One)
        {
            if (playerIndex == PlayerIndex.None || !Player.TryGetGlassesHandle(playerIndex, out var glassesHandle))
            {
                return false;
            }

            return Instance.glassesCores.TryGetValue(glassesHandle, out var glassesCore)
                && glassesCore.IsTracked;
        }

        /// <summary>
        /// Reset this <see cref="T:TiltFive.Glasses"/>.
        /// </summary>
        /// <param name="glassesSettings">Glasses settings for configuring the specified glassesCore.</param>
        /// <param name="playerIndex">The specified player. If None is provided, this resets all glasses.</param>
        [Obsolete("Converted to be an internal function.")]
        public static void Reset(GlassesSettings glassesSettings, SpectatorSettings spectatorSettings = null, PlayerIndex playerIndex = PlayerIndex.None)
        {
            Reset(playerIndex, glassesSettings, spectatorSettings);
        }

        /// <summary>
        /// Validates the specified glassesSettings with the specified glasses core.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if the glasses core is valid with the given settings,
        ///     <c>false</c> otherwise.
        /// </returns>
        /// <param name="glassesSettings">Glasses settings.</param>
        /// <param name="playerIndex">The specified glasses to validate. If None is provided, this uses the default glasses.</param>
        [Obsolete("Converted to be an internal function.")]
        public static bool Validate(GlassesSettings glassesSettings, SpectatorSettings spectatorSettings = null, PlayerIndex playerIndex = PlayerIndex.One)
        {
            return Validate(playerIndex, glassesSettings, spectatorSettings);
        }

        /// <summary>
        /// Updates this <see cref="T:TiltFive.Glasses"/>.
        /// </summary>
        /// <param name="glassesSettings">Glasses settings for the update.</param>
        [Obsolete("Converted to be an internal function.")]
        public static void Update(GlassesSettings glassesSettings, ScaleSettings scaleSettings, GameBoardSettings gameBoardSettings)
        {
            UpdateAllGlassesCores(glassesSettings, scaleSettings, gameBoardSettings);
        }

        /// <summary>
        /// Indicate if the specified glasses are connected.
        /// </summary>
        /// <returns><c>true</c> if the glasses are connected, <c>false</c> otherwise.</returns>
        /// <param name="glassesHandle">Glasses handle to check.</param>
        [Obsolete("Deprecated in favor of Player.IsConnected().")]
        public static bool IsConnected(PlayerIndex playerIndex = PlayerIndex.One)
        {
            return Player.TryGetGlassesHandle(playerIndex, out var glassesHandle)
                && IsConnected(glassesHandle);
        }

        [Obsolete("Converted to be an internal function.")]
        public static void ScanForGlasses() { Scan(); }

        #endregion Deprecated Public Functions


        #endregion Public Functions


        #region Internal Functions

        internal static void Scan()
        {
            // Enumerate the available glasses provided by the native plugin
            UInt64[] glassesHandles = new UInt64[GlassesSettings.MAX_SUPPORTED_GLASSES_COUNT];
            Debug.Assert(glassesHandles.Length <= Byte.MaxValue);
            byte glassesCount = (byte)glassesHandles.Length;
            int result = NativePlugin.T5_RESULT_SUCCESS;

            try
            {
                var refreshResult = NativePlugin.RefreshGlassesAvailable();
                result = NativePlugin.GetGlassesHandles(ref glassesCount, glassesHandles);
            }
            catch (System.Exception e)
            {
                Log.Error($"Error getting glasses handles: {e.Message}");
            }

            // Add/Remove entries from the glassesCores dictionary
            var glassesCores = Instance.glassesCores;
            var incomingHandles = Instance.incomingHandles;
            var lostHandles = Instance.lostHandles;

            incomingHandles.Clear();
            lostHandles.Clear();

            // If we ran into a problem getting the glasses handles, all bets are off — just tear down the now-useless glasses cores.
            if (result != NativePlugin.T5_RESULT_SUCCESS)
            {
                foreach (var keyValuePair in glassesCores)
                {
                    var lostHandle = keyValuePair.Key;
                    var glassesCore = keyValuePair.Value;

                    glassesCore.Dispose();
                    lostHandles.Add(lostHandle);
                }
                glassesCores.Clear();
            }
            else
            {
                // Add newly connected glasses
                for (int i = 0; i < glassesCount; i++)
                {
                    incomingHandles.Add(glassesHandles[i]);

                    // If we don't already have a glassesCore for this handle,
                    // and we still have an available player slot, then create a glassesCore.
                    if (!glassesCores.ContainsKey(glassesHandles[i]) && !Player.AllSupportedPlayersConnected())
                    {
#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
                        glassesCores[glassesHandles[i]] = new GlassesDeviceCore(glassesHandles[i]);
#else
                        glassesCores[glassesHandles[i]] = new GlassesCore(glassesHandles[i]);
#endif
                    }
                }

                // Prune disconnected glasses
                foreach (var currentHandle in glassesCores.Keys)
                {
                    if (!incomingHandles.Contains(currentHandle))
                    {
                        lostHandles.Add(currentHandle);
                    }
                }
                foreach (var lostHandle in lostHandles)
                {
                    glassesCores[lostHandle].Dispose();
                    glassesCores.Remove(lostHandle);
                }
            }

            // If we don't have a default glasses ID yet, assign the first one we got from the native plugin as the default.
            if (!Instance.defaultGlassesHandle.HasValue && result == 0 && glassesCount > 0)
            {
                Instance.defaultGlassesHandle = glassesHandles[0];
            }

            // If defaultGlassesId isn't valid anymore, reset its value.
            if (Instance.defaultGlassesHandle.HasValue && !glassesCores.ContainsKey(Instance.defaultGlassesHandle.Value))
            {
                Instance.defaultGlassesHandle = null;
            }
        }

        internal static void Reset(PlayerIndex playerIndex, GlassesSettings glassesSettings, SpectatorSettings spectatorSettings = null)
        {
            if (spectatorSettings == null && !TryGetSpectatorSettings(out spectatorSettings))
            {
                Log.Error("Glasses.Reset() could not find any spectator settings.");
                return;
            }

            // If playerIndex is none, reset all glasses
            if (playerIndex == PlayerIndex.None)
            {
                foreach (var glassesCore in Instance.glassesCores.Values)
                {
                    glassesCore.Reset(glassesSettings, spectatorSettings);
                }
                return;
            }

            if (Player.TryGetGlassesHandle(playerIndex, out var glassesHandle))
            {
                Reset(glassesSettings, spectatorSettings, glassesHandle);
            }
        }

        internal static void Reset(GlassesSettings glassesSettings, SpectatorSettings spectatorSettings, GlassesHandle glassesHandle)
        {
            if (Instance.glassesCores.TryGetValue(glassesHandle, out var glassesCore) && spectatorSettings != null)
            {
                glassesCore.Reset(glassesSettings, spectatorSettings);
            }
        }

        internal static bool Validate(PlayerIndex playerIndex, GlassesSettings glassesSettings, SpectatorSettings spectatorSettings = null)
        {
            if (!Player.TryGetGlassesHandle(playerIndex, out var glassesHandle))
            {
                return false;
            }

            if (spectatorSettings == null && !TryGetSpectatorSettings(out spectatorSettings))
            {
                return false;
            }

            return Validate(glassesSettings, spectatorSettings, glassesHandle);
        }

        internal static bool Validate(GlassesSettings glassesSettings, SpectatorSettings spectatorSettings, GlassesHandle glassesHandle)
        {
            return Instance.glassesCores.TryGetValue(glassesHandle, out var glassesCore)
                && glassesCore.Validate(glassesSettings, spectatorSettings);
        }

        internal static void UpdateAllGlassesCores(GlassesSettings glassesSettings, ScaleSettings scaleSettings, GameBoardSettings gameBoardSettings)
        {
            if (!TryGetSpectatorSettings(out var spectatorSettings))
            {
                return;
            }

            // Update the glasses cores
            foreach (var glassesCore in Instance.glassesCores.Values)
            {
                glassesCore.Update(glassesSettings, scaleSettings, gameBoardSettings, spectatorSettings);
            }
        }

        internal static void Update(GlassesHandle glassesHandle, GlassesSettings glassesSettings, ScaleSettings scaleSettings,
            GameBoardSettings gameBoardSettings, SpectatorSettings spectatorSettings)
        {
            if (Instance.glassesCores.TryGetValue(glassesHandle, out var glassesCore))
            {
                glassesCore.Update(glassesSettings, scaleSettings, gameBoardSettings, spectatorSettings);
            }
        }

        internal static bool IsTracked(GlassesHandle glassesHandle)
        {
            return Instance.glassesCores.TryGetValue(glassesHandle, out var glassesCore)
                && glassesCore.IsTracked;
        }

        internal static bool IsConnected(GlassesHandle glassesHandle)
        {
            return Instance.glassesCores.ContainsKey(glassesHandle);
        }

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
        internal static bool TryGetGlassesDevice(GlassesHandle glassesHandle, out GlassesDevice glassesDevice)
        {
            if(!Instance.glassesCores.TryGetValue(glassesHandle, out var glassesCore)
                || !(glassesCore is GlassesDeviceCore glassesDeviceCore))
            {
                glassesDevice = null;
                return false;
            }

            glassesDevice = glassesDeviceCore.glassesDevice;
            return true;
        }
#endif // UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE

        internal static bool TryGetFriendlyName(GlassesHandle glassesHandle, out string friendlyName)
        {
            if(!Instance.glassesCores.TryGetValue(glassesHandle, out var glassesCore))
            {
                friendlyName = null;
                return false;
            }

            return glassesCore.TryGetFriendlyName(out friendlyName);
        }

#if TILT_FIVE_ENABLE_PROJECTOR_EXTRINSICS_ADJUSTMENTS
        internal static bool TrySetProjectorExtrinsicsAdjustment(GlassesHandle glassesHandle, float[] args)
        {
            if (!Instance.glassesCores.TryGetValue(glassesHandle, out var glassesCore))
            {
                return false;
            }
            return glassesCore.TrySetProjectorExtrinsicsAdjustment(args);
        }

        internal static bool SetSingleEyeMode(GlassesHandle glassesHandle, AREyes eye)
        {
            if (!Instance.glassesCores.TryGetValue(glassesHandle, out var glassesCore))
            {
                return false;
            }
            return glassesCore.SetSingleEyeMode(eye);
        }
#endif // TILT_FIVE_ENABLE_PROJECTOR_EXTRINSICS_ADJUSTMENTS

        /// <summary>
        /// Get handle for the default glasses. Typically, the first glasses plugged in will become
        /// the default glasses, at which point this function will return its handle.
        /// </summary>
        /// <returns>GlassesHandle corresponding to the default glasses if present, otherwise null.
        /// </returns>
        internal static GlassesHandle? GetDefaultGlassesHandle()
        {
            var defaultGlassesHandle = Instance.defaultGlassesHandle;
            return defaultGlassesHandle.HasValue && Instance.glassesCores.ContainsKey(defaultGlassesHandle.Value)
                ? defaultGlassesHandle
                : null;
        }

        internal static void SetDefaultGlassesHandle(GlassesHandle glassesHandle)
        {
            Instance.defaultGlassesHandle = glassesHandle;
        }

        internal static GlassesHandle[] GetAllConnectedGlassesHandles()
        {
            var keys = Instance.glassesCores.Keys;
            GlassesHandle[] glassesHandles = new GlassesHandle[keys.Count];
            keys.CopyTo(glassesHandles, 0);
            return glassesHandles;
        }

        internal static void OnDisable()
        {
            foreach (var glassesCore in Instance.glassesCores.Values)
            {
                glassesCore.Dispose();
            }
            Instance.glassesCores.Clear();
        }

        #endregion Internal Functions

        #region Private Functions

        private static GameObject GetPoseRoot(GlassesHandle glassesHandle)
        {
            if (!Instance.glassesCores.TryGetValue(glassesHandle, out var glassesCore))
            {
                return null;
            }
            return glassesCore.headPose.gameObject;
        }

        private static Camera GetLeftEye(GlassesHandle glassesHandle)
        {
            if (!Instance.glassesCores.TryGetValue(glassesHandle, out var glassesCore))
            {
                return null;
            }
            return glassesCore.leftEye;
        }

        private static Camera GetRightEye(GlassesHandle glassesHandle)
        {
            if (!Instance.glassesCores.TryGetValue(glassesHandle, out var glassesCore))
            {
                return null;
            }

            return glassesCore.rightEye;
        }

        private static GlassesCore GetPlayerOneGlassesCore()
        {
            if(Player.TryGetGlassesHandle(PlayerIndex.One, out var glassesHandle)
                && Instance.glassesCores.TryGetValue(glassesHandle, out var glassesCore))
            {
                return glassesCore;
            }
            return null;
        }

        private static bool TryGetSpectatorSettings(out SpectatorSettings spectatorSettings)
        {
            if (TiltFiveSingletonHelper.TryGetISceneInfo(out var sceneInfo))
            {
                if (sceneInfo is TiltFiveManager2 tiltFiveManager2)
                {
                    spectatorSettings = tiltFiveManager2.spectatorSettings;
                    return true;
                }

                if (sceneInfo is TiltFiveManager tiltFiveManager)
                {
                    spectatorSettings = tiltFiveManager.spectatorSettings;
                    return true;
                }
            }

            spectatorSettings = null;
            return false;
        }

        #endregion

        private class BaseGlassesCore : TrackableCore<GlassesSettings, T5_GlassesPose>, IDisposable
        {
            public GlassesHandle glassesHandle;

            public GameObject headPoseRoot;
            public Transform headPose => headPoseRoot.transform;

            private T5_GlassesPoseUsage glassesPoseUsage;

            /// <summary>
            /// The default position of the glasses relative to the board.
            /// </summary>
            /// <remarks>
            /// The glasses camera will snap back to this position if the glasses are unavailable.
            /// If different behavior is desired in this scenario, a different camera should be used.
            /// </remarks>
            private readonly Vector3 DEFAULT_GLASSES_POSITION_GAME_BOARD_SPACE = new Vector3(0f, 0.5f, -0.5f);

            /// <summary>
            /// The default rotation of the glasses relative to the board.
            /// </summary>
            /// <remarks>
            /// The glasses camera will snap back to this rotation if the glasses are unavailable.
            /// If different behavior is desired in this scenario, a different camera should be used.
            /// </remarks>
            private readonly Quaternion DEFAULT_GLASSES_ROTATION_GAME_BOARD_SPACE = Quaternion.Euler(new Vector3(-45f, 0f, 0f));

            public BaseGlassesCore(
                GlassesHandle glassesHandle,
                T5_GlassesPoseUsage glassesPoseUsage,
                string name)
            {
                this.glassesHandle = glassesHandle;
                this.glassesPoseUsage = glassesPoseUsage;
                headPoseRoot = new GameObject(name);
            }

            public virtual void Dispose()
            {
                GameObject.Destroy(headPoseRoot);
            }

            /// <summary>
            /// Updates this <see cref="T:TiltFive.Glasses.BaseGlassesCore"/>
            /// </summary>
            /// <param name="glassesSettings">Glasses settings for the update.</param>
            /// <param name="scaleSettings">Scale settings for the update.</param>
            /// <param name="gameBoardSettings">Gameboard settings for the update.</param>
            protected override void Update(
                GlassesSettings glassesSettings,
                ScaleSettings scaleSettings,
                GameBoardSettings gameBoardSettings)
            {
                // Obtain the latest glasses pose.
                base.Update(glassesSettings, scaleSettings, gameBoardSettings);
            }

            protected override void SetDefaultPoseGameboardSpace(GlassesSettings settings)
            {
                pose_UGBD = new Pose(
                    DEFAULT_GLASSES_POSITION_GAME_BOARD_SPACE,
                    DEFAULT_GLASSES_ROTATION_GAME_BOARD_SPACE);
            }

            protected override void SetPoseUnityWorldSpace(
                ScaleSettings scaleSettings,
                GameBoardSettings gameBoardSettings)
            {
                pose_UWRLD = GameboardToWorldSpace(pose_UGBD, scaleSettings, gameBoardSettings);
            }

            protected override bool TryCheckConnected(out bool connected)
            {
                connected = IsConnected(glassesHandle);
                return connected;
            }

            protected override bool TryGetStateFromPlugin(
                out T5_GlassesPose glassesPose,
                out bool poseIsValid,
                GameBoardSettings gameBoardSettings)
            {
                T5_GlassesPose glassesPoseResult = new T5_GlassesPose { };

                int result = NativePlugin.T5_RESULT_UNKNOWN_ERROR;
                try
                {
                    result = NativePlugin.GetGlassesPose(
                        glassesHandle,
                        ref glassesPoseResult,
                        glassesPoseUsage);
                }
                catch (System.Exception e)
                {
                    Log.Error(e.Message);
                }

                if (result == NativePlugin.T5_RESULT_SUCCESS)
                {
                  GameBoard.SetGameboardType(glassesHandle, glassesPoseResult.GameboardType);
                }
                else
                {
                  GameBoard.SetGameboardType(glassesHandle, GameboardType.GameboardType_None);
                }

                poseIsValid = result == NativePlugin.T5_RESULT_SUCCESS && glassesPoseResult.GameboardType != GameboardType.GameboardType_None;
                glassesPose = glassesPoseResult;
                return result == NativePlugin.T5_RESULT_SUCCESS;
            }

            protected override void SetPoseGameboardSpace(
                in T5_GlassesPose glassesPose,
                GlassesSettings glassesSettings,
                ScaleSettings scaleSettings,
                GameBoardSettings gameboardSettings)
            {
                pose_UGBD = GetPoseGameboardSpace(glassesPose);
            }

            protected override void SetInvalidPoseGameboardSpace(
                in T5_GlassesPose glassesPose,
                GlassesSettings settings,
                ScaleSettings scaleSettings,
                GameBoardSettings gameboardSettings)
            {
                var newPose_GameboardSpace = GetPoseGameboardSpace(glassesPose);

                if (!isTracked && settings.RejectUntrackedPositionData)
                {
                    switch (settings.FailureMode)
                    {
                        case TrackableSettings.TrackingFailureMode.FreezePosition:
                            pose_UGBD = new Pose(pose_UGBD.position, newPose_GameboardSpace.rotation);
                            break;
                        // If we want to freeze both position and rotation when tracking is lost, things are easy - just do nothing.
                        case TrackableSettings.TrackingFailureMode.FreezePositionAndRotation:
                            break;
                        // Otherwise, we may want to keep the legacy behavior of snapping to a default position when tracking is lost.
                        case TrackableSettings.TrackingFailureMode.SnapToDefault:
                            // TODO: Rethink the existence of the preview pose? Is that a separate tracking failure mode?
                            if (settings.usePreviewPose)
                            {
                                pose_UGBD = new Pose(WorldToGameboardSpace(settings.previewPose.position, scaleSettings, gameboardSettings),
                                    WorldToGameboardSpace(settings.previewPose.rotation, gameboardSettings));
                            }
                            // Otherwise do nothing and let the developer control the head pose camera themselves.
                            // It will be up to them to let go once head tracking kicks in again.
                            break;
                    }
                }
                else    // Either things are working well and we're tracked, or we don't care about invalid data and want to display it regardless.
                {
                    pose_UGBD = newPose_GameboardSpace;
                }
            }

            protected override void SetDrivenObjectTransform(
                GlassesSettings settings,
                ScaleSettings scaleSettings,
                GameBoardSettings gameBoardSettings)
            {
                if (!isTracked && settings.RejectUntrackedPositionData)
                {
                    switch (settings.FailureMode)
                    {
                        case TrackableSettings.TrackingFailureMode.FreezePosition:
                            headPose.SetPositionAndRotation(headPose.position, pose_UWRLD.rotation);
                            break;
                        // If we want to freeze both position and rotation when tracking is lost, things are easy - just do nothing.
                        case TrackableSettings.TrackingFailureMode.FreezePositionAndRotation:
                            break;
                        // Otherwise, we may want to keep the legacy behavior of snapping to a default position when tracking is lost.
                        case TrackableSettings.TrackingFailureMode.SnapToDefault:
                            // TODO: Rethink the existence of the preview pose? Is that a separate tracking failure mode?
                            if (settings.usePreviewPose)
                            {
                                headPose.SetPositionAndRotation(
                                    settings.previewPose.position,
                                    settings.previewPose.rotation);
                            }
                            // Otherwise do nothing and let the developer control the head pose camera themselves.
                            // It will be up to them to let go once head tracking kicks in again.
                            break;
                    }
                }
                else    // Either things are working well and we're tracked, or we don't care about invalid data and want to display it regardless.
                {
                    headPose.SetPositionAndRotation(pose_UWRLD.position, pose_UWRLD.rotation);
                }
            }

            private Pose GetPoseGameboardSpace(T5_GlassesPose glassesPose)
            {
                // Unity reference frames:
                //
                // UGLS        - Unity GLaSses local space.
                //               +x right, +y up, +z forward
                // UGBD        - Unity Gameboard space.
                //               +x right, +y up, +z forward
                //
                // Tilt Five reference frames:
                //
                // DC          - Our right-handed version of Unity's default camera space.
                //               +x right, +y up, +z backward
                // GBD         - Gameboard space.
                //               +x right, +y forward, +z up

                Quaternion rotToDC_GBD = Quaternion.AngleAxis((-Mathf.PI / 2f) * Mathf.Rad2Deg, Vector3.right);

                Quaternion rotToGBD_DC = Quaternion.Inverse(rotToDC_GBD);

                Quaternion rotToGLS_GBD = glassesPose.RotationToGLS_GBD;

                Quaternion rotToGLS_DC = rotToGLS_GBD * rotToGBD_DC;

                Quaternion rotToUGBD_UGLS = new Quaternion(rotToGLS_DC.x, rotToGLS_DC.y, -rotToGLS_DC.z, rotToGLS_DC.w);

                // Swap from right-handed (T5 internal) to left-handed (Unity) coord space.
                Vector3 posOfUGLS_UGBD = ConvertPosGBDToUGBD(glassesPose.PosOfGLS_GBD);

                return new Pose(posOfUGLS_UGBD, rotToUGBD_UGLS);
            }
        }

        /// <summary>
        /// Trackable core for the smoothed glasses preview pose.
        /// </summary>
        /// <remarks>
        /// Provides a less noisy pose than the head pose used for glasses presentation. Intended
        /// for preview screen display.
        /// </remarks>
        private class GlassesPreviewCore : BaseGlassesCore
        {
            public GlassesPreviewCore(GlassesHandle glassesHandle) :
                base(glassesHandle, T5_GlassesPoseUsage.SpectatorPresentation, $"Glasses {glassesHandle} preview")
            { }

            /// <summary>
            /// Updates this <see cref="T:TiltFive.Glasses.GlassesPreviewCore"/>
            /// </summary>
            /// <param name="glassesSettings">Glasses settings for the update.</param>
            /// <param name="scaleSettings">Scale settings for the update.</param>
            /// <param name="gameBoardSettings">Gameboard settings for the update.</param>
            public new void Update(
                GlassesSettings glassesSettings,
                ScaleSettings scaleSettings,
                GameBoardSettings gameBoardSettings)
            {
                // Obtain the latest glasses pose.
                base.Update(glassesSettings, scaleSettings, gameBoardSettings);
            }
        }

        /// <summary>
        /// Internal Glasses core runtime.
        /// </summary>
        private class GlassesCore : BaseGlassesCore
        {
            public PlayerIndex playerIndex;
            public GameObject baseObject;
            public string friendlyName;

            /// <summary>
            /// Configuration ready indicator.
            /// </summary>
            public bool configured = false;

            public Dictionary<AREyes, Vector3> eyePositions = new Dictionary<AREyes, Vector3>()
            {
                { AREyes.EYE_LEFT, new Vector3() },
                { AREyes.EYE_RIGHT, new Vector3() }
            };

            public Dictionary<AREyes, Quaternion> eyeRotations = new Dictionary<AREyes, Quaternion>()
            {
                { AREyes.EYE_LEFT, new Quaternion() },
                { AREyes.EYE_RIGHT, new Quaternion() }
            };

            public GlassesPreviewCore previewCore;

            public GlassesCore(GlassesHandle glassesHandle) :
                base(glassesHandle, T5_GlassesPoseUsage.GlassesPresentation, $"Glasses {glassesHandle}")
            {
                if(!Player.TryGetPlayerIndex(glassesHandle, out playerIndex))
                {
                    Player.TryAddPlayer(glassesHandle, out playerIndex);
                }
                previewCore = new GlassesPreviewCore(glassesHandle);
                CameraImage.Initialize(glassesHandle);

                Log.Info($"Glasses {glassesHandle} connected.");
            }

            public override void Dispose()
            {
                GameObject.Destroy(splitStereoCamera);
                GameObject.Destroy(baseObject);
                CameraImage.RemoveCore(glassesHandle);
                previewCore.Dispose();

                base.Dispose();

                Log.Info($"Glasses {glassesHandle} (\"{friendlyName}\") disconnected");
            }

            /// <summary>
            /// The split stereo camera implementation used in lieu of XRSettings.
            /// </summary>
            private SplitStereoCamera splitStereoCamera = null;

            internal Camera leftEye => splitStereoCamera?.leftEyeCamera;
            internal Camera rightEye => splitStereoCamera?.rightEyeCamera;

            /// <summary>
            /// Reset this <see cref="T:TiltFive.Glasses.GlassesCore"/>
            /// </summary>
            /// <param name="glassesSettings">Glasses settings for configuring the instance.</param>
            /// <param name="spectatorSettings">Spectator settings for configuring the instance.</param>
            public virtual void Reset(GlassesSettings glassesSettings, SpectatorSettings spectatorSettings)
            {
                base.Reset(glassesSettings);

                configured = false;

                if(null == spectatorSettings.spectatorCamera)
                {
                    Log.Error($"Required Camera assignment missing from { spectatorSettings.GetType() }.  Check Spectator settings in Tilt Five Manager");
                    return;
                }

#if UNITY_EDITOR
                if (glassesSettings.tiltFiveXR)
                {
#endif
                    //if the splitScreenCamera does not exist already.
                    if (null == splitStereoCamera)
                    {
                        //get the head pose camera's GameObject
                        GameObject spectatorCameraObject = spectatorSettings.spectatorCamera.gameObject;

                        // Initialize the SplitStereoCamera
                        splitStereoCamera = spectatorCameraObject.AddComponent<SplitStereoCamera>();
                        if (null == headPoseRoot)
                        {

                            headPoseRoot = new GameObject($"Glasses {glassesHandle}");
                            previewCore = new GlassesPreviewCore(glassesHandle);
                        }
                        if (glassesSettings.objectTemplate && null == baseObject)
                        {
#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
                            if(glassesSettings.objectTemplate.TryGetComponent<PlayerInput>(out var playerInput))
                            {
                                Log.Warn("Attaching a PlayerInput component to the Object Template is not recommended," +
                                    " as the Object Template does not persist across scene loads." +
                                    "Consider using the Input Template instead.");
                            }
#endif //UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
                            baseObject = GameObject.Instantiate(glassesSettings.objectTemplate, headPoseRoot.transform);
                            baseObject.name = $"{baseObject.transform.parent.name} - Prefab {playerIndex}";
                        }
                        splitStereoCamera.Initialize(headPoseRoot, glassesSettings, spectatorSettings);
                    }
#if UNITY_EDITOR
                }
#endif

                TryGetFriendlyName(out friendlyName);

                configured = true;
            }

            /// <summary>
            /// Tests this <see cref="T:TiltFive.Glasses.GlassesCore"/> for validity
            /// with the parameterized <see cref="T:TiltFive.Glasses.GlassesSettings"/>
            /// </summary>
            /// <returns><c>true</c>, if valid, <c>false</c> otherwise.</returns>
            /// <param name="glassesSettings">Glasses settings.</param>
            public bool Validate(GlassesSettings glassesSettings, SpectatorSettings spectatorSettings)
            {
                bool valid = true;

                valid &= splitStereoCamera != null
                    && glassesSettings.cameraTemplate == splitStereoCamera.cameraTemplate
                    && spectatorSettings == splitStereoCamera.spectatorSettings;

                return valid;
            }

            public bool TryGetFriendlyName(out string friendlyName)
            {
                T5_StringUTF8 friendlyNameResult = "";
                int result = NativePlugin.T5_RESULT_UNKNOWN_ERROR;

                try
                {
                    result = NativePlugin.GetGlassesFriendlyName(glassesHandle, ref friendlyNameResult);
                }
                catch (System.Exception e)
                {
                    Log.Error($"Error getting friendly name: {e.Message}");
                }
                finally
                {
                    friendlyName = (result == NativePlugin.T5_RESULT_SUCCESS)
                    ? friendlyNameResult
                    : null;

                    // Unfortunately we can't use a "using" block for friendlyNameResult
                    // since "using" parameters are readonly, preventing us from passing it via "ref".
                    // We do the next best thing with try-finally and dispose of it here.
                    friendlyNameResult.Dispose();
                }

                return result == NativePlugin.T5_RESULT_SUCCESS;
            }

#if TILT_FIVE_ENABLE_PROJECTOR_EXTRINSICS_ADJUSTMENTS
            public bool TrySetProjectorExtrinsicsAdjustment(float[] args)
            {
                int result = 1;

                try
                {
                    result = NativePlugin.SetProjectorExtrinsicsAdjustment(glassesHandle, args);
                }
                catch (System.Exception e)
                {
                    Log.Error($"Error setting projector extrinsics adjustment: ${e.Message}");
                }
                return result == NativePlugin.T5_RESULT_SUCCESS;
            }

            public bool SetSingleEyeMode(AREyes eye)
            {
                Camera leftEyeCamera = splitStereoCamera.leftEyeCamera;
                Camera rightEyeCamera = splitStereoCamera.rightEyeCamera;
                if (leftEyeCamera == null || rightEyeCamera == null)
                {
                    Log.Error("Eye camera null during call to SetSingleEyeMode()");
                    return false;
                }

                switch (eye)
                {
                    case AREyes.EYE_LEFT:
                    {
                        // Enable only the left eye
                        leftEyeCamera.enabled = true;
                        rightEyeCamera.enabled = false;
                        // Clear the right eye texture
                        RenderTexture rt = RenderTexture.active;
                        RenderTexture.active = rightEyeCamera.targetTexture;
                        GL.Viewport(rightEyeCamera.pixelRect);
                        GL.Clear(true, true, Color.clear);
                        RenderTexture.active = rt;
                        break;
                    }
                    case AREyes.EYE_RIGHT:
                    {
                        // Enable only the right eye
                        leftEyeCamera.enabled = false;
                        rightEyeCamera.enabled = true;
                        // Clear the left eye texture
                        RenderTexture rt = RenderTexture.active;
                        RenderTexture.active = leftEyeCamera.targetTexture;
                        GL.Viewport(leftEyeCamera.pixelRect);
                        GL.Clear(true, true, Color.clear);
                        RenderTexture.active = rt;
                        break;
                    }
                    case AREyes.EYE_MAX:
                        leftEyeCamera.enabled = true;
                        rightEyeCamera.enabled = true;
                        break;
                }
                return true;
            }
#endif // TILT_FIVE_ENABLE_PROJECTOR_EXTRINSICS_ADJUSTMENTS

            /// <summary>
            /// Updates this <see cref="T:TiltFive.Glasses.GlassesCore"/>
            /// </summary>
            /// <param name="glassesSettings">Glasses settings for the update.</param>
            public virtual void Update(GlassesSettings glassesSettings, ScaleSettings scaleSettings, GameBoardSettings gameBoardSettings, SpectatorSettings spectatorSettings)
            {
                if (null == glassesSettings)
                {
                    Log.Error("GlassesSettings configuration required for Glasses tracking Update.");
                    return;
                }

                if (null == splitStereoCamera)
                {
                    Log.Error($"Stereo camera(s) missing from Glasses {glassesHandle} - aborting Update.");
                    return;
                }

                if (glassesSettings.cameraTemplate != splitStereoCamera.cameraTemplate)
                {
                    Log.Warn("Found mismatched template Cameras in GlassesCore Update - should call Reset.");
                    return;
                }

                if (spectatorSettings != splitStereoCamera.spectatorSettings)
                {
                    Log.Warn("Found mismatched spectator settings in GlassesCore Update - should call Reset.");
                    return;
                }

                // Obtain the latest glasses poses.
                base.Update(glassesSettings, scaleSettings, gameBoardSettings);
                previewCore.Update(glassesSettings, scaleSettings, gameBoardSettings);

                // Check whether the glasses are plugged in and available.
                splitStereoCamera.glassesHandle = glassesHandle;
                splitStereoCamera.enabled = isConnected;

                // Sync settings with splitStereoCamera
                splitStereoCamera.glassesSettings = glassesSettings;
                splitStereoCamera.spectatorSettings = spectatorSettings;

                // Enable spectating for player specified in the spectator settings, if they're available.
                splitStereoCamera.UseSpectatorCamera = Player.IsConnected(spectatorSettings.spectatedPlayer)
                    && Player.TryGetGlassesHandle(spectatorSettings.spectatedPlayer, out var spectatorGlassesHandle)
                    && spectatorGlassesHandle == glassesHandle;

                // Get the glasses pose in Unity world-space.
                float scaleToUWRLD_UGBD = scaleSettings.GetScaleToUWRLD_UGBD(gameBoardSettings.gameBoardScale);
                float scaleToUGBD_UWRLD = 1.0f / scaleToUWRLD_UGBD;

                // Set the game board transform on the SplitStereoCamera.
                splitStereoCamera.posUGBD_UWRLD = gameboardPos_UWRLD.position;
                splitStereoCamera.rotToUGBD_UWRLD = gameboardPos_UWRLD.rotation;
                splitStereoCamera.scaleToUGBD_UWRLD = scaleToUGBD_UWRLD;

                // TODO: Revisit native XR support.

                // NOTE: We do this because "Mock HMD" in UNITY_2017_0_2_OR_NEWER
                // the fieldOfView is locked to 111.96 degrees (Vive emulation),
                // so setting custom projection matrices is broken. If Unity
                // opens the API to custom settings, we can go back to native XR
                // support.

                // Manual split screen 'new glasses' until the day Unity lets
                // me override their Mock HMD settings.

                // compute half ipd translation
                float ipd_UGBD = GlassesSettings.DEFAULT_IPD_UGBD;
                if(!Display.GetGlassesIPD(glassesHandle, ref ipd_UGBD) && isConnected)
                {
                    Log.Error("Failed to obtain Glasses IPD");
                }
                float ipd_UWRLD = scaleToUWRLD_UGBD * ipd_UGBD;
                Vector3 eyeOffset = (headPose.right.normalized * (ipd_UWRLD * 0.5f));

                // set the left eye camera offset from the head by the half ipd amount (-)
                eyePositions[AREyes.EYE_LEFT] = headPose.position - eyeOffset;
                eyeRotations[AREyes.EYE_LEFT] = headPose.rotation;

                // set the right eye camera offset from the head by the half ipd amount (+)
                eyePositions[AREyes.EYE_RIGHT] = headPose.position + eyeOffset;
                eyeRotations[AREyes.EYE_RIGHT] = headPose.rotation;

                Camera leftEyeCamera = splitStereoCamera.leftEyeCamera;
                if (null != leftEyeCamera)
                {
                    GameObject leftEye = leftEyeCamera.gameObject;
                    leftEye.transform.position = eyePositions[AREyes.EYE_LEFT];
                    leftEye.transform.rotation = eyeRotations[AREyes.EYE_LEFT];

                    //make sure projection fields are synchronized to the head camera.
                    leftEyeCamera.nearClipPlane = glassesSettings.nearClipPlane / scaleToUGBD_UWRLD;
                    leftEyeCamera.farClipPlane = glassesSettings.farClipPlane / scaleToUGBD_UWRLD;
                    leftEyeCamera.fieldOfView = glassesSettings.fieldOfView;
                    leftEyeCamera.cullingMask = glassesSettings.cullingMask;
                }

                Camera rightEyeCamera = splitStereoCamera.rightEyeCamera;
                if (null != rightEyeCamera)
                {
                    GameObject rightEye = rightEyeCamera.gameObject;
                    rightEye.transform.position = eyePositions[AREyes.EYE_RIGHT];
                    rightEye.transform.rotation = eyeRotations[AREyes.EYE_RIGHT];

                    //make sure projection fields are synchronized to the head camera.
                    rightEyeCamera.nearClipPlane = glassesSettings.nearClipPlane / scaleToUGBD_UWRLD;
                    rightEyeCamera.farClipPlane = glassesSettings.farClipPlane / scaleToUGBD_UWRLD;
                    rightEyeCamera.fieldOfView = glassesSettings.fieldOfView;
                    rightEyeCamera.cullingMask = glassesSettings.cullingMask;
                }

                // TODO: Poll less frequently by plumbing t5_hmdGetChangedParams up to Unity.
                if (!TryGetFriendlyName(out glassesSettings.friendlyName))
                {
                    glassesSettings.friendlyName = GlassesSettings.DEFAULT_FRIENDLY_NAME;
                }
            }

            protected override void SetDrivenObjectTransform(GlassesSettings settings, ScaleSettings scaleSettings, GameBoardSettings gameBoardSettings)
            {
                base.SetDrivenObjectTransform(settings, scaleSettings, gameBoardSettings);
            }
        }

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
        private class GlassesDeviceCore : GlassesCore, IDisposable
        {
            internal GlassesDevice glassesDevice;
            internal GameObject playerTemplateObject;

            private enum TrackingState : int
            {
                None,
                Limited,
                Tracking
            }

            public GlassesDeviceCore(GlassesHandle glassesHandle) : base(glassesHandle)
            {
                Input.AddGlassesDevice(playerIndex);
                glassesDevice = Input.GetGlassesDevice(playerIndex);

                if(glassesDevice != null && glassesDevice.added)
                {
                    InputSystem.EnableDevice(glassesDevice);
                }
                if(TiltFiveManager2.IsInstantiated)
                {
                    TiltFiveManager2.Instance.RefreshInputDevicePairings();
                }
            }

            public override void Dispose()
            {
                base.Dispose();

                if(glassesDevice != null && glassesDevice.added)
                {
                    if (glassesDevice.LeftWand != null)
                    {
                        Input.RemoveWandDevice(playerIndex, ControllerIndex.Left);
                    }
                    if (glassesDevice.RightWand != null)
                    {
                        Input.RemoveWandDevice(playerIndex, ControllerIndex.Right);
                    }
                    InputSystem.DisableDevice(glassesDevice);
                    Input.RemoveGlassesDevice(playerIndex);
                }
                GameObject.Destroy(playerTemplateObject);
            }

            public override void Reset(GlassesSettings glassesSettings, SpectatorSettings spectatorSettings)
            {
                base.Reset(glassesSettings, spectatorSettings);

                if(glassesSettings.playerTemplate)
                {
                    playerTemplateObject = GameObject.Instantiate(glassesSettings.playerTemplate);
                    if(TiltFiveManager2.IsInstantiated)
                    {
                        TiltFiveManager2.Instance.RefreshInputDevicePairings();
                    }
                }
            }

            protected override void SetDrivenObjectTransform(GlassesSettings settings, ScaleSettings scaleSettings, GameBoardSettings gameboardSettings)
            {
                base.SetDrivenObjectTransform(settings, scaleSettings, gameboardSettings);

                if(glassesDevice == null || !glassesDevice.added)
                {
                    return;
                }

                // World space positions
                QueueDeltaStateEvent(glassesDevice.devicePosition, pose_UWRLD.position);
                QueueDeltaStateEvent(glassesDevice.centerEyePosition, pose_UWRLD.position);
                QueueDeltaStateEvent(glassesDevice.leftEyePosition, eyePositions[AREyes.EYE_LEFT]);
                QueueDeltaStateEvent(glassesDevice.rightEyePosition, eyePositions[AREyes.EYE_RIGHT]);

                // World space rotations
                QueueDeltaStateEvent(glassesDevice.deviceRotation, pose_UWRLD.rotation);
                QueueDeltaStateEvent(glassesDevice.centerEyeRotation, pose_UWRLD.rotation);
                QueueDeltaStateEvent(glassesDevice.leftEyeRotation, pose_UWRLD.rotation);
                QueueDeltaStateEvent(glassesDevice.rightEyeRotation, pose_UWRLD.rotation);

                // Gameboard space positions
                QueueDeltaStateEvent(glassesDevice.RawPosition, pose_UGBD.position);
                QueueDeltaStateEvent(glassesDevice.RawLeftEyePosition, WorldToGameboardSpace(eyePositions[AREyes.EYE_LEFT], scaleSettings, gameboardSettings));
                QueueDeltaStateEvent(glassesDevice.RawRightEyePosition, WorldToGameboardSpace(eyePositions[AREyes.EYE_RIGHT], scaleSettings, gameboardSettings));

                // Gameboard space rotations
                QueueDeltaStateEvent(glassesDevice.RawRotation, pose_UGBD.rotation);
                QueueDeltaStateEvent(glassesDevice.leftEyeRotation, pose_UGBD.rotation);
                QueueDeltaStateEvent(glassesDevice.rightEyeRotation, pose_UGBD.rotation);

                InputSystem.QueueDeltaStateEvent(glassesDevice.isTracked, isTracked);

                var trackingState = TrackingState.Tracking;
                if (!isTracked)
                {
                    trackingState = settings.FailureMode == TrackableSettings.TrackingFailureMode.FreezePosition
                        ? TrackingState.Limited
                        : TrackingState.None;
                }

                InputSystem.QueueDeltaStateEvent(glassesDevice.trackingState, (int)trackingState);
            }

            private static void QueueDeltaStateEvent(Vector3Control vector3Control, Vector3 delta)
            {
                InputSystem.QueueDeltaStateEvent(vector3Control.x, delta.x);
                InputSystem.QueueDeltaStateEvent(vector3Control.y, delta.y);
                InputSystem.QueueDeltaStateEvent(vector3Control.z, delta.z);
            }

            // For some reason, using QueueDeltaStateEvent on a QuaternionControl with a Quaternion as the delta state doesn't work.
            // As a workaround, let's do it component-wise, since we know floats seem fine.
            private static void QueueDeltaStateEvent(QuaternionControl quaternionControl, Quaternion delta)
            {
                InputSystem.QueueDeltaStateEvent(quaternionControl.w, delta.w);
                InputSystem.QueueDeltaStateEvent(quaternionControl.x, delta.x);
                InputSystem.QueueDeltaStateEvent(quaternionControl.y, delta.y);
                InputSystem.QueueDeltaStateEvent(quaternionControl.z, delta.z);
            }
        }
#endif // UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE

    }
}
