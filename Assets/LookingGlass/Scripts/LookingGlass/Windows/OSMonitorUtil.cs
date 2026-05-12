using System;
using System.Runtime.InteropServices;
using UnityEngine;
using LookingGlass.Toolkit;

namespace LookingGlass {
    //NOTE: WINDOWS-ONLY as currently written!
    internal static class OSMonitorUtil {
        private const string User32DLL = "User32";
        private const string ShcoreDLL = "Shcore";

        [Serializable]
        public enum OSScaleFactor {
            DEVICE_SCALE_FACTOR_INVALID = 0,
            SCALE_100_PERCENT = 100,
            SCALE_120_PERCENT = 120,
            SCALE_125_PERCENT = 125,
            SCALE_140_PERCENT = 140,
            SCALE_150_PERCENT = 150,
            SCALE_160_PERCENT = 160,
            SCALE_175_PERCENT = 175,
            SCALE_180_PERCENT = 180,
            SCALE_200_PERCENT = 200,
            SCALE_225_PERCENT = 225,
            SCALE_250_PERCENT = 250,
            SCALE_300_PERCENT = 300,
            SCALE_350_PERCENT = 350,
            SCALE_400_PERCENT = 400,
            SCALE_450_PERCENT = 450,
            SCALE_500_PERCENT = 500
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct MonitorInfo {
            public int structSize;
            public ScreenRect monitorRect;
            public ScreenRect workArea;
            public int flags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string deviceName;
        }

        [DllImport(User32DLL)]
        public static extern bool EnumDisplayMonitors(
            IntPtr displayContext,
            IntPtr clippingRect,
            Func<IntPtr, IntPtr, ScreenRect, int, bool> callbackPerMonitor,
            int unknown
        );

        [DllImport(User32DLL)]
        public static extern bool GetMonitorInfoW(IntPtr monitor, ref MonitorInfo info);

        [DllImport(ShcoreDLL)]
        public static extern int GetScaleFactorForMonitor(IntPtr monitor, out OSScaleFactor scaleFactor);

        [DllImport(ShcoreDLL)]
        public static extern int GetProcessDpiAwareness(IntPtr process, out int result);

        //NOTE: We cannot set DPI awareness of an already-running program that has relied on its value already!
        //Thus, we should assume we can never set the DPI awareness of the Unity editor, nor any Unity builds, since we can't run it as early as possible in the main program before Unity initializes itself.
        //[DllImport(ShcoreDLL)]
        //public static extern int SetProcessDpiAwareness(int processDpiAwarenessValue);
    }
}
