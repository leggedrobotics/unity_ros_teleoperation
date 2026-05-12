using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using LookingGlass.Toolkit;

namespace LookingGlass.Editor {
    [InitializeOnLoad]
    [Serializable]
    public class PreviewWindow : ScriptableObject {
        static PreviewWindow() {
            GameViewExtensions.editorIsGameViewPaired = IsPaired;
        }

        #region Pairs
        //NOTE: I would've used a dictionary, but if it was a Dictionary<HologramCamera, PreviewWindow>,
        //      the keys could get destroyed and we might have trouble with multiple null values as keys.
        private static List<PreviewWindow> all = new();

        public static int Count => all.Count;
        public static IEnumerable<PreviewWindow> All {
            get {
                Clean();
                foreach (PreviewWindow preview in all)
                    yield return preview;
            }
        }

        public static bool IsPaired(EditorWindow gameView) {
            if (gameView == null)
                throw new ArgumentNullException(nameof(gameView));

            Clean();
            return all.Exists(p => p.GameView == gameView);
        }

        public static PreviewWindow GetPreview(HologramCamera camera) {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));
            Clean();
            return all.Where(p => p.hologramCamera == camera).FirstOrDefault();
        }

        private static void Clean() {
            for (int i = all.Count - 1; i >= 0; i--) {
                PreviewWindow preview = all[i];
                if (preview == null || preview.GameView == null)
                    Close(i);
            }
        }

        public static bool IsPreviewOpenForCamera(HologramCamera camera) {
            if (camera == null)
                return false;
            Clean();
            return all.Exists(p => p.hologramCamera == camera);
        }
        public static bool IsPreviewOpenForDevice(string serial) {
            Clean();
            return all.Exists(p => p.hologramCamera.TargetLKG == serial);
        }

        private static void Close(int index) {
            try {
                Assert.IsTrue(index >= 0 && index < all.Count, nameof(index) + " should be in range [0, " + all.Count + "), but was " + index + " instead.");

                PreviewWindow preview = all[index];
                all.RemoveAt(index);

                if (preview != null)
                    ScriptableObject.DestroyImmediate(preview);
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        public static void Close(HologramCamera camera) {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));

            int index = -1;
            PreviewWindow preview = null;
            for (int i = 0; i < all.Count; i++) {
                if (all[i].hologramCamera == camera) {
                    index = i;
                    preview = all[i];
                    break;
                }
            }
            if (preview == null) {
                Debug.LogError("Failed to close " + camera + "'s preview window! It couldn't be found.");
                return;
            }

            Close(index);
        }

        public static void CloseAll() {
            for (int i = all.Count - 1; i >= 0; i--)
                Close(i);
        }
        #endregion

        private const string WindowNamePrefix = "LookingGlass Game View";

        public static PreviewWindow Create(HologramCamera camera) {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));
            if (IsPreviewOpenForCamera(camera))
                throw new InvalidOperationException(camera + " already has a game view created for it!");

            PreviewWindow preview = ScriptableObject.CreateInstance<PreviewWindow>();

            //Accounts for the case that the PreviewWindow destroys itself during Awake or OnEnable
            if (preview == null) {
                Debug.LogWarning("The preview window destroyed itself before being able to be used!");
                return null;
            }

            try {
                preview.name = WindowNamePrefix;

                if (all == null)
                    all = new List<PreviewWindow>();

                if (all.Count == 1) {
                    EditorApplication.wantsToQuit += CloseAllAndAcceptQuit;
                }

                preview.hologramCamera = camera;
                preview.RecreateGameView();
                EditorApplication.update += preview.OnUpdate;
                Undo.undoRedoPerformed += preview.OnUndoRedo;

                all.Add(preview);
                return preview;
            } catch (Exception e) {
                Debug.LogException(e);
                ScriptableObject.DestroyImmediate(preview);
                return null;
            }
        }

        private static List<WindowsOSMonitor> monitors;
        private static bool supportsExperimentalDisplayDPIScaling =
#if UNITY_EDITOR_WIN && !UNITY_6000_0_OR_NEWER
            true
