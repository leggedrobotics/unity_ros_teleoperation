using System;
using System.Runtime.InteropServices;
using UnityEngine;
using LookingGlass.Toolkit;

namespace LookingGlass {
    //NOTE: WINDOWS-ONLY as currently written!
    internal static class OSWindowUtil {
        private const string User32DLL = "User32";
        private const string Kernel32DLL = "Kernel32";

        // Constants for window styles
        private const int GWL_STYLE = -16; // Offset to retrieve window styles
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_FRAMECHANGED = 0x0020;
        // SWP_NOOWNERZORDER prevents the owner window from being re-ordered.
        // SWP_SHOWWINDOW displays the window after setting its position and size.
        private const uint SWP_NOOWNERZORDER = 0x0200;
        private const uint SWP_SHOWWINDOW = 0x0040;

        // Window style flags
        private const int WS_MAXIMIZE = 0x01000000;
        private const int WS_BORDER = 0x00800000;
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_THICKFRAME = 0x00040000;

        [DllImport(Kernel32DLL)]
        public static extern uint GetLastError();

        [DllImport(User32DLL)]
        public static extern bool EnumWindows(Func<IntPtr, int, bool> callbackPerWindow, int unknown);

        [DllImport(User32DLL)]
        public static extern bool GetWindowRect(IntPtr window, out ScreenRect result);

        [DllImport(User32DLL)]
        public static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int left, int top, int width, int height, uint flags);

        [DllImport(User32DLL)]
        private static extern int GetWindowTextLengthW(IntPtr window);

        [DllImport(User32DLL)]
        private static extern int GetWindowTextW(IntPtr window, [MarshalAs(UnmanagedType.LPWStr)] char[] title, int maxCharacters);
        [DllImport(User32DLL)]
        private static extern int GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport(User32DLL)]
        private static extern int SetWindowLongPtr(IntPtr hWnd, int nIndex, int dwNewLong);

        public static string GetWindowTextW(IntPtr window) {
            int length = GetWindowTextLengthW(window);
            if (length <= 0)
                return "";

            //NOTE: The last param of GetWindowTextW(...), nMaxCount, INCLUDES the NULL character, so +1!
            char[] buffer = new char[length + 1];
            int count = GetWindowTextW(window, buffer, length + 1);
            return new string(buffer, 0, length);
        }

        public static void SetBorderlessWindow(IntPtr window, int x, int y, int width, int height) {
            // Retrieve the current window style
            int style = GetWindowLongPtr(window, GWL_STYLE);
            //Debug.Log($"Current style: 0x{style:X}");

            // Modify the style (e.g., remove WS_CAPTION to hide the title bar)
            // WS_BORDER makes no difference
            int newStyle = style & ~(WS_CAPTION | WS_THICKFRAME);

            // Set the new style
            SetWindowLongPtr(window, GWL_STYLE, newStyle);

            
            // Apply the style change(this will move the window back to center)
            SetWindowPos(window, IntPtr.Zero, x, y, width, height, SWP_FRAMECHANGED | SWP_NOOWNERZORDER | SWP_SHOWWINDOW);

            style = GetWindowLongPtr(window, GWL_STYLE);
            //Debug.Log($"Current style: 0x{style:X}");


        }
    }
}
