using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using LookingGlass.Toolkit;

using ToolkitDisplay = LookingGlass.Toolkit.Display;

namespace LookingGlass
{
    public static class MacWindowing
    {
        [DllImport("libWindowController")]
        public static extern bool SetWindowRect(String name, Int32 x, Int32 y, UInt32 w, UInt32 h);

        [DllImport("libWindowController")]
        public static extern bool SetBorderless(String name);

        [DllImport("libWindowController")]
        public static extern bool SetWindowToLookingGlass(String name);

        [DllImport("libWindowController")]
        public static extern UInt32 MonitorCount();

        [DllImport("libWindowController")]
        public static extern IntPtr MonitorName(UInt32 index);

        [DllImport("libWindowController")]
        public static extern Int32 MonitorX(UInt32 index);

        [DllImport("libWindowController")]
        public static extern Int32 MonitorY(UInt32 index);

        [DllImport("libWindowController")]
        public static extern Int32 MonitorIsLookingGlass(UInt32 index);

        public static string GetCString(IntPtr cStringPtr)
        {
            string result = Marshal.PtrToStringAnsi(cStringPtr);

            // Free the memory allocated in the C function
            Marshal.FreeHGlobal(cStringPtr);
            return result;
        }

        public static int FindLkgIndex()
        {
            var count = MonitorCount();
            var output = "";
            var lkgIndex = -1;
            for (int i = 0; i < count; i++)
            {
                var index = (UInt32)i;
                var name = GetCString(MonitorName(index));
                var x = MonitorX(index);
                var y = MonitorY(index);
                var isLkg = MonitorIsLookingGlass(index);
                output += $"{i} ({x},{y}) {name} ? {isLkg} \n";
                if (isLkg == 1)
                {
                    lkgIndex = i;
                }
            }
            Debug.Log(output);
            return lkgIndex;
        }

        public static IEnumerator SetupMacWindowing(ToolkitDisplay display)
        {
            if (display == null) {
                Debug.LogError("Failed to find a display from the " + nameof(HologramCamera) + "!");
                yield break;
            }

            // first start by checking if finagling is necessary
            int lkgIndex = FindLkgIndex();
            int prefsIndex = PlayerPrefs.GetInt("UnitySelectMonitor", 1);
            int fullscreen = PlayerPrefs.GetInt("Screenmanager Fullscreen mode", 0);
            int w = PlayerPrefs.GetInt("Screenmanager Resolution Width", 0);
            int h = PlayerPrefs.GetInt("Screenmanager Resolution Height", 0);
            Debug.Log(
                $"lkgIndexArg: {lkgIndex}, prefsidx: {prefsIndex}, fs: {fullscreen}, w: {w}, h: {h}"
            );

            // if the prefs index is the same as we just read and set up correctly
            // it should have started on the lkg, so we can get out of here
            if (
                prefsIndex == lkgIndex
                && fullscreen == 1
                && w == display.calibration.screenW
                && h == display.calibration.screenH
            )
            {
                yield break;
            }

            // make sure it's windowed before attempting
            if (Screen.fullScreenMode != FullScreenMode.Windowed)
            {
                Debug.Log("setting to windowed");
                Screen.SetResolution(Screen.width, Screen.height, FullScreenMode.Windowed);

                // doesn't really work
                // yield return new WaitUntil(() => Screen.width == 1024 && Screen.height == 768);

                // this is so messy
                yield return new WaitForSecondsRealtime(2f);

                Debug.Log("done setting to windowed");
            }

            // just some padding, not sure if necessary
            yield return null;
            yield return null;

            Debug.Log(Application.productName);
            SetBorderless(Application.productName);
            SetWindowRect(Application.productName, 20, 20, (uint)1024, (uint)768);

            yield return null;
            yield return null;

            SetWindowRect(
                Application.productName,
                display.hardwareInfo.windowCoordX,
                display.hardwareInfo.windowCoordY,
                // display.hardwareInfo.windowCoordX,
                // -display.hardwareInfo.windowCoordY / 2,
                (uint) display.calibration.screenW,
                (uint) display.calibration.screenH
            );

            yield return null;
            yield return null;

            yield return new WaitForSecondsRealtime(0.5f);

            Screen.SetResolution(
                display.calibration.screenW,
                display.calibration.screenH,
                FullScreenMode.FullScreenWindow
            );

            yield return null;
            yield return null;

            PlayerPrefs.SetInt("UnitySelectMonitor", lkgIndex);
        }
    }
}
