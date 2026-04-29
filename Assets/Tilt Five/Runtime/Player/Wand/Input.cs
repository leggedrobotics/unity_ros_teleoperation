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
using UnityEngine;

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
using UnityEngine.InputSystem;
#endif

using TiltFive.Logging;

namespace TiltFive
{
    /// <summary>
    /// Provides access to Wand inputs
    /// </summary>
    public static class Input
    {
        #region Public Enums

        public enum WandButton : UInt32
        {
            T5      = 1 << 0,
            One     = 1 << 1,
            Two     = 1 << 2,
            Three   = 1 << 7,
            Y       = 1 << 3,
            B       = 1 << 4,
            A       = 1 << 5,
            X       = 1 << 6,
            [Obsolete("WandButton.System is deprecated, please use Wandbutton.T5 instead.")]
            System  = T5,
            [Obsolete("WandButton.Z is deprecated, please use Wandbutton.Three instead.")]
            Z       = Three,
        }

        #endregion Public Enums


        #region Private Fields

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
        internal static GlassesDevice[] glassesDevices = new GlassesDevice[PlayerSettings.MAX_SUPPORTED_PLAYERS];
        internal static WandDevice[,] wandDevices = new WandDevice[PlayerSettings.MAX_SUPPORTED_PLAYERS, 2];
#endif

        #endregion


        #region Public Functions

        /// <summary>
        /// Whether the indicated wand button is currently being pressed.
        /// </summary>
        /// <param name="button">The wand button to check.</param>
        /// <param name="controllerIndex">Unless specified, the state of the right-hand wand is returned.</param>
        /// <remarks>If the wand is unavailable, this function returns a default value of false.</remarks>
        /// <returns>Returns true if the button is being pressed</param>
        /// <param name="playerIndex">The index of the player using the wand we want to query.</param>
        /// <returns></returns>
        public static bool GetButton(WandButton button,
            ControllerIndex controllerIndex = ControllerIndex.Right, PlayerIndex playerIndex = PlayerIndex.One)
        {
            return TryGetButton(button, out var pressed, controllerIndex, playerIndex) && pressed;
        }

        /// <summary>
        /// Whether the indicated wand button is currently being pressed. Fails if the wand is unavailable.
        /// </summary>
        /// <param name="button">The wand button to check.</param>
        /// <param name="pressed"></param>
        /// <param name="controllerIndex">Unless specified, the state of the right-hand wand is returned.</param>
        /// <param name="playerIndex">The index of the player using the wand we want to query.</param>
        /// <remarks>If the wand is unavailable, this function returns false and <paramref name="pressed"/> is set to a default value of false.</remarks>
        /// <returns>Returns true if the button state was successfully obtained.</returns>
        public static bool TryGetButton(WandButton button, out bool pressed,
            ControllerIndex controllerIndex = ControllerIndex.Right, PlayerIndex playerIndex = PlayerIndex.One)
        {
            if(!Player.TryGetGlassesHandle(playerIndex, out var glassesHandle))
            {
                pressed = false;
                return false;
            }

            return Wand.TryGetButton(button, out pressed, glassesHandle, controllerIndex);
        }

        /// <summary>
        /// Whether the indicated wand button was pressed during this frame.
        /// </summary>
        /// <param name="button">The wand button to check.</param>
        /// <param name="controllerIndex">Unless specified, the state of the right-hand wand is returned.</param>
        /// <param name="playerIndex">The index of the player using the wand we want to query.</param>
        /// <remarks>If the wand is unavailable, this function returns a default value of false.</remarks>
        /// <returns>Returns true if the button was pressed during this frame.</returns>
        public static bool GetButtonDown(WandButton button,
            ControllerIndex controllerIndex = ControllerIndex.Right, PlayerIndex playerIndex = PlayerIndex.One)
        {
            return TryGetButtonDown(button, out var buttonDown, controllerIndex, playerIndex) && buttonDown;
        }

        /// <summary>
        /// Whether the indicated wand button was pressed during this frame. Fails if the wand is unavailable.
        /// </summary>
        /// <param name="button">The wand button to check.</param>
        /// <param name="buttonDown">Whether the button was pressed during this frame.</param>
        /// <param name="controllerIndex">Unless specified, the state of the right-hand wand is returned.</param>
        /// <param name="playerIndex">The index of the player using the wand we want to query.</param>
        /// <remarks>If the wand is unavailable, this function returns false and <paramref name="buttonDown"/> is given a default value of false.</remarks>
        /// <returns>Returns true if the button state was successfully obtained.</returns>
        public static bool TryGetButtonDown(WandButton button, out bool buttonDown,
            ControllerIndex controllerIndex = ControllerIndex.Right, PlayerIndex playerIndex = PlayerIndex.One)
        {
            // The specified player's glasses aren't even connected, let alone the wand. No way to get a rising edge here.
            if(!Player.TryGetGlassesHandle(playerIndex, out var glassesHandle))
            {
                buttonDown = false;
                return false;
            }

            return Wand.TryGetButtonDown(button, out buttonDown, glassesHandle, controllerIndex);
        }

