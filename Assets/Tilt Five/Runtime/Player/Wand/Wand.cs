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
using TiltFive.Logging;

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.InputSystem.Controls;
#endif

using WandButton = TiltFive.Input.WandButton;

namespace TiltFive
{
    /// <summary>
    /// Wand Settings encapsulates all configuration data used by the Wand's
    /// tracking runtime to compute the Wand Pose and apply it to the driven GameObject.
    /// </summary>
    [System.Serializable]
    public class WandSettings : TrackableSettings
    {
        public ControllerIndex controllerIndex;

        public GameObject GripPoint;
        public GameObject FingertipPoint;
        public GameObject AimPoint;

        // TODO: Think about some accessors for physical attributes of the wand (length, distance to tip, etc)?
        internal WandSettings Copy()
        {
            return (WandSettings)MemberwiseClone();
        }
    }

    /// <summary>
    /// The Wand API and runtime.
    /// </summary>
    public class Wand : Singleton<Wand>
    {
        #region Private Fields

        /// <summary>
        /// The collection of WandCores. GlassesHandles are mapped to pairs of right/left WandCores.
        /// </summary>
        private Dictionary<GlassesHandle, WandPair> wandCores = new Dictionary<GlassesHandle, WandPair>();

        /// <summary>
        /// The default position of the wand relative to the board.
        /// </summary>
        /// <remarks>
        /// The wand GameObject will snap back to this position if the glasses and/or wand are unavailable.
        /// </remarks>
        private static readonly Vector3 DEFAULT_WAND_POSITION_GAME_BOARD_SPACE = new Vector3(0f, 0.25f, -0.25f);
        /// <summary>
        /// A left/right offset to the default wand position, depending on handedness.
        /// </summary>
        private static readonly Vector3 DEFAULT_WAND_HANDEDNESS_OFFSET_GAME_BOARD_SPACE = new Vector3(0.125f, 0f, 0f);

        /// <summary>
        /// The default rotation of the wand relative to the board.
        /// </summary>
        /// <remarks>
        /// The wand GameObject will snap back to this rotation if the glasses are unavailable.
        /// If different behavior is desired in this scenario, a different camera should be used.
        /// </remarks>
        private static readonly Quaternion DEFAULT_WAND_ROTATION_GAME_BOARD_SPACE = Quaternion.Euler(new Vector3(-33f, 0f, 0f));


        // Handles for Glasses that have just connected
        private HashSet<GlassesHandle> incomingHandles = new HashSet<GlassesHandle>();
        // Handles for Glasses that have just disconnected
        private HashSet<GlassesHandle> lostHandles = new HashSet<GlassesHandle>();

        private HashSet<WandCore> lostWands = new HashSet<WandCore>();


        // Scan for new wands every half second.
        private static DateTime lastScanAttempt = System.DateTime.MinValue;

        // This should likely become a query into the native library.
        private static readonly double wandScanInterval = 0.5d;

        private static bool wandAvailabilityErroredOnce = false;


        // Used to guard against certain functions executing more than once per frame
        private static int currentFrame = -1;

        #endregion Private Fields


        #region Private Structs

        private struct WandPair
        {
            public WandCore RightWand;
            public WandCore LeftWand;

            public WandPair(WandCore right, WandCore left)
            {
                RightWand = right;
                LeftWand = left;
            }

            public WandCore this[ControllerIndex controllerIndex]
            {
                get
                {
                    switch (controllerIndex)
                    {
                        case ControllerIndex.Right:
                            return RightWand;
                        case ControllerIndex.Left:
                            return LeftWand;
                        default:
                            // TODO: If we get an unexpected value here, should we fail silently or throw an exception?
                            return null;
                    }
                }
            }

            public bool TryGet(ControllerIndex controllerIndex, out WandCore wandCore)
            {
                wandCore = this[controllerIndex];
                return wandCore != null;
            }
        }

        #endregion Private Structs


        #region Public Functions

        /// <summary>
        /// Gets the position of the wand in world space.
        /// </summary>
        /// <param name="controllerIndex"></param>
        /// <param name="controllerPosition"></param>
        /// <param name="glassesHandle">The specified glasses. If null is provided, this uses the default glasses.</param>
        /// <returns>If the indicated wand is not connected, this returns the zero vector.</returns>
        public static Vector3 GetPosition(
            ControllerIndex controllerIndex = ControllerIndex.Right,
            ControllerPosition controllerPosition = ControllerPosition.Grip,
            PlayerIndex playerIndex = PlayerIndex.One)
        {
            if(!Player.TryGetGlassesHandle(playerIndex, out var glassesHandle)
                || !Instance.wandCores.TryGetValue(glassesHandle, out var wandPair)
                || !wandPair.TryGet(controllerIndex, out var wandCore))
            {
                return Vector3.zero;
            }

            switch (controllerPosition)
            {
                case ControllerPosition.Fingertips:
                    return wandCore.fingertipsPose_UnityWorldSpace.position;
                case ControllerPosition.Aim:
                    return wandCore.aimPose_UnityWorldSpace.position;
                case ControllerPosition.Grip:
                    return wandCore.Pose_UnityWorldSpace.position;
                default:
                    return Vector3.zero;
            }
        }

        /// <summary>
        /// Gets the rotation of the wand in world space.
        /// </summary>
        /// <param name="controllerIndex"></param>
        /// <param name="glassesHandle">The specified glasses. If null is provided, this uses the default glasses.</param>
        /// <returns>If the indicated wand is not connected, this returns the identity quaternion.</returns>
        public static Quaternion GetRotation(ControllerIndex controllerIndex = ControllerIndex.Right, PlayerIndex playerIndex = PlayerIndex.One)
        {
            if (!Player.TryGetGlassesHandle(playerIndex, out var glassesHandle)
                || !Instance.wandCores.TryGetValue(glassesHandle, out var wandPair)
                || !wandPair.TryGet(controllerIndex, out var wandCore))
            {
                return Quaternion.identity;
            }

            return wandCore.Pose_UnityWorldSpace.rotation;
        }