#else
            false
#endif
            ;

        [SerializeField] private EditorWindow gameView;
        [SerializeField] private HologramCamera hologramCamera;
        [SerializeField] private WindowsOSMonitor matchingMonitor;

        private bool setCustomRenderSize = false;
        private Rect lastPos;
        private int frameCount = 0;

        public HologramCamera HologramCamera => hologramCamera;
        public EditorWindow GameView => gameView;

        #region Unity Messages
        //NOTE: For some reason, Unity auto-destroys this ScriptableObject when loading a new scene..
        private void OnDestroy() {
            EditorApplication.update -= OnUpdate;
            Undo.undoRedoPerformed -= OnUndoRedo;

            if (gameView != null) {
                gameView.Close();
                EditorWindow.DestroyImmediate(gameView);
            }

            if (hologramCamera != null) {
                hologramCamera.ClearCustomLenticularRegion();
                hologramCamera.renderBlackIfAny.Remove(RenderBlackIf);
            }

            if (all != null) {
                all.Remove(this);
                if (all.Count == 0) {
                    EditorApplication.wantsToQuit -= CloseAllAndAcceptQuit;
                }
            }
        }
        #endregion

        /// <summary>
        /// <para>Forcibly re-positions the GameView that's associated with the preview for the given <see cref="HologramCamera"/>.</para>
        /// <para>This is required when the camera switches which LKG display it's targetting, since Unity and/or the OS may prevent the window from moving properly between monitors once positioned.</para>
        /// </summary>
        public void ForceReposition() {
            RecreateGameView();
        }

        //NOTE: We recreate the game view more often than necessary because if we rely on Reflection too much,
        //the game view(s) get stuck. Recreating the whole window seems to flush Unity's internal state, and prevent that!
        private EditorWindow RecreateGameView() {
            if (this.gameView != null) {
                this.gameView.Close();
                EditorWindow.DestroyImmediate(this.gameView);
            }

            frameCount = 0;
            lastPos = default;

            hologramCamera.renderBlackIfAny.Remove(RenderBlackIf);
            hologramCamera.renderBlackIfAny.Add(RenderBlackIf);

            EditorWindow gameView = (EditorWindow) EditorWindow.CreateInstance(LookingGlass.GameViewExtensions.GameViewType);
#if UNITY_EDITOR_WIN
            gameView.Show();
#else
            //NOTE: On MacOS Big Sur (Intel) with Unity 2019.4, there was a 25px bottom-bar of unknown origin messing up the preview window.
            //This weird bottom bar did NOT occur on Unity 2018.4.
            //Either way, ShowUtility() made this issue go away.
            gameView.ShowUtility();
#endif
            gameView.titleContent = new GUIContent(name);
            this.gameView = gameView;

            if (frameCount >= 5) {
                InitializeWithHologramCamera();
            }

            return gameView;
        }

        private bool RenderBlackIf() => frameCount <= 7;

        private void OnUndoRedo()
        {
            if (this == null)
            {
                //NOTE: Not sure why OnDestroy is not picking this up first.. but let's check ourselves anyway.
                Undo.undoRedoPerformed -= OnUndoRedo;
                return;
            }

            if (hologramCamera == null) {
                Debug.LogWarning("The target " + nameof(LookingGlass.HologramCamera) + " component was destroyed. Closing its preview window.");
                DestroyImmediate(this);
                return;
            }

            if (gameView == null) {
                Debug.LogWarning("The editor preview window was closed.");
                DestroyImmediate(this);
                return;
            }
            // logic to handle undo/redo here, we need to reset render size
            setCustomRenderSize = false;
            UpdateFromResolutionIfNeeded();
        }


        private void OnUpdate() {
            if (this == null) {
                //NOTE: Not sure why OnDestroy is not picking this up first.. but let's check ourselves anyway.
                EditorApplication.update -= OnUpdate;
                return;
            }

            if (hologramCamera == null) {
                Debug.LogWarning("The target " + nameof(LookingGlass.HologramCamera) + " component was destroyed. Closing its preview window.");
                DestroyImmediate(this);
                return;
            }

            if (gameView == null) {
                Debug.LogWarning("The editor preview window was closed.");
                DestroyImmediate(this);
                return;
            }

            if (frameCount < 5 && (!LKGDisplaySystem.IsLoading || LKGDisplaySystem.LKGDisplayCount > 0)) {
                Rect position = gameView.position;
                if (position != lastPos) {
                    lastPos = position;
                    try {
                        InitializeWithHologramCamera();
                    } catch (Exception e) {
                        Debug.LogError("An error occurred while updating the preview window! It will be closed.");
                        Debug.LogException(e);
                        DestroyImmediate(this);
                        return;
                    }
                }
            }

            if (frameCount == 7) {
                if (hologramCamera.Preview2D)
                    hologramCamera.RenderPreview2D();
                else
                    hologramCamera.RenderQuilt();
                EditorApplication.QueuePlayerLoopUpdate();
                gameView.Repaint();
            }

            frameCount++;
        }

        private static bool CloseAllAndAcceptQuit() {
            CloseAll();
            return true;
        }

        private Rect CalculateIdealPosition() {
            RectInt unscaledRect;
            bool useManualPreview = Preview.UseManualPreview;
            Calibration cal = hologramCamera.Calibration;
            ScreenRect screenRect = hologramCamera.DisplayRect;

            if (useManualPreview) {
                ManualPreviewSettings settings = Preview.ManualPreviewSettings;
                unscaledRect = new RectInt(settings.position, settings.resolution);
            } else {
                unscaledRect = new RectInt(screenRect.left, screenRect.top, screenRect.Width, screenRect.Height);
            }

            RectInt scaledRect;

            int indexInList = -1;
            if (!useManualPreview && supportsExperimentalDisplayDPIScaling) {
                if (monitors == null)
                    monitors = new List<WindowsOSMonitor>();
                else
                    monitors.Clear();
                monitors.AddRange(WindowsOSMonitor.GetAll());
                indexInList = monitors.FindIndex((WindowsOSMonitor monitor) => monitor.NonScaledRect.Equals(unscaledRect));
            }

            if (indexInList >= 0) {
                matchingMonitor = monitors[indexInList];
                scaledRect = matchingMonitor.ScaledRect;
            } else {
                if (!useManualPreview && supportsExperimentalDisplayDPIScaling)
                    Debug.LogWarning("Unable to find a monitor matching the unscaled rect of " + unscaledRect + " from HoPS calibration data. " +
                        "The preview window might not handle DPI screen scaling properly.");

                scaledRect = unscaledRect;
            }

            //NOTE: When testing different resolutions, we must currently anchor the preview window to the bottom-left of the LKG device's screen.
            //This keeps the center visually consistent.
            //We've never tried to recalculate center values in the calibration data, though it might be possible.

            //After a few frames, we need to re-check to see what Unity allowed our position rect to be!
            //It will automatically resize to avoid going outside the screen, or overlapping the Windows taskbar.
            Rect idealRect = new Rect(scaledRect.position, scaledRect.size);

            return idealRect;
        }
        private void InitializeWithHologramCamera() {
            Assert.IsNotNull(hologramCamera);
            setCustomRenderSize = false;

            Rect idealRect = CalculateIdealPosition();

            //The default maxSize is usually good enough (Unity mentions 4000x4000),
            //But if we're on an 8K LKG device, this isn't large enough!
            //Just to be sure, let's check our maxSize is large enough for the ideal rect we want to set our size to:
            Vector2 prevMaxSize = gameView.maxSize;
            if (prevMaxSize.x < idealRect.width ||
                prevMaxSize.y < idealRect.height)
                gameView.maxSize = idealRect.size;

            if (frameCount < 1)
                gameView.position = idealRect;

            if (!Preview.UseManualPreview) {
                //THIS ONLY WORKS WHEN DOCKED: Which never helps us lol..
                //gameView.maximized = true;

                //INSTEAD, let's do:
                gameView.AutoClickMaximizeButtonOnWindows();
            }

            gameView.SetFreeAspectSize();

#if UNITY_EDITOR_OSX
            gameView.SetShowToolbar(true);
#endif


#if UNITY_EDITOR_WIN && UNITY_6000_0_OR_NEWER
            // only for Unity6 on Windows, we need to force the window to be truely fullscreen
            EditorUpdates.Delay(6, () => ForceBorderlessWindow(idealRect));
#else
            UpdateFromResolutionIfNeeded();
#endif
        }

        private void ForceBorderlessWindow(Rect idealRect) {
#if UNITY_EDITOR_WIN
            IEnumerable<IntPtr> windowPtrs = UnityBuildWindowUtil.GetGameViewWindowPtrs();
            if (windowPtrs.Any()) // Check if there are any IntPtr values
            {
                foreach (IntPtr intPtr in windowPtrs) {
                    WindowsOSWindow window = new WindowsOSWindow(intPtr);

                    // Process the valid IntPtr values
                    window.SetBorderlessWindow((int)idealRect.x, (int)idealRect.y, (int)idealRect.width, (int)idealRect.height);

                }

            } else {
                // Handle the case where the return is empty
                Debug.Log("No game view window pointers found.");
            }
#endif
            UpdateFromResolutionIfNeeded();
        }

        private void UpdateFromResolutionIfNeeded() {
            Rect position = gameView.position;
            Calibration cal = hologramCamera.Calibration;
            Vector2 area = gameView.GetTargetSize();

            ScreenRect region;
            if (!Preview.UseManualPreview && supportsExperimentalDisplayDPIScaling) {
                //NOTE: The calibration works when using NON-scaled pixel coordinate values.
                //Even though this EditorWindow needs SCALED pixel coordinate values.
                Vector2Int scaledPos = new Vector2Int(
                    (int) position.x + (int) area.x,
                    (int) position.y + (int) area.y
                );

                Vector2Int scaledOffset = scaledPos - matchingMonitor.ScaledRect.position;
                Vector2Int unscaledOffset = Vector2Int.RoundToInt(matchingMonitor.UnscalePoint(scaledOffset));
                Vector2Int unscaledPos = unscaledOffset + matchingMonitor.NonScaledRect.position;

                Vector2Int scaledSize = new Vector2Int(
                    (int) area.x,
                    (int) area.y
                );

                Vector2Int unscaledSize = Vector2Int.RoundToInt(matchingMonitor.UnscalePoint(scaledSize));

                //Calibration uses NON-scaled values:
                region = new ScreenRect() {
                    left = unscaledPos.x,
                    top = unscaledPos.y,
                    right = unscaledPos.x + unscaledSize.x,
                    bottom = unscaledPos.y + unscaledSize.y
                };
            } else {
                region = new ScreenRect()
                {
                    left = (int)position.x,
                    top = (int)position.y,
                    right = (int)position.x + (int)area.x,
                    bottom = (int)position.y + (int)area.y
                };

                // override region only for M1 Mac and Unity6+
#if UNITY_6000_0_OR_NEWER && UNITY_EDITOR_OSX
                //Debug.Log($"area {area} region {region}");
                string processorType = SystemInfo.processorType;
                if (area.x >= 2 * cal.screenW || area.y >= 2 * cal.screenH)
                {
                    //Debug.Log("the area is wrong so we use cal's res instead");
                    // The area on Mac is 2x the resolution so we hardcoded it to cal's screen W and H
                    region = new ScreenRect()
                    {
                        left = (int)position.x,
                        top = 71,// hard-coded the top area for Mac M1
                        right = (int)position.x + (int)cal.screenW,
                        bottom = (int)position.y + (int)cal.screenH
                    };
                }
#endif
            }

            if (!setCustomRenderSize && !region.Equals(hologramCamera.LenticularRegion)) {
                hologramCamera.UseCustomLenticularRegion(region);
                hologramCamera.RenderQuilt(forceRender: true);
                setCustomRenderSize = true;
            }

            gameView.SetGameViewTargetDisplay((int) hologramCamera.TargetDisplay);
            gameView.SetGameViewZoom();
        }
    }
}