        /// <summary>
        /// Whether the indicated wand button was released during this frame.
        /// </summary>
        /// <param name="button">The wand button to check.</param>
        /// <param name="controllerIndex">Unless specified, the state of the right-hand wand is returned.</param>
        /// <param name="playerIndex">The index of the player using the wand we want to query.</param>
        /// <returns>Returns true if the button was released this frame, or false if the wand is unavailable.</returns>
        public static bool GetButtonUp(WandButton button,
            ControllerIndex controllerIndex = ControllerIndex.Right, PlayerIndex playerIndex = PlayerIndex.One)
        {
            return TryGetButtonUp(button, out var buttonUp, controllerIndex, playerIndex) && buttonUp;
        }

        /// <summary>
        /// Whether the indicated wand button was released during this frame. Fails if the wand is unavailable.
        /// </summary>
        /// <param name="button">The wand button to check.</param>
        /// <param name="buttonUp">Whether the button was released during this frame.</param>
        /// <param name="controllerIndex">Unless specified, the state of the right-hand wand is returned.</param>
        /// <param name="playerIndex">The index of the player using the wand we want to query.</param>
        /// <remarks>If the wand is unavailable, this function returns false and
        /// <paramref name="buttonUp"/> is set to the return value of <see cref="GetButtonUp(WandButton, WandTarget)"/> GetButtonUp.</remarks>
        /// <returns>Returns true if the button state was successfully obtained.</returns>
        public static bool TryGetButtonUp(WandButton button, out bool buttonUp,
            ControllerIndex controllerIndex = ControllerIndex.Right, PlayerIndex playerIndex = PlayerIndex.One)
        {
            // TODO: Tweak the Wand.cs TryGetButtonDown to check if the glasses disconnected this frame.
            // If it did, and the button was held last frame, then buttonUp can be true.
            if(!Player.TryGetGlassesHandle(playerIndex, out var glassesHandle))
            {
                buttonUp = false;
                return false;
            }

            return Wand.TryGetButtonUp(button, out buttonUp, glassesHandle, controllerIndex);
        }

        /// <summary>
        /// Gets the direction and magnitude of the stick's tilt for the indicated wand.
        /// </summary>
        /// <param name="controllerIndex">Unless specified, the state of the right-hand wand joystick is used.</param>
        /// <param name="playerIndex">The index of the player using the wand we want to query.</param>
        /// <returns>Returns a vector representing the direction and magnitude of the stick's tilt.</returns>
        public static Vector2 GetStickTilt(ControllerIndex controllerIndex = ControllerIndex.Right, PlayerIndex playerIndex = PlayerIndex.One)
        {
            if(TryGetStickTilt(out var stickTilt, controllerIndex, playerIndex))
            {
                return stickTilt;
            }
            return Vector2.zero;
        }

        /// <summary>
        /// Gets the direction and magnitude of the stick's tilt for the indicated wand. Fails if the wand is unavailable.
        /// </summary>
        /// <param name="stickTilt">A vector representing the direction and magnitude of the stick's tilt.</param>
        /// <param name="controllerIndex">Unless specified, the state of the right-hand wand joystick is used.</param>
        /// <param name="playerIndex">The index of the player using the wand we want to query.</param>
        /// <returns>Returns true if the joystick state was successfully obtained.</returns>
        public static bool TryGetStickTilt(out Vector2 stickTilt,
            ControllerIndex controllerIndex = ControllerIndex.Right, PlayerIndex playerIndex = PlayerIndex.One)
        {
            if(!Player.TryGetGlassesHandle(playerIndex, out var glassesHandle))
            {
                stickTilt = Vector2.zero;
                return false;
            }
            return Wand.TryGetStickTilt(out stickTilt, glassesHandle, controllerIndex);
        }

        /// <summary>
        /// Gets the degree to which the trigger is depressed, from 0.0 (released) to 1.0 (fully depressed).
        /// </summary>
        /// <param name="controllerIndex">Unless specified, the state of the right-hand wand trigger is used.</param>
        /// <param name="playerIndex">The index of the player using the wand we want to query.</param>
        /// <returns>Returns a float representing how much the trigger has depressed by the user,
        /// from 0.0 (released) to 1.0 (fully depressed).</returns>
        public static float GetTrigger(ControllerIndex controllerIndex = ControllerIndex.Right, PlayerIndex playerIndex = PlayerIndex.One)
        {
            if(TryGetTrigger(out var triggerDisplacement, controllerIndex, playerIndex))
            {
                return triggerDisplacement;
            }
            return 0f;
        }