        public static bool IsTracked(ControllerIndex controllerIndex = ControllerIndex.Right, PlayerIndex playerIndex = PlayerIndex.One)
        {
            if (!Player.TryGetGlassesHandle(playerIndex, out var glassesHandle)
                || !Instance.wandCores.TryGetValue(glassesHandle, out var wandPair)
                || !wandPair.TryGet(controllerIndex, out var wandCore))
            {
                return false;
            }

            return wandCore.IsTracked;
        }

        /// <summary>
        /// Gets the connection status of the indicated wand.
        /// </summary>
        /// <param name="connected"></param>
        /// <param name="playerIndex"></param>
        /// <param name="controllerIndex"></param>
        /// <returns>Returns false if something went wrong while attempting to check wand connectivity.</returns>
        public static bool TryCheckConnected(out bool connected, PlayerIndex playerIndex, ControllerIndex controllerIndex = ControllerIndex.Right)
        {
            if(!Player.TryGetGlassesHandle(playerIndex, out var glassesHandle)
                || !Instance.wandCores.TryGetValue(glassesHandle, out var wandPair)
                || !wandPair.TryGet(controllerIndex, out var wandCore))
            {
                connected = false;
                return true;
            }

            connected = wandCore.IsConnected;
            return true;
        }

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
        /// <summary>
        /// Attempts to get the WandDevice corresponding to the specfied player and controller index.
        /// </summary>
        /// <param name="playerIndex"></param>
        /// <param name="controllerIndex"></param>
        /// <param name="wandDevice"></param>
        /// <returns>
        /// Returns <c>false</c> along with a null WandDevice if the WandDevice could not be obtained for the specified
        /// player and wand combination (e.g. if the specified player is not connected, or if their wand is not connected).
        /// </returns>
        public static bool TryGetWandDevice(PlayerIndex playerIndex, ControllerIndex controllerIndex, out WandDevice wandDevice)
        {
            if(!Player.TryGetGlassesHandle(playerIndex, out var glassesHandle) || !Instance.wandCores.TryGetValue(glassesHandle, out var wandPair)
                || !wandPair.TryGet(controllerIndex, out var wandCore) || !(wandCore is WandDeviceCore wandDeviceCore))
            {
                wandDevice = null;
                return false;
            }

            wandDevice = wandDeviceCore.wandDevice;
            return true;
        }
#endif

