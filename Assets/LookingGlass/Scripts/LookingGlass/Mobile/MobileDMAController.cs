using UnityEngine;
using UnityEngine.Events;
using LookingGlass.Toolkit;

using Display = UnityEngine.Display;

#if UNITY_IOS
using System.Threading.Tasks;
using System.IO;
#endif

namespace LookingGlass.Mobile {
    /// <summary>
    /// Allows you to use your mobile device (iOS/Android) as a display with a given resolution, and activate a 2nd display fullscreen for the <see cref="HologramCamera"/> when a 2nd display is plugged in and detected.
    /// </summary>
    /// <remarks>
    /// The default resolution for the 2D screen (on your mobile device) is 600x1000,
    /// which is less than mobile native resolutions likely for performance reasons, but can be any values you'd like it to render (fullscreen) at that are less than are equal to your device's native resolution.
    /// </remarks>
    public class MobileDMAController : MonoBehaviour {
        [Tooltip("The " + nameof(HologramCamera) + " that will be used IFF there is a second display connected.\n\n" +
            "The second display (connected to the mobile device) is assumed to be the Looking Glass display.\n" +
            "If set (non-null), this camera's " + nameof(GameObject) + " will be deactivated until a 2nd display is plugged in.\n\n" +
            "Once the second display is detected,\n" +
            "1. The second display will be activated (fullscreened) to its native resolution, and\n" +
            "2. This camera's " + nameof(GameObject) + " will be activated, and set to target " + nameof(HologramCamera.DisplayTarget.Display2) + " (the 2nd display, aka index 1).")]
        [SerializeField] private HologramCamera camera;

        [Header("Optional")]
        [Tooltip("The amount of pixels in width you'd like the main display's window to render at (on your mobile device's built-in display).\n" +
            "This can be less than your mobile device's native resolution width.\n\n" +
            "Set to 0 to use the native resolution width of the iOS device.")]
        private int width2D = 600;

        [Tooltip("The amount of pixels in height you'd like the main display's window to render at (on your mobile device's built-in display).\n" +
            "This can be less than your mobile device's native resolution height.\n\n" +
            "Set to 0 to use the native resolution height of the iOS device.")]
        private int height2D = 1000;

        private string loadedVisualJsonPath = "";

        public HologramCamera Camera => camera;
        public int Width2D => width2D;
        public int Height2D => height2D;

        public string LoadedVisualJsonPath {
            set {
                loadedVisualJsonPath = value;
                RefreshDisplays();
            }
            get { return loadedVisualJsonPath; }
        }
        public static string VisualJsonFilePath => UserStorage.PersistentDataJSONPath;

        public static event UnityAction<Calibration> onCalibrationLoaded;

        private void OnValidate() {
            if (camera == null)
                Reset();
        }


        private void Reset() {
            //TODO: I wonder if we can get a callback like this Reset() message,
            //  But ALSO for when we (the user) drags the MobileDMAController prefab
            //  from their Project view into the scene?
            camera = HologramCamera.Instance;

            if (camera != null)
                camera.targetDisplay = 
#if UNITY_EDITOR
                    HologramCamera.DisplayTarget.Display2;
#else
                    HologramCamera.DisplayTarget.Display1;
#endif

        }

        private void Start() {
            Display[] displays = Display.displays;
            Display main = displays[0];
            width2D = main.systemWidth;
            height2D = main.systemHeight;
            Set2DScreenResolution(width2D, height2D);

            Reset();

#if !UNITY_EDITOR
            if (camera != null)
                camera.gameObject.SetActive(displays.Length == 2);
#endif
        }

        private void OnEnable() {
            Display.onDisplaysUpdated += RefreshDisplays;
        }

        private void OnDisable() {
            Display.onDisplaysUpdated -= RefreshDisplays;
        }

        //NOTE:
        //  This did NOT work when called from [RuntimeInitializeOnLoadMethod(...)] during before NOR after scene load, so
        //  Keeping this called in this script's Start() and separate from HologramCamera.cs is the best for now.

        /// <summary>
        /// This starts the 2D monitor (on the iPhone or mobile device) at the given screen resolution of width2D x height2D.
        /// </summary>
        private void Set2DScreenResolution(int width, int height) {
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
        }

        public void RefreshDisplays() {
            Display[] displays = Display.displays;
            bool hasTwoDisplays = displays.Length == 2;

            // If there are two displays, connect to the Looking Glass
            if (hasTwoDisplays) {
                Display second = displays[1];

                if (!second.active
                    || second.renderingWidth != second.systemWidth
                    || second.renderingHeight != second.systemHeight) {
                    Debug.Log("External Display activated, resolution: " + second.systemWidth + "x" + second.systemHeight);
                    second.Activate(second.systemWidth, second.systemHeight,
#if UNITY_2022_2_OR_NEWER
                        new RefreshRate() {
                            numerator = 60,
                            denominator = 1
                        }
#else
                        60
#endif
                    );
                }
            }

            if (camera != null) {
#if !UNITY_EDITOR
                camera.gameObject.SetActive(hasTwoDisplays);
#endif
                if (hasTwoDisplays) {
                    camera.TargetDisplay = HologramCamera.DisplayTarget.Display2;
#if UNITY_IOS
                    RefreshDisplaysMobile();

                    _ = CheckToSetRotatedScreen();
#endif
                }
            }
        }

#if UNITY_IOS
        private async Task<bool> CheckToSetRotatedScreen() {
            await camera.WaitForInitialization();

            Calibration cal = camera.Calibration;
            LKGDeviceType deviceType = cal.GetDeviceType();
            if (deviceType == LKGDeviceType._16inPortraitGen3 || deviceType == LKGDeviceType._27inPortraitGen3) {
                Debug.Log("[" + nameof(MobileDMAController) + "] Determined that the lenticular output to the display should be rotated!");
                Shader rotatedLenticular = Util.FindShader("LookingGlass/Lenticular (Rotated 90 CW)");
                if (rotatedLenticular != null) {
                    Debug.Log("[" + nameof(MobileDMAController) + "] Applying the rotated lenticular shader...");
                    camera.Debugging.OverrideLenticularShader = rotatedLenticular;
                    return true;
                } else {
                    Debug.LogError("[" + nameof(MobileDMAController) + "] Unable to find rotated lenticular shader.");
                }
            }
            else {
                camera.Debugging.OverrideLenticularShader = null;
            }
            return false;
        }

        private void RefreshDisplaysMobile()
        {
            string calibrationPath = loadedVisualJsonPath;

            if (!Path.Equals(calibrationPath, VisualJsonFilePath) && File.Exists(calibrationPath))
            {
                // copy the file to local
                File.Copy(calibrationPath, VisualJsonFilePath, true);
                Debug.Log($"[{nameof(MobileDMAController)}] Copied {VisualJsonFilePath} to {calibrationPath}");
            }

            calibrationPath = VisualJsonFilePath;

            if (File.Exists(calibrationPath))
            {
                string visualJson = File.ReadAllText(calibrationPath);
                Calibration calibration = Calibration.Parse(visualJson);
                camera.Debugging.UseManualCalibration(calibration);

                calibration = camera.Calibration;
                Debug.Log($"[{nameof(MobileDMAController)}] Loaded Calibration for: {calibration.serial}, type: {calibration.GetDeviceType()} from {calibrationPath}");
                onCalibrationLoaded?.Invoke(calibration);
            }
            else
            {
                Debug.Log($"[MobileDMAController] Could not find calibration file at {calibrationPath}");
            }
        }
#endif
    }
}