        /// <summary>
        /// Gets the degree to which the trigger is depressed, from 0.0 (released) to 1.0 (fully depressed). Fails if the wand is unavailable.
        /// </summary>
        /// <param name="triggerDisplacement">A float representing how much the trigger has depressed by the user,
        /// from 0.0 (released) to 1.0 (fully depressed).</param>
        /// <param name="controllerIndex">Unless specified, the state of the right-hand wand trigger is used.</param>
        /// <param name="playerIndex">The index of the player using the wand we want to query.</param>
        /// <returns>Returns true if the trigger state was successfully obtained.</returns>
        public static bool TryGetTrigger(out float triggerDisplacement,
            ControllerIndex controllerIndex = ControllerIndex.Right, PlayerIndex playerIndex = PlayerIndex.One)
        {
            if (!Player.TryGetGlassesHandle(playerIndex, out var glassesHandle))
            {
                triggerDisplacement = 0f;
                return false;
            }
            return Wand.TryGetTrigger(out triggerDisplacement, glassesHandle, controllerIndex);
        }

        /// <summary>
        /// Gets the connection status of the indicated wand.
        /// </summary>
        /// <param name="controllerIndex">Unless specified, the right-hand wand is queried.</param>
        /// <returns>Returns true if connected, false otherwise.</returns>
        public static bool GetWandAvailability(ControllerIndex controllerIndex = ControllerIndex.Right)
        {
            return Wand.TryCheckConnected(out var connected, PlayerIndex.One, controllerIndex) && connected;
        }

        public static void Update()
        {
            // Deleting this function would be appropriate, but since it's public,
            // there's a small chance it could break someone's code...
            // ...not that I can think of a use-case that involves calling TiltFive.Input.Update()
        }

        #endregion Public Functions


        #region Internal Functions

        internal static bool GetButton(this T5_ControllerState controllerState, WandButton button)
        {
            var buttonsState = controllerState.ButtonsState;

            switch (button)
            {
                case WandButton.T5:
                    return buttonsState.T5;
                case WandButton.One:
                    return buttonsState.One;
                case WandButton.Two:
                    return buttonsState.Two;
                case WandButton.Y:
                    return buttonsState.Y;
                case WandButton.B:
                    return buttonsState.B;
                case WandButton.A:
                    return buttonsState.A;
                case WandButton.X:
                    return buttonsState.X;
                case WandButton.Three:
                    return buttonsState.Three;
                default:
                    throw new ArgumentException("Invalid WandButton argument - enum value does not exist");
            }

        }

        internal static bool TryGetButton(this T5_ControllerState controllerState, WandButton button)
        {
            return controllerState.ButtonsValid && controllerState.GetButton(button);
        }

        internal static Vector2 TryGetStick(this T5_ControllerState controllerState)
        {
            return controllerState.AnalogValid ? controllerState.Stick : Vector2.zero;
        }

        internal static float TryGetTrigger(this T5_ControllerState controllerState)
        {
            return controllerState.AnalogValid ? controllerState.Trigger : 0f;
        }

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
        internal static GlassesDevice GetGlassesDevice(PlayerIndex playerIndex)
        {
            return glassesDevices[(int)playerIndex - 1];
        }

        internal static WandDevice GetWandDevice(PlayerIndex playerIndex, ControllerIndex controllerIndex)
        {
            return wandDevices[(int)playerIndex - 1, (int)controllerIndex];
        }
#endif

        #endregion Internal Functions


        #region Private Functions

        static Input()
        {
            Wand.Scan();
        }

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
        internal static void AddGlassesDevice(PlayerIndex playerIndex)
        {
            var glassesDevices = Input.glassesDevices;
            int i = (int)playerIndex - 1;

            // Create a GlassesDevice if necessary
            if (glassesDevices[i] == null)
            {
                var preexistingGlassesDevice = InputSystem.GetDevice<GlassesDevice>($"Player{playerIndex}");
                if (preexistingGlassesDevice != null)
                {
                    glassesDevices[i] = preexistingGlassesDevice;
                    glassesDevices[i].PlayerIndex = playerIndex;
                }
                else
                {
                    glassesDevices[i] = InputSystem.AddDevice<GlassesDevice>($"T5 Glasses - Player {playerIndex}");
                    glassesDevices[i].PlayerIndex = playerIndex;
                    InputSystem.AddDeviceUsage(glassesDevices[i], $"Player{playerIndex}");
                }

            }
            else if (!glassesDevices[i].added)
            {
                InputSystem.AddDevice(glassesDevices[i]);
            }
        }

