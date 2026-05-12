using LookingGlass.Toolkit;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LookingGlass {
    /// <summary>
    /// <para>Represents a window in the Windows OS.</para>
    /// <para>This struct contains helper methods to get and move these windows in the native OS.</para>
    /// </summary>
    public struct WindowsOSWindow {
        private static readonly Lazy<List<IntPtr>> WindowBuffer = new Lazy<List<IntPtr>>(() => new List<IntPtr>(), true);
        private static bool isEnumerating = false;

        private IntPtr handle;

        /// <summary>
        /// The native pointer to the window, usable with the Win32 API.
        /// </summary>
        public IntPtr Handle => handle;

        /// <summary>
        /// Is the pointer non-null (non-zero)?<br />
        /// NOTE: This does NOT change if the window is closed -- this is simply a null check.
        /// </summary>
        public bool IsNotNull => handle != IntPtr.Zero;

        public WindowsOSWindow(IntPtr handle) {
            this.handle = handle;
        }

        /// <summary>
        /// Gets the window's title text.
        /// </summary>
        public string GetTitle() {
            return OSWindowUtil.GetWindowTextW(handle);
        }

        /// <summary>
        /// Gets the window's rect.
        /// </summary>
        public ScreenRect GetRect() {
            ScreenRect result = new ScreenRect();
            OSWindowUtil.GetWindowRect(handle, out result);
            return result;
        }

        /// <summary>
        /// <para>Attempts to set the window's position on the screen in screen pixel coordinates, where the top-left is (0, 0), +x is right, and +y is down.</para>
        /// <para>NOTE: This will NOT have any effect on fullscreened windows, except for moving them between different screens.</para>
        /// </summary>
        /// <param name="x">The desired screen-space pixel x-coordinate.</param>
        /// <param name="y">The desired screen-space pixel y-coordinate.</param>
        /// <param name="width">The desired width of the window in pixels.</param>
        /// <param name="height">The desired height of the window in pixels.</param>
        public void SetPosition(int x, int y, int width, int height) {
            OSWindowUtil.SetWindowPos(handle, IntPtr.Zero, x, y, width, height, 0);
        }

        public void SetBorderlessWindow(int x, int y, int width, int height) {
            OSWindowUtil.SetBorderlessWindow(handle, x, y, width, height);
        }

        /// <summary>
        /// Gets all available, open windows from your Windows OS.
        /// </summary>
        public static IEnumerable<WindowsOSWindow> GetAll() {
            if (isEnumerating) {
                Debug.LogError("Failed to get all OS windows -- the logic assumes that they will not be iterated multiple times over simultaneously!");
                yield break;
            }

            isEnumerating = true;
            List<IntPtr> buffer = WindowBuffer.Value;

            try {
                OSWindowUtil.EnumWindows((IntPtr window, int unknown) => {
                    buffer.Add(window);
                    return true;
                }, 0);
                foreach (IntPtr window in buffer)
                    yield return new WindowsOSWindow(window);
            } finally {
                buffer.Clear();
                isEnumerating = false;
            }
        }
    }
}