        /// <summary>
        /// Try to send a haptics impulse to the specified wand
        /// </summary>
        /// <param name="amplitude">The amplitude of the impulse, between [0.0,1.0].</param>
        /// <param name="duration">The duration of the impulse, between 0.0 and  0.320 s.</param>
        /// <param name="playerIndex"></param>
        /// <param name="controllerIndex"></param>
        /// <returns>Returns false if something went wrong while attempting to send the impulse command</returns>
        public static bool TrySendImpulse(float amplitude, float duration, PlayerIndex playerIndex = PlayerIndex.One, ControllerIndex controllerIndex = ControllerIndex.Right)
        {
            if (!Player.TryGetGlassesHandle(playerIndex, out var glassesHandle)
                || !Instance.wandCores.TryGetValue(glassesHandle, out var wandPair)
                || !wandPair.TryGet(controllerIndex, out var wandCore))
            {
                return false;
            }

            try
            {
                if (NativePlugin.SendImpulse(glassesHandle, controllerIndex, amplitude, (UInt16)(Mathf.Clamp(duration, 0.0f, 0.320f) * 1000)) == 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (DllNotFoundException e)
            {
                Log.Info("Tilt Five library missing. Could not connect to wand: {0}", e.Message);
                return false;
            }
            catch (Exception e)
            {
                Log.Error(
                    "Failed to send impulse to wand: {0}",
                    e.ToString());
                return false;
            }
        }

        #region Deprecated Public Functions

        // Update is called once per frame
        [Obsolete("Converted to be an internal function.")]
        public static void Update(WandSettings wandSettings, ScaleSettings scaleSettings, GameBoardSettings gameBoardSettings, PlayerIndex playerIndex = PlayerIndex.One)
        {
            Update(playerIndex, wandSettings, scaleSettings, gameBoardSettings);
        }

        [Obsolete("Converted to be an internal function.")]
        public static void ScanForWands()
        {
            Scan();
        }

        #endregion Deprecated Public Functions

        #endregion Public Functions


        #region Internal Functions

        internal static void Update(PlayerIndex playerIndex, WandSettings wandSettings, ScaleSettings scaleSettings, GameBoardSettings gameBoardSettings)
        {
            // Update the relevant WandCore
            if (Player.TryGetGlassesHandle(playerIndex, out var glassesHandle))
            {
                Update(glassesHandle, wandSettings, scaleSettings, gameBoardSettings);
            }
        }

        internal static void Update(GlassesHandle glassesHandle, WandSettings wandSettings, ScaleSettings scaleSettings, GameBoardSettings gameBoardSettings)
        {
            if (Instance.wandCores.TryGetValue(glassesHandle, out var wandPair)
                && wandPair.TryGet(wandSettings.controllerIndex, out var wandCore))
            {
                wandCore.Update(wandSettings, scaleSettings, gameBoardSettings);
            }
        }

        internal static void OnDisable()
        {
            foreach (var wandPair in Instance.wandCores.Values)
            {
                wandPair.RightWand?.Dispose();
                wandPair.LeftWand?.Dispose();
            }
            Instance.wandCores.Clear();
        }

        internal static void Scan()
        {
            // Tell the native plugin to search for wands.
            TryScanForWands();

            // Obtain the latest set of connected glasses
            var connectedGlassesHandles = Glasses.GetAllConnectedGlassesHandles();

            // Add/Remove entries from the wandCores dictionary depending on which glasses appeared/disappeared
            // ---------------------------------------------------
            var wandCores = Instance.wandCores;
            var incomingHandles = Instance.incomingHandles;
            var lostHandles = Instance.lostHandles;
            var lostWands = Instance.lostWands;

            incomingHandles.Clear();
            lostHandles.Clear();
            lostWands.Clear();

            // Add newly connected wands
            for (int i = 0; i < connectedGlassesHandles.Length; i++)
            {
                var glassesHandle = connectedGlassesHandles[i];
                incomingHandles.Add(glassesHandle);

                var rightWandCore = Instance.ObtainWandCore(glassesHandle, ControllerIndex.Right);
                var leftWandCore = Instance.ObtainWandCore(glassesHandle, ControllerIndex.Left);

                // Obtain and store a pair of WandCores to associate with this GlassesHandle.
                // If either wand is unavailable, we'll just store null.
                // This will also clear any WandCores that represented a now-disconnected wand.
                wandCores[glassesHandle] = new WandPair(rightWandCore, leftWandCore);
            }

            // Prune disconnected wands
            foreach (var glassesHandle in wandCores.Keys)
            {
                // If a pair of glasses disconnects, save its handle for the next foreach loop below
                if (!incomingHandles.Contains(glassesHandle))
                {
                    lostHandles.Add(glassesHandle);
                }
                // otherwise, check if its wands are still connected.
                else
                {
                    if (wandCores.TryGetValue(glassesHandle, out var wandPair))
                    {
                        if (wandPair.TryGet(ControllerIndex.Left, out var leftWandCore)
                            && (!TryGetWandAvailability(out bool leftWandConnected, glassesHandle, ControllerIndex.Left) || !leftWandConnected))
                        {
                            lostWands.Add(leftWandCore);
                        }
                        if (wandPair.TryGet(ControllerIndex.Right, out var rightWandCore)
                            && (!TryGetWandAvailability(out bool rightWandConnected, glassesHandle, ControllerIndex.Right) || !rightWandConnected))
                        {
                            lostWands.Add(rightWandCore);
                        }
                    }
                }
            }

            foreach (var lostHandle in lostHandles)
            {
                var lostWandPair = wandCores[lostHandle];

                if (lostWandPair.TryGet(ControllerIndex.Right, out var lostRightWand))
                {
                    lostWands.Add(lostRightWand);
                }

                if (lostWandPair.TryGet(ControllerIndex.Left, out var lostLeftWand))
                {
                    lostWands.Add(lostLeftWand);
                }
                wandCores.Remove(lostHandle);
            }

            foreach (var lostWand in lostWands)
            {
                lostWand.Dispose();
            }
        }

        internal static void GetLatestInputs()
        {
            // If the previous/current wand states are shuffled more than once per frame,
            // our button rising/falling edge detection breaks.
            // Detect this scenario and return early to prevent this.
            if (Time.frameCount == currentFrame)
            {
                return;
            }
            currentFrame = Time.frameCount;

            foreach (var wandPair in Instance.wandCores.Values)
            {
                wandPair.RightWand?.GetLatestInputs();
                wandPair.LeftWand?.GetLatestInputs();
            }
        }

        internal static bool GetButton(WandButton button, GlassesHandle glassesHandle,
            ControllerIndex controllerIndex = ControllerIndex.Right)
        {
            if(!Instance.wandCores.TryGetValue(glassesHandle, out var wandPair)
                || !wandPair.TryGet(controllerIndex, out var wandCore))
            {
                return false;
            }
            return wandCore.GetButton(button);
        }

        internal static bool TryGetButton(WandButton button, out bool pressed,
            GlassesHandle glassesHandle, ControllerIndex controllerIndex = ControllerIndex.Right)
        {
            if (!Instance.wandCores.TryGetValue(glassesHandle, out var wandPair)
                || !wandPair.TryGet(controllerIndex, out var wandCore))
            {
                pressed = false;
                return false;
            }

            return wandCore.TryGetButton(button, out pressed);
        }

        internal static bool GetButtonDown(WandButton button,
            GlassesHandle glassesHandle, ControllerIndex controllerIndex = ControllerIndex.Right)
        {
            if (!Instance.wandCores.TryGetValue(glassesHandle, out var wandPair)
                || !wandPair.TryGet(controllerIndex, out var wandCore))
            {
                return false;
            }

            return wandCore.GetButtonDown(button);
        }

        internal static bool TryGetButtonDown(WandButton button, out bool buttonDown,
            GlassesHandle glassesHandle, ControllerIndex controllerIndex = ControllerIndex.Right)
        {
            if (!Instance.wandCores.TryGetValue(glassesHandle, out var wandPair)
                || !wandPair.TryGet(controllerIndex, out var wandCore))
            {
                buttonDown = false;
                return false;
            }

            return wandCore.TryGetButtonDown(button, out buttonDown);
        }

        internal static bool GetButtonUp(WandButton button,
            GlassesHandle glassesHandle, ControllerIndex controllerIndex = ControllerIndex.Right)
        {
            if (Instance.wandCores.TryGetValue(glassesHandle, out var wandPair)
                || !wandPair.TryGet(controllerIndex, out var wandCore))
            {
                // TODO: Handle corner case in which wandCore existed during the previous frame, and the button was pressed
                // We'd want to return true in that scenario.
                return false;
            }

            return wandCore.GetButtonUp(button);
        }

        internal static bool TryGetButtonUp(WandButton button, out bool buttonUp,
            GlassesHandle glassesHandle, ControllerIndex controllerIndex = ControllerIndex.Right)
        {
            if (!Instance.wandCores.TryGetValue(glassesHandle, out var wandPair)
                || !wandPair.TryGet(controllerIndex, out var wandCore))
            {
                // TODO: Handle corner case in which wandCore existed during the previous frame, and the button was pressed.
                // We'd want to return true in that scenario.
                buttonUp = false;
                return false;
            }

            return wandCore.TryGetButtonUp(button, out buttonUp);
        }

        internal static Vector2 GetStickTilt(GlassesHandle glassesHandle, ControllerIndex controllerIndex = ControllerIndex.Right)
        {
            if (Instance.wandCores.TryGetValue(glassesHandle, out var wandPair)
                || !wandPair.TryGet(controllerIndex, out var wandCore))
            {
                return Vector2.zero;
            }

            return wandCore.GetStickTilt();
        }

        internal static bool TryGetStickTilt(out Vector2 stickTilt,
            GlassesHandle glassesHandle, ControllerIndex controllerIndex = ControllerIndex.Right)
        {
            if (!Instance.wandCores.TryGetValue(glassesHandle, out var wandPair)
                || !wandPair.TryGet(controllerIndex, out var wandCore))
            {
                stickTilt = Vector2.zero;
                return false;
            }

            return wandCore.TryGetStickTilt(out stickTilt);
        }

        internal static float GetTrigger(GlassesHandle glassesHandle, ControllerIndex controllerIndex = ControllerIndex.Right)
        {
            if (!Instance.wandCores.TryGetValue(glassesHandle, out var wandPair)
                || !wandPair.TryGet(controllerIndex, out var wandCore))
            {
                return 0f;
            }

            return wandCore.GetTrigger();
        }

        internal static bool TryGetTrigger(out float triggerDisplacement,
            GlassesHandle glassesHandle, ControllerIndex controllerIndex = ControllerIndex.Right)
        {
            if (!Instance.wandCores.TryGetValue(glassesHandle, out var wandPair)
                || !wandPair.TryGet(controllerIndex, out var wandCore))
            {
                triggerDisplacement = 0f;
                return false;
            }

            return wandCore.TryGetTrigger(out triggerDisplacement);
        }

        internal static bool TryGetWandControlsState(GlassesHandle glassesHandle, out T5_ControllerState? controllerState,
            ControllerIndex controllerIndex = ControllerIndex.Right)
        {
            int result = NativePlugin.T5_RESULT_UNKNOWN_ERROR;

            try
            {
                T5_ControllerState state = new T5_ControllerState();
                result = NativePlugin.GetControllerState(glassesHandle, controllerIndex, ref state);
                controllerState = (result == NativePlugin.T5_RESULT_SUCCESS)
                    ? state
                    : (T5_ControllerState?)null;
            }
            catch (Exception e)
            {
                controllerState = null;
                Log.Error(e.Message);
            }

            return result == NativePlugin.T5_RESULT_SUCCESS;
        }

        #endregion Internal Functions


        #region Private Functions

        private static bool TryScanForWands()
        {
            var currentTime = System.DateTime.Now;
            var timeSinceLastScan = currentTime - lastScanAttempt;

            // Scan for wands if the scan interval has elapsed to catch newly connected wands.
            if (timeSinceLastScan.TotalSeconds >= wandScanInterval)
            {
                int result = NativePlugin.T5_RESULT_UNKNOWN_ERROR;

                try
                {
                    result = NativePlugin.ScanForWands();
                }
                catch (System.DllNotFoundException e)
                {
                    Log.Info(
                        "Could not connect to Tilt Five plugin to scan for wands: {0}",
                        e);
                }
                catch (Exception e)
                {
                    Log.Error(e.Message);
                }

                lastScanAttempt = currentTime;
                return (result == NativePlugin.T5_RESULT_SUCCESS);
            }

            return false;
        }

        private static bool TryGetWandAvailability(out bool connected, GlassesHandle glassesHandle, ControllerIndex controllerIndex)
        {
            if (!wandAvailabilityErroredOnce)
            {
                try
                {
                    T5_Bool wandAvailable = false;
                    int result = NativePlugin.GetWandAvailability(glassesHandle, ref wandAvailable, controllerIndex);

                    if (result == NativePlugin.T5_RESULT_SUCCESS)
                    {
                        connected = wandAvailable;
                        return true;
                    }
                }
                catch (DllNotFoundException e)
                {
                    Log.Info("Could not connect to Tilt Five plugin for wand: {0}", e.Message);
                    wandAvailabilityErroredOnce = true;
                }
                catch (Exception e)
                {
                    Log.Error(
                        "Failed to connect to Tilt Five plugin for wand availability: {0}",
                        e.ToString());
                    wandAvailabilityErroredOnce = true;
                }
            }

            connected = false;
            return false;
        }

        private WandCore ObtainWandCore(GlassesHandle glassesHandle, ControllerIndex controllerIndex)
        {
            WandCore wandCore = null;
            var glassesAlreadyMonitored = wandCores.TryGetValue(glassesHandle, out var wandPair);

            // Ask the native plugin whether this wand is currently connected.
            if (TryGetWandAvailability(out var wandConnected, glassesHandle, controllerIndex) && wandConnected)
            {
                // If we're not already monitoring this wand, go ahead and create a corresponding WandCore.
                if (!glassesAlreadyMonitored || !wandPair.TryGet(controllerIndex, out wandCore))
                {
#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
                    wandCore = new WandDeviceCore(glassesHandle, controllerIndex);
#else
                    wandCore = new WandCore(glassesHandle, controllerIndex);
#endif
                }
                return wandCore;
            }
            // If this wand is disconnected and we were previously monitoring it, mark it as disconnected
            else if(glassesAlreadyMonitored && wandPair.TryGet(controllerIndex, out var lostWandCore))
            {
                lostWands.Add(lostWandCore);
            }

            return wandCore;
        }

        #endregion Private Functions


        #region Private Classes

        /// <summary>
        /// Internal Wand core runtime.
        /// </summary>
        private class WandCore : TrackableCore<WandSettings, T5_ControllerState>, IDisposable
        {
            #region Public Fields

            public readonly GlassesHandle glassesHandle;
            public readonly ControllerIndex controllerIndex;

            public Pose fingertipsPose_GameboardSpace = new Pose(DEFAULT_WAND_POSITION_GAME_BOARD_SPACE, Quaternion.identity);
            public Pose aimPose_GameboardSpace = new Pose(DEFAULT_WAND_POSITION_GAME_BOARD_SPACE, Quaternion.identity);

            public Pose gripPose_UnityWorldSpace => pose_UWRLD;
            public Pose fingertipsPose_UnityWorldSpace;
            public Pose aimPose_UnityWorldSpace;

            protected T5_ControllerState? currentState;
            protected T5_ControllerState? previousState;

            #endregion Public Fields


            #region Public Functions

            public WandCore(GlassesHandle glassesHandle, ControllerIndex controllerIndex)
            {
                this.glassesHandle = glassesHandle;
                this.controllerIndex = controllerIndex;

                Glasses.TryGetFriendlyName(glassesHandle, out var friendlyName);
                Log.Info($"Glasses {glassesHandle} (\"{friendlyName}\") {Enum.GetName(typeof(ControllerIndex), controllerIndex)} Wand connected");
            }

            public virtual void GetLatestInputs()
            {
                previousState = currentState;

                try
                {
                    T5_ControllerState state = new T5_ControllerState();

                    var result = NativePlugin.GetControllerState(glassesHandle, controllerIndex, ref state);

                    currentState = (result == NativePlugin.T5_RESULT_SUCCESS)
                        ? state
                        : (T5_ControllerState?)null;
                }
                catch (Exception e)
                {
                    currentState = null;
                    Log.Error(e.Message);
                }
            }

            public bool GetButton(WandButton button)
            {
                // If the wand isn't connected, GetButton() should return a default value of false.
                return currentState?.GetButton(button) ?? false;
            }

            public bool TryGetButton(WandButton button, out bool pressed)
            {
                pressed = currentState?.GetButton(button) ?? false;

                // If the wand isn't connected, TryGetButton() should fail.
                return currentState.HasValue;
            }

            public bool GetButtonDown(WandButton button)
            {

                // If the current wand state is null, the wand isn't connected.
                // If so, let the application assume the user isn't pressing the button currently.
                var pressed = currentState?.GetButton(button) ?? false;

                // If the previous wand state is null, the wand wasn't connected.
                // If so, let the application assume the user wasn't pressing the button last frame.
                var previouslyPressed = previousState?.GetButton(button) ?? false;

                // The wand could potentially connect while the user is holding a button, so just report the button state.
                if (!previousState.HasValue && currentState.HasValue)
                {
                    return pressed;
                }
                // Return true if the button is currently pressed, but was unpressed on the previous frame.
                return pressed && !previouslyPressed;
            }

            public bool TryGetButtonDown(WandButton button, out bool buttonDown)
            {
                // Even if this operation fails, give buttonDown a default value.
                buttonDown = GetButtonDown(button);
                return currentState.HasValue;
            }

            public bool GetButtonUp(WandButton button)
            {
                // If the current wand state is null, the wand isn't connected.
                // If so, let the application assume the user isn't pressing the button currently.
                var pressed = currentState?.GetButton(button) ?? false;

                // If the previous wand state is null, the wand wasn't connected.
                // If so, let the application assume the user wasn't pressing the button last frame.
                var previouslyPressed = previousState?.GetButton(button) ?? false;

                // Return true if the button is currently released, but was pressed on the previous frame.
                return previousState.HasValue
                    ? !pressed && previouslyPressed
                    // If the current state exists but the previous state was null, the wand has just connected.
                    // There's no way for the button to be pressed during the previous frame,
                    // so there's no way for the button to have been released this frame. Always return false.
                    : false;
            }

            public bool TryGetButtonUp(WandButton button, out bool buttonUp)
            {
                // Even if this operation fails, give buttonUp a default value.
                buttonUp = GetButtonUp(button);
                return currentState.HasValue;
            }

            public Vector2 GetStickTilt()
            {
                return currentState?.Stick ?? Vector2.zero;
            }

            public bool TryGetStickTilt(out Vector2 stickTilt)
            {
                stickTilt = GetStickTilt();
                return currentState.HasValue;
            }

            public float GetTrigger()
            {
                return currentState?.Trigger ?? 0.0f;
            }

            public bool TryGetTrigger(out float triggerDisplacement)
            {
                triggerDisplacement = GetTrigger();
                return currentState.HasValue;
            }

            #endregion Public Functions


            #region Overrides

            public new void Reset(WandSettings wandSettings)
            {
                base.Reset(wandSettings);
            }

            public new virtual void Update(WandSettings wandSettings, ScaleSettings scaleSettings, GameBoardSettings gameBoardSettings)
            {
                if (wandSettings == null)
                {
                    Log.Error("WandSettings configuration required for Wand tracking updates.");
                    return;
                }

                base.Update(wandSettings, scaleSettings, gameBoardSettings);
            }

            protected override void SetDefaultPoseGameboardSpace(WandSettings settings)
            {
                pose_UGBD = GetDefaultPoseGameboardSpace(settings);
                // We don't have a good offset that we can use for default fingertips/aim poses, so just use the default pose for everything
                fingertipsPose_GameboardSpace = pose_UGBD;
                aimPose_GameboardSpace = pose_UGBD;
            }

            protected static Pose GetDefaultPoseGameboardSpace(WandSettings settings)
            {
                Vector3 defaultPosition = DEFAULT_WAND_POSITION_GAME_BOARD_SPACE;

                defaultPosition += DEFAULT_WAND_HANDEDNESS_OFFSET_GAME_BOARD_SPACE
                    * (settings.controllerIndex == ControllerIndex.Right ? 1f : -1f);
                return new Pose(defaultPosition, DEFAULT_WAND_ROTATION_GAME_BOARD_SPACE);
            }

            protected override void SetPoseUnityWorldSpace(ScaleSettings scaleSettings, GameBoardSettings gameBoardSettings)
            {
                pose_UWRLD = GameboardToWorldSpace(pose_UGBD, scaleSettings, gameBoardSettings);
                fingertipsPose_UnityWorldSpace = GameboardToWorldSpace(fingertipsPose_GameboardSpace, scaleSettings, gameBoardSettings);
                aimPose_UnityWorldSpace = GameboardToWorldSpace(aimPose_GameboardSpace, scaleSettings, gameBoardSettings);
            }

            protected override bool TryCheckConnected(out bool connected)
            {
                if (!wandAvailabilityErroredOnce)
                {
                    try
                    {
                        T5_Bool wandAvailable = false;
                        int result = NativePlugin.GetWandAvailability(glassesHandle, ref wandAvailable, controllerIndex);

                        if (result == NativePlugin.T5_RESULT_SUCCESS)
                        {
                            isConnected = wandAvailable;
                            connected = wandAvailable;
                            return true;
                        }
                    }
                    catch (DllNotFoundException e)
                    {
                        Log.Info("Could not connect to Tilt Five plugin for wand: {0}", e.Message);
                        wandAvailabilityErroredOnce = true;
                    }
                    catch (Exception e)
                    {
                        Log.Error(
                            "Failed to connect to Tilt Five plugin for wand availability: {0}",
                            e.ToString());
                        wandAvailabilityErroredOnce = true;
                    }
                }

                isConnected = false;
                connected = false;
                return false;
            }

            protected override bool TryGetStateFromPlugin(out T5_ControllerState controllerState, out bool poseIsValid, GameBoardSettings gameBoardSettings)
            {
                if (!TryGetWandControlsState(glassesHandle, out var controllerStateResult, controllerIndex))
                {
                    poseIsValid = false;
                    controllerState = new T5_ControllerState();

                    return false;
                }

                controllerState = controllerStateResult.Value;
                poseIsValid = Glasses.IsTracked(glassesHandle) && controllerState.PoseValid;

                return true;
            }

            protected override void SetPoseGameboardSpace(in T5_ControllerState controllerState,
                WandSettings wandSettings, ScaleSettings scaleSettings, GameBoardSettings gameboardSettings)
            {
                // Unity reference frames:
                //
                // UWND        - Unity WaND local space.
                //               +x right, +y up, +z forward
                // UGBD        - Unity Gameboard space.
                //               +x right, +y up, +z forward
                //
                // Tilt Five reference frames:
                //
                // DW          - Our right-handed version of Unity's default wand space.
                //               +x right, +y down, +z forward
                // GBD         - Gameboard space.
                //               +x right, +y forward, +z up

                Vector3 gripPosition_UGBD = ConvertPosGBDToUGBD(controllerState.GripPos_GBD);
                Vector3 fingertipsPosition_UGBD = ConvertPosGBDToUGBD(controllerState.FingertipsPos_GBD);
                Vector3 aimPosition_UGBD = ConvertPosGBDToUGBD(controllerState.AimPos_GBD);
                var rotation_UGBD = CalculateRotation(controllerState.RotToWND_GBD);

                ProcessTrackingData(gripPosition_UGBD, fingertipsPosition_UGBD, aimPosition_UGBD,
                    rotation_UGBD, wandSettings, scaleSettings, gameboardSettings,
                    out pose_UGBD, out fingertipsPose_GameboardSpace, out aimPose_GameboardSpace);
            }

            /// <summary>
            /// Handle setting the wand pose when we know the controller state isn't valid.
            /// </summary>
            /// <param name="t5_ControllerState">The invalid state. It can still be useful if we want to use its rotation values.</param>
            /// <param name="settings"></param>
            /// <param name="scaleSettings"></param>
            /// <param name="gameboardSettings"></param>
            protected override void SetInvalidPoseGameboardSpace(in T5_ControllerState t5_ControllerState, WandSettings settings, ScaleSettings scaleSettings, GameBoardSettings gameboardSettings)
            {
                SetPoseGameboardSpace(t5_ControllerState, settings, scaleSettings, gameboardSettings);
            }

            protected override void SetDrivenObjectTransform(WandSettings wandSettings, ScaleSettings scaleSettings, GameBoardSettings gameBoardSettings)
            {
                if (wandSettings.GripPoint != null)
                {
                    wandSettings.GripPoint.transform.SetPositionAndRotation(gripPose_UnityWorldSpace.position, gripPose_UnityWorldSpace.rotation);
                }

                if (wandSettings.FingertipPoint != null)
                {
                    wandSettings.FingertipPoint.transform.SetPositionAndRotation(fingertipsPose_UnityWorldSpace.position, fingertipsPose_UnityWorldSpace.rotation);
                }

                if (wandSettings.AimPoint != null)
                {
                    wandSettings.AimPoint.transform.SetPositionAndRotation(aimPose_UnityWorldSpace.position, aimPose_UnityWorldSpace.rotation);
                }
            }

            public virtual void Dispose()
            {
                Log.Info($"Glasses {glassesHandle} {controllerIndex} Wand disconnected");
            }

            #endregion Overrides


            #region Private Helper Functions

            protected Quaternion CalculateRotation(Quaternion rotToWND_GBD)
            {
                Quaternion rotToDW_GBD = Quaternion.AngleAxis(90f, Vector3.right);
                Quaternion rotToGBD_DW = Quaternion.Inverse(rotToDW_GBD);
                Quaternion rotToWND_DW = rotToWND_GBD * rotToGBD_DW;
                Quaternion rotToUGBD_UWND = new Quaternion(rotToWND_DW.x, -rotToWND_DW.y, rotToWND_DW.z, rotToWND_DW.w);
                return rotToUGBD_UWND;
            }

            protected void ProcessTrackingData(Vector3 gripPosition_UGBD, Vector3 fingertipsPosition_UGBD, Vector3 aimPosition_UGBD, Quaternion rotToUGBD_WND,
                WandSettings wandSettings, ScaleSettings scaleSettings, GameBoardSettings gameBoardSettings,
                out Pose gripPose_UGBD, out Pose fingertipsPose_UGBD, out Pose aimPose_UGBD)
            {
                var incomingGripPose_UGBD = new Pose(gripPosition_UGBD, rotToUGBD_WND);
                var incomingFingertipsPose_UGBD = new Pose(fingertipsPosition_UGBD, rotToUGBD_WND);
                var incomingAimPose_UGBD = new Pose(aimPosition_UGBD, rotToUGBD_WND);

                // Get the distance between the tracking points and the grip point.
                // Currently, when a pose is considered invalid, the position and rotation reported by the native plugin are completely zero'd out.
                // This crunches the tracking points together unless we consider the stale positions from the previous frame.
                // It also means that we don't get the desired behavior for TrackingFailureMode.FreezePosition,
                // in which the wand position freezes while still showing rotation values reported by the IMU.
                // TODO: In the native plugin, even for invalid poses (both wand and glasses, which are also affected),
                // include an offset for the tracking points, and pass through rotation data.
                var staleGripPose_UGBD = pose_UGBD;
                var staleFingertipsPose_UGBD = fingertipsPose_GameboardSpace;
                var staleAimPose_UGBD = aimPose_GameboardSpace;

                var gripPointOffsetDistance = 0f;
                var fingertipsPointOffsetDistance = Mathf.Max((fingertipsPosition_UGBD - gripPosition_UGBD).magnitude,
                    (staleFingertipsPose_UGBD.position - staleGripPose_UGBD.position).magnitude);
                var aimPointOffsetDistance = Mathf.Max((aimPosition_UGBD - gripPosition_UGBD).magnitude,
                    (staleAimPose_UGBD.position - staleGripPose_UGBD.position).magnitude);

                // Handle invalid poses
                gripPose_UGBD = FilterTrackingPointPose(staleGripPose_UGBD, staleGripPose_UGBD, incomingGripPose_UGBD, gripPointOffsetDistance, wandSettings);
                fingertipsPose_UGBD = FilterTrackingPointPose(staleGripPose_UGBD, staleFingertipsPose_UGBD, incomingFingertipsPose_UGBD, fingertipsPointOffsetDistance, wandSettings);
                aimPose_UGBD = FilterTrackingPointPose(staleGripPose_UGBD, staleAimPose_UGBD, incomingAimPose_UGBD, aimPointOffsetDistance, wandSettings);

            }

            protected Pose FilterTrackingPointPose(Pose staleGripPointPose, Pose staleTrackingPointPose,
                Pose newTrackingPointPose, float trackingPointOffsetDistance, WandSettings settings)
            {
                if (!isTracked && settings.RejectUntrackedPositionData)
                {
                    switch (settings.FailureMode)
                    {
                        default:
                        // If we have an undefined FailureMode for some reason, fall through to FreezePosition
                        case TrackableSettings.TrackingFailureMode.FreezePosition:
                            // We need to determine where to put each tracking point.
                            // We only want to freeze the position of the grip point while allowing the other
                            // points to rotate around it, colinear to the grip point's forward vector.
                            var extrapolatedPosition = staleGripPointPose.position +
                                Quaternion.Inverse(newTrackingPointPose.rotation) * Vector3.forward * trackingPointOffsetDistance;
                            return new Pose(extrapolatedPosition, newTrackingPointPose.rotation);
                        case TrackableSettings.TrackingFailureMode.FreezePositionAndRotation:
                            return staleTrackingPointPose;
                        case TrackableSettings.TrackingFailureMode.SnapToDefault:
                            return GetDefaultPoseGameboardSpace(settings);
                    }
                }
                else
                {
                    return newTrackingPointPose;
                }
            }

            #endregion Private Helper Functions
        }

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
        private class WandDeviceCore : WandCore
        {
            internal WandDevice wandDevice;

            private enum TrackingState : int
            {
                None,
                Limited,
                Tracking
            }

            public WandDeviceCore(GlassesHandle glassesHandle, ControllerIndex controllerIndex) : base(glassesHandle, controllerIndex)
            {
                if(Glasses.TryGetGlassesDevice(glassesHandle, out var glassesDevice))
                {
                    //Add the new Wand Device to the Input System only when a new Wand Core is being created
                    Input.AddWandDevice(glassesDevice.PlayerIndex, controllerIndex);
                    wandDevice = Input.GetWandDevice(glassesDevice.PlayerIndex, controllerIndex);
                    InputSystem.QueueConfigChangeEvent(wandDevice);
                    if(controllerIndex == ControllerIndex.Right)
                    {
                        glassesDevice.RightWand = wandDevice;
                    }
                    else
                    {
                        glassesDevice.LeftWand = wandDevice;
                    }
                    if(TiltFiveManager2.IsInstantiated){
                        TiltFiveManager2.Instance.RefreshInputDevicePairings();
                    }
                }
            }

            public override void Dispose()
            {
                base.Dispose();

                if (Player.TryGetPlayerIndex(glassesHandle, out var playerIndex))
                {
                    Input.RemoveWandDevice(playerIndex, controllerIndex);
                }
            }

            public override void GetLatestInputs()
            {
                base.GetLatestInputs();

                // If the wandDevice isn't added to the input system for any reason,
                // don't bother queueing any delta state events.
                if (!wandDevice.added || !wandDevice.enabled)
                {
                    return;
                }

                InputSystem.QueueDeltaStateEvent(wandDevice.TiltFive, currentState.HasValue && currentState.Value.TryGetButton(WandButton.T5));
                InputSystem.QueueDeltaStateEvent(wandDevice.One, currentState.HasValue && currentState.Value.TryGetButton(WandButton.One));
                InputSystem.QueueDeltaStateEvent(wandDevice.Two, currentState.HasValue && currentState.Value.TryGetButton(WandButton.Two));
                InputSystem.QueueDeltaStateEvent(wandDevice.Three, currentState.HasValue && currentState.Value.TryGetButton(WandButton.Three));
                InputSystem.QueueDeltaStateEvent(wandDevice.A, currentState.HasValue && currentState.Value.TryGetButton(WandButton.A));
                InputSystem.QueueDeltaStateEvent(wandDevice.B, currentState.HasValue && currentState.Value.TryGetButton(WandButton.B));
                InputSystem.QueueDeltaStateEvent(wandDevice.X, currentState.HasValue && currentState.Value.TryGetButton(WandButton.X));
                InputSystem.QueueDeltaStateEvent(wandDevice.Y, currentState.HasValue && currentState.Value.TryGetButton(WandButton.Y));

                InputSystem.QueueDeltaStateEvent(wandDevice.Stick, currentState.HasValue ? currentState.Value.TryGetStick() : Vector2.zero);
                InputSystem.QueueDeltaStateEvent(wandDevice.Trigger, currentState.HasValue ? currentState.Value.TryGetTrigger() : 0f);
            }

            protected override void SetDrivenObjectTransform(WandSettings wandSettings, ScaleSettings scaleSettings, GameBoardSettings gameBoardSettings)
            {
                base.SetDrivenObjectTransform(wandSettings, scaleSettings, gameBoardSettings);

                // If the wandDevice isn't added to the input system for any reason,
                // don't bother queueing any delta state events.
                if (!wandDevice.added || !wandDevice.enabled)
                {
                    return;
                }
            
                // Time to inject our wand state into the Input System.
                QueueDeltaStateEvent(wandDevice.devicePosition, pose_UWRLD.position);
                QueueDeltaStateEvent(wandDevice.FingertipsPosition, fingertipsPose_UnityWorldSpace.position);
                QueueDeltaStateEvent(wandDevice.AimPosition, aimPose_UnityWorldSpace.position);

                QueueDeltaStateEvent(wandDevice.RawGripPosition, pose_UGBD.position);
                QueueDeltaStateEvent(wandDevice.RawFingertipsPosition, fingertipsPose_GameboardSpace.position);
                QueueDeltaStateEvent(wandDevice.RawAimPosition, aimPose_GameboardSpace.position);

                QueueDeltaStateEvent(wandDevice.deviceRotation, pose_UWRLD.rotation);
                QueueDeltaStateEvent(wandDevice.RawRotation, pose_UGBD.rotation);

                InputSystem.QueueDeltaStateEvent(wandDevice.isTracked, isTracked);

                var trackingState = TrackingState.Tracking;
                if (!isTracked)
                {
                    trackingState = wandSettings.FailureMode == TrackableSettings.TrackingFailureMode.FreezePosition
                        ? TrackingState.Limited
                        : TrackingState.None;
                }

                InputSystem.QueueDeltaStateEvent(wandDevice.trackingState, (int)trackingState);
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
#endif

        #endregion Private Classes


        #region Public Classes

        /// <summary>
        /// Suspends coroutine execution until the provided player's wand has connected.
        /// </summary>
        /// <remarks>
        /// This custom yield instruction is useful for performing tasks that depend on
        /// a specific player's wand being connected and initialized. This is a tidy alternative to
        /// polling <see cref="Wand.TryCheckConnected(out bool, PlayerIndex, ControllerIndex)"/>
        /// in a loop or using UnityEngine.WaitUntil.
        /// </remarks>
        /// <example>
        /// Let's assume we are making a game that requires both wands (left and right) from a specific player to be connected.
        ///
        /// We may want to display a prompt for the specified player to connect both of their wands,
        /// update the prompt as the wands are connected, then dismiss the prompt once the player is ready.
        ///
        /// private IEnumerator PromptPlayerForBothWands(PlayerIndex targetPlayer)
        /// {
        ///     // Let's assume this coroutine is part of a script with a member variable called playerConnectivityPrompt.
        ///     // Start by defining some text asking the player to connect their left and right wands, then revealing the prompt.
        ///     playerConnectivityPrompt.SetText("Waiting for left and right wands...");
        ///     playerConnectivityPrompt.Show();
        ///
        ///     // Suspend this coroutine until the player's right wand has connected.
        ///     yield return new WaitUntilWandConnected(targetPlayer, ControllerIndex.Right);
        ///
        ///     // Adjust the prompt text now that we know that the right wand is connected.
        ///     playerConnectivityPrompt.SetText("Waiting for left wand...");
        ///
        ///     // Suspend this coroutine until the player's left wand has connected.
        ///     yield return new WaitUntilWandConnected(targetPlayer, ControllerIndex.Left);
        ///
        ///     // Now that both wands are connected, we can hide the prompt and end the coroutine.
        ///     playerConnectivityPrompt.Hide();
        /// }
        /// </example>
        public class WaitUntilWandConnected : CustomYieldInstruction
        {
            public override bool keepWaiting => !Player.IsConnected(playerIndex)                    // Keep waiting if the player isn't connected,
                || !Wand.TryCheckConnected(out bool wandConnected, playerIndex, controllerIndex)    // or if their wand can't be checked,
                || !wandConnected;                                                                  // or if their wand isn't connected.
            private PlayerIndex playerIndex = PlayerIndex.One;
            private ControllerIndex controllerIndex = ControllerIndex.Right;

            public WaitUntilWandConnected(PlayerIndex playerIndex, ControllerIndex controllerIndex)
            {
                this.playerIndex = playerIndex;
                this.controllerIndex = controllerIndex;
            }
        }

        #endregion
    }

}
