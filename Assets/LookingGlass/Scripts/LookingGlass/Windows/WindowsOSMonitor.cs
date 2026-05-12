using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using LookingGlass.Toolkit;

namespace LookingGlass {
    [Serializable]
    public struct WindowsOSMonitor {
        [Serializable]
        private struct RawData {
            public IntPtr handle;
            public ScreenRect nonScaledRect;
            public ScreenRect scaledRect;
            public float scalingFactor;
        }

        private static readonly Lazy<List<RawData>> WindowBuffer
            = new Lazy<List<RawData>>(() => new List<RawData>(), true);
        private static bool isEnumerating = false;
        public const int DefaultDPI = 96;

        private IntPtr handle;
        [SerializeField] private RectInt nonScaledRect;
        [SerializeField] private RectInt scaledRect;
        [SerializeField] private float scalingFactor;

        public IntPtr Handle => handle;

        /// <summary>
        /// Is the pointer non-null (non-zero)?<br />
        /// NOTE: This does NOT change if the window is closed -- this is simply a null check.
        /// </summary>
        public bool IsNotNull => handle != IntPtr.Zero;

        public RectInt NonScaledRect => nonScaledRect;
        public RectInt ScaledRect => scaledRect;
        public float ScalingFactor => scalingFactor;

        private WindowsOSMonitor(RawData rawData) {
            handle = rawData.handle;
            nonScaledRect = rawData.nonScaledRect.ToUnityRect();
            scaledRect = rawData.scaledRect.ToUnityRect();
            scalingFactor = rawData.scalingFactor;
        }

        //We use division here, because a 150% monitor zoom in means we have LESS space for our "150% larger pixels".
        //A (1920, 1080) native monitor would have space to display (1280, 720) pixels scaled at 150%.
        public Vector2 ScalePoint(Vector2 unscaledPointRelativeToMonitor) {
            return unscaledPointRelativeToMonitor / scalingFactor;
        }

        //Here, we re-expand back to unscaled pixel coordinates.
        public Vector2 UnscalePoint(Vector2 scaledPointRelativeToMonitor) {
            return scaledPointRelativeToMonitor * scalingFactor;
        }

        public static IEnumerable<WindowsOSMonitor> GetAll() {
            if (isEnumerating) {
                Debug.LogError("Failed to get all OS windows -- the logic assumes that they will not be iterated multiple times over simultaneously!");
                yield break;
            }

            isEnumerating = true;
            List<RawData> buffer = WindowBuffer.Value;

            try {
                OSMonitorUtil.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                    (IntPtr monitor, IntPtr displayContext, ScreenRect osRect, int unknown) => {
                        float scale = 1;

                        OSMonitorUtil.GetScaleFactorForMonitor(monitor, out OSMonitorUtil.OSScaleFactor scaleFactor);
                        if (scaleFactor != OSMonitorUtil.OSScaleFactor.DEVICE_SCALE_FACTOR_INVALID)
                            scale = ((float) (int) scaleFactor) / 100;

                        buffer.Add(new RawData() {
                            handle = monitor,
                            nonScaledRect = osRect,
                            scaledRect = osRect,
                            scalingFactor = scale
                        });
                        return true;
                    }, 0
                );

                ApplyScalingToPositions(buffer);

                foreach (RawData data in buffer)
                    yield return new WindowsOSMonitor(data);
            } finally {
                buffer.Clear();
                isEnumerating = false;
            }
        }

        //NOTE: This method assumes that EnumDisplayMonitors doesn't bake in the scaling into the OS-returned rectangles.
        //For example, even if a (1920, 1080) monitor is at 150% scaling,
        //We expect that EnumDisplayMonitors still returns a rect with a size of (1920, 1080).
        
        //In the method below, we apply that scaling (its effective size in that example would become (1280, 720)),
        //and then shift all other display(s) that are affected by this size change.
        private static void ApplyScalingToPositions(List<RawData> data) {
            Assert.IsNotNull(data);
            Assert.IsTrue(data.Count > 0, "Failed to find any monitor data! (The list of monitor data had 0 elements)");

            data.Sort((RawData a, RawData b) => {
                //ASC by y-position (top-left corner)
                int sortValue = a.nonScaledRect.top - b.nonScaledRect.top;
                
                //(Tiebreak for multiple monitors at same y-position) then, ASC by x-position (top-left corner)
                if (sortValue == 0)
                    sortValue = a.nonScaledRect.left - b.nonScaledRect.left;

                return sortValue;
            });
            int indexOfMainMonitor = data.FindIndex(d => d.nonScaledRect.left == 0 && d.nonScaledRect.top == 0);
            Assert.IsTrue(indexOfMainMonitor >= 0, "Failed to find the main monitor with a top-left position of (0, 0)!");

            for (int i = indexOfMainMonitor; i < data.Count; i++) {
                RawData monitor = data[i];
                //We use division here, because a 150% monitor zoom in means we have LESS space for our "150% larger pixels".
                //A (1920, 1080) native monitor would have space to display (1280, 720) pixels scaled at 150%.
                float scale = 1 / monitor.scalingFactor;
                monitor.scaledRect.right = monitor.scaledRect.left + (int) (scale * monitor.scaledRect.Width);
                monitor.scaledRect.bottom = monitor.scaledRect.top + (int) (scale * monitor.scaledRect.Height);

                data[i] = monitor;

                //For any monitors arranged below and to the right of this monitor,
                //We need to shift them by the amount of pixels that changed.
                int shiftX = monitor.scaledRect.Width - monitor.nonScaledRect.Width;
                int shiftY = monitor.scaledRect.Height - monitor.nonScaledRect.Height;
                for (int j = i + 1; j < data.Count; j++) {
                    RawData otherMonitor = data[j];
                    if (shiftY != 0 && otherMonitor.nonScaledRect.top > monitor.nonScaledRect.top) {
                        int diffY = otherMonitor.nonScaledRect.top - monitor.nonScaledRect.top;
                        float diffYPercent = (float) diffY / monitor.nonScaledRect.Height;

                        diffYPercent = Mathf.Min(1, diffYPercent);
                        int proratedShiftY = (int) (diffYPercent * shiftY);
                        
                        otherMonitor.scaledRect.top += proratedShiftY;
                        otherMonitor.scaledRect.bottom += proratedShiftY;
                    }

                    if (shiftX != 0 && otherMonitor.nonScaledRect.left > monitor.nonScaledRect.left) {
                        int diffX = otherMonitor.nonScaledRect.left - monitor.nonScaledRect.left;
                        float diffXPercent = (float) diffX / monitor.nonScaledRect.Width;

                        diffXPercent = Mathf.Min(1, diffXPercent);
                        int proratedShiftX = (int) (diffXPercent * shiftX);

                        otherMonitor.scaledRect.left += proratedShiftX;
                        otherMonitor.scaledRect.right += proratedShiftX;
                    }

                    data[j] = otherMonitor;
                }
            }
        }
    }
}
