#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
#define EXTENDED_INPUT_WINDOWS
#endif

using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LookingGlass {
    /// <summary>
    /// Enables working with extended keys which Unity does not support.
    /// </summary>
    /// <remarks>
    /// Currently this class only works on Windows. The method <see cref="GetKey(int)"/> is the only method that would
    /// needs to be updated to support other operating systems.
    /// </remarks>
    internal static class InputExtensions {
        /// <summary>
        /// Keeps track of the state of a key and frame time.
        /// </summary>
        private class KeyState {
            /// <summary>
            /// The frame when the key was updated.
            /// </summary>
            public int Frame;

            /// <summary>
            /// Whether the key was pressed on that frame.
            /// </summary>
            public bool Pressed;
        }

        private static Dictionary<int, KeyState> keyStates = new Dictionary<int, KeyState>();

#if EXTENDED_INPUT_WINDOWS
        [DllImport("User32.dll")]
        private static extern short GetKeyState(int keyCode);
#endif

        /// <summary>
        /// Gets the state of the specified key for the current frame.
        /// </summary>
        /// <param name="keyCode">The key to test.</param>
        /// <param name="isPressed">Indicates if the key was pressed.</param>
        /// <param name="didChange">Indicates if <paramref name="isPressed"/> changed on the current frame.</param>
        private static void GetKeyCurrentFrame(int keyCode, out bool isPressed, out bool didChange) {
            if (!keyStates.TryGetValue(keyCode, out KeyState keyState))
                keyStates[keyCode] = keyState = new KeyState();

            // If the state is already from the current frame, just reuse it
            if (keyState.Frame == Time.frameCount) {
                // Update the out var to use last state
                isPressed = keyState.Pressed;

                // We know it did change on this frame since the frame number is the same
                didChange = true;
            }

            // The state is not from the current frame. Get the new state.
            isPressed = GetKey(keyCode);

            // Check to see if it's changed since it was recorded.
            if (isPressed == keyState.Pressed) {
                // Nothing changed
                didChange = false;
            } else {
                // Store the frame when it changed
                keyState.Frame = Time.frameCount;

                // Store the new state
                keyState.Pressed = isPressed;

                // It did change on this frame
                didChange = true;
            }
        }

        /// <summary>
        /// Gets a value that indicates if the specified key is currently held.
        /// </summary>
        /// <param name="keyCode">The key code to test.</param>
        /// <returns>
        /// <c>true</c> if the specified key is held; otherwise <c>false</c>.
        /// </returns>
        public static bool GetKey(int keyCode) {
#if EXTENDED_INPUT_WINDOWS
            byte[] result = System.BitConverter.GetBytes(GetKeyState(keyCode));
            if (result[0] > 1) { return true; }
#endif

            // TODO: How do we handle this on Mac and other platforms?

            return false;
        }

        /// <summary>
        /// Gets a value that indicates if the specified key is currently held.
        /// </summary>
        /// <param name="keyCode">
        /// The <see cref="PortraitKeyCode"/> to test.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified key is held; otherwise <c>false</c>.
        /// </returns>
        public static bool GetKey(PortraitKeyCode keyCode) { return GetKey((int) keyCode); }

        /// <summary>
        /// Gets a value that indicates if the specified key was pressed on the current frame.
        /// </summary>
        /// <param name="keyCode">
        /// The key to test.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified key was pressed on the current frame; otherwise <c>false</c>.
        /// </returns>
        public static bool GetKeyDown(int keyCode) {
            GetKeyCurrentFrame(keyCode, out bool isPressed, out bool didChange);
            return didChange && isPressed;
        }

        /// <summary>
        /// Gets a value that indicates if the specified key was pressed on the current frame.
        /// </summary>
        /// <param name="keyCode">The <see cref="PortraitKeyCode"/> to test.</param>
        /// <returns>
        /// <c>true</c> if the specified key was pressed on the current frame; otherwise <c>false</c>.
        /// </returns>
        public static bool GetKeyDown(PortraitKeyCode keyCode) => GetKeyDown((int) keyCode);

        /// <summary>
        /// Gets a value that indicates if the specified key was released on the current frame.
        /// </summary>
        /// <param name="keyCode">The key to test.</param>
        /// <returns>
        /// <c>true</c> if the specified key was released on the current frame; otherwise <c>false</c>.
        /// </returns>
        public static bool GetKeyUp(int keyCode) {
            GetKeyCurrentFrame(keyCode, out bool isPressed, out bool didChange);
            return didChange && !isPressed;
        }

        /// <summary>
        /// Gets a value that indicates if the specified key was released on the current frame.
        /// </summary>
        /// <param name="keyCode">The <see cref="PortraitKeyCode"/> test.</param>
        /// <returns>
        /// <c>true</c> if the specified key was released on the current frame; otherwise <c>false</c>.
        /// </returns>
        public static bool GetKeyUp(PortraitKeyCode keyCode) => GetKeyUp((int) keyCode);
    }
}