        internal static void AddWandDevice(PlayerIndex playerIndex, ControllerIndex controllerIndex)
        {
            var wandDevices = Input.wandDevices;
            int i = (int)playerIndex - 1;
            int j = (int)controllerIndex;

            // Unfortunately, the enum ControllerIndex.Primary still exists, and Unity seems to have a habit
            // of substituting its display name when we're trying to get the display name for ControllerIndex.Right.
            // TODO: Localize
            var handednessLabel = controllerIndex == ControllerIndex.Right ? "Right" : "Left";

            // If we already know about a wandDevice corresponding to our input parameters, add it to the input system if it isn't already added
            if (wandDevices[i, j] != null && !wandDevices[i, j].added)
            {
                var wandDevice = wandDevices[i, j];
                InputSystem.AddDevice(wandDevice);
                wandDevice.ControllerIndex = controllerIndex;
                InputSystem.QueueConfigChangeEvent(wandDevice);
                InputSystem.AddDevice(wandDevice);
            }
            else
            {
                // Otherwise, ask the input system if it has a matching wandDevice.
                // This corner case (in which a matching wandDevice exists, but the static field in Player.cs is empty)
                // can occur when reloading scripts. Unity's input system keeps the device alive, but this class suffers amnesia.
                WandDevice currentWandDevice = InputSystem.GetDevice<WandDevice>($"Player{playerIndex}-{handednessLabel}Hand");

                // If the input system does have a matching wandDevice, just remember it and we're done
                if (currentWandDevice != null)
                {
                    wandDevices[i, j] = currentWandDevice;
                }
                // Otherwise, add a brand new wandDevice to the input system and remember it.
                else
                {
                    wandDevices[i, j] = InputSystem.AddDevice<WandDevice>($"T5 Wand - P{(int)playerIndex} {handednessLabel}");
                }

                wandDevices[i, j].playerIndex = playerIndex;
                wandDevices[i, j].ControllerIndex = controllerIndex;
                InputSystem.AddDeviceUsage(wandDevices[i, j], $"Player{playerIndex}");
                InputSystem.AddDeviceUsage(wandDevices[i, j], $"Player{playerIndex}-{handednessLabel}Hand");
                InputSystem.AddDeviceUsage(wandDevices[i, j], wandDevices[i, j].ControllerIndex == ControllerIndex.Left ? CommonUsages.LeftHand : CommonUsages.RightHand);
                InputSystem.QueueConfigChangeEvent(wandDevices[i, j]);
            }
        }

        internal static void RemoveGlassesDevice(PlayerIndex playerIndex)
        {
            var glassesDevices = Input.glassesDevices;
            int i = (int)playerIndex - 1;

            // Destroy a GlassesDevice if it exists
            if (glassesDevices[i] != null)
            {
                var preexistingGlassesDevice = InputSystem.GetDevice<GlassesDevice>($"Player{playerIndex}");
                if (preexistingGlassesDevice != null)
                {
                    InputSystem.RemoveDevice(preexistingGlassesDevice);
                    glassesDevices[i] = null;
                }
            }
        }

        internal static void RemoveWandDevice(PlayerIndex playerIndex, ControllerIndex controllerIndex)
        {
            var wandDevices = Input.wandDevices;
            int i = (int)playerIndex - 1;
            int j = (int)controllerIndex;

            // Unfortunately, the enum ControllerIndex.Primary still exists, and Unity seems to have a habit
            // of substituting its display name when we're trying to get the display name for ControllerIndex.Right.
            // TODO: Localize
            var handednessLabel = controllerIndex == ControllerIndex.Right ? "Right" : "Left";

            // If we already know about a wandDevice corresponding to our input parameters, Remove the Device and let the system know if it's disappearance
            if (wandDevices[i, j] != null && !wandDevices[i, j].added)
            {
                var wandDevice = wandDevices[i, j];
                InputSystem.RemoveDevice(wandDevice);
                InputSystem.QueueConfigChangeEvent(wandDevice);
                wandDevices[i, j] = null;
            }
            else
            {
                // Otherwise, ask the input system if it has a matching wandDevice.
                // This corner case (in which a matching wandDevice exists, but the static field in Player.cs is empty)
                // can occur when reloading scripts. Unity's input system keeps the device alive, but this class suffers amnesia.
                WandDevice currentWandDevice = InputSystem.GetDevice<WandDevice>($"Player{playerIndex}-{handednessLabel}Hand");

                // If the input system does have a matching wandDevice, just destroy it
                if (currentWandDevice != null)
                {
                    InputSystem.RemoveDevice(currentWandDevice);
                    InputSystem.QueueConfigChangeEvent(currentWandDevice);
                    wandDevices[i, j] = null;
                }
            }
        }
#endif

        #endregion Private Functions
    }

}
