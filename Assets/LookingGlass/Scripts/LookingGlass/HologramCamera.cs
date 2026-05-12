//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using LookingGlass.Toolkit;

#if HAS_URP
using UnityEngine.Rendering.Universal;
#endif

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

#if HAS_NEWTONSOFT_JSON
using Newtonsoft.Json.Linq;
#endif

using ToolkitDisplay = LookingGlass.Toolkit.Display;

namespace LookingGlass {
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [HelpURL("https://docs.lookingglassfactory.com/Unity/Scripts/HologramCamera/")]
    [DefaultExecutionOrder(DefaultExecutionOrder)]
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public partial class HologramCamera : MonoBehaviour {
        private static List<HologramCamera> all = new List<HologramCamera>(8);

        /// <summary>
        /// Called when <see cref="All"/> is updated due to OnEnable or OnDisable on any <see cref="HologramCamera"/> component.
        /// </summary>
        public static event Action onListChanged;
        public static event Action<HologramCamera> onAnyInitialized;

        internal static event Action<HologramCamera> onAnyQuiltSettingsChanged;
        internal static event Action<HologramCamera> onAnyCalibrationReloaded;

        /// <summary>
        /// <para>
        /// Determines whether or not errors will be silenced.<br />
        /// These errors may log depending on the graphics API your project is built with, due to Unity's <see cref="UnityEngine.Display"/> API's limited support non-DirectX-based graphics APIs.
        /// </para>
        /// <para>Note that in order for setting this to take effect, you must set this value before any <see cref="HologramCamera"/> initializes during its Awake.</para>
        /// <para>See also: <seealso cref="DefaultExecutionOrder"/>, <seealso cref="RuntimeInitializeOnLoadMethodAttribute"/>, <seealso cref="RuntimeInitializeLoadType.BeforeSceneLoad"/></para>
        /// </summary>
        /// <example>
        /// <code>
        /// [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        /// private static void SupressErrorsExample() {
        ///     HologramCamera.SuppressDisplayWindowingErrors = true;
        /// }
        /// </code>
        /// </example>
        public static bool SuppressDisplayWindowingErrors { get; set; } = false;

        /// <summary>
        /// The most-recently enabled <see cref="HologramCamera"/> component, or <c>null</c> if there is none.
        /// </summary>
        public static HologramCamera Instance {
            get {
                if (all == null || all.Count <= 0)
                    return null;
                return all[all.Count - 1];
            }
        }

        public static int Count => all.Count;
        public static bool AnyEnabled => all.Count > 0;
        internal static Func<HologramCamera, bool> UsePostProcessing { get; set; }

        public static HologramCamera Get(int index) => all[index];
        public static IEnumerable<HologramCamera> All {
            get {
                foreach (HologramCamera h in all)
                    yield return h;
            }
        }

        public static void UpdateAllCalibrations() {
            foreach (HologramCamera h in all)
                if (!h.AreQuiltSettingsLockedForRecording)
                    h.UpdateCalibration();
        }

        private static void RegisterToList(HologramCamera hologramCamera) {
            Assert.IsNotNull(hologramCamera);
            all.Add(hologramCamera);
            onListChanged?.Invoke();
        }

        private static void UnregisterFromList(HologramCamera hologramCamera) {
            Assert.IsNotNull(hologramCamera);
            all.Remove(hologramCamera);
            onListChanged?.Invoke();
        }

        #region Versions
        internal static bool isDevVersion = false;

        private static PluginVersionAsset versionAsset;

        public static bool IsVersionLoaded {
            get {
                try {
                    //We expect that sometimes (such as during serialization callbacks), we won't be able to load
                    //the version via the Resources API.
                    EnsureAssetIsLoaded();
                } catch { }
                return versionAsset != null;
            }
        }

        /// <summary>
        /// The currently-running version of the LookingGlass Unity Plugin.
        /// </summary>
        public static SemanticVersion Version {
            get {
                EnsureAssetIsLoaded();
                return versionAsset.Version;
            }
        }

#if UNITY_EDITOR
        static HologramCamera() {
            //NOTE: Cannot load from Resources in static constructor
            EditorApplication.update += AutoLoadVersion;
        }
#endif


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoLoadVersion() {
#if UNITY_EDITOR
            EditorApplication.update -= AutoLoadVersion;
#endif
            EnsureAssetIsLoaded();
        }

        internal static readonly Version MajorHDRPRefactorVersion = new Version(1, 5, 0);
        internal static bool IsUpdatingBetween(Version previous, Version next, Version threshold)
            => previous < next && previous < threshold && next >= threshold;

        private static void EnsureAssetIsLoaded() {
            if (versionAsset == null)
                versionAsset = Resources.Load<PluginVersionAsset>("Plugin Version");
        }

        #endregion

        public const int DefaultExecutionOrder = -1000;
        private const string SingleViewCameraName = "Single-View Camera";
        private const string PostProcessCameraName = "Post-Process Camera";
        private const string FinalScreenCameraName = "Final Screen Camera";

        public const float DefaultNearClipFactor = 1.5f;
        public const float DefaultFarClipFactor = 4;

        [SerializeField, HideInInspector] private SerializableVersion lastSavedVersion;


        [Tooltip("In builds, this forces the main window to snap to the LKG display, despite being associated with your primary display (Display index 0).\n\n" +
            "Set this to false for 2-monitor setups that require screen-based raycasting (including clicking in the UI) to match to the correct screen. Otherwise, leave this set to true.\n\n" +
            "Has no effect in the Unity editor.\n\n" +
            "The default value is true.\n"
        )]
        [SerializeField] internal bool forceDisplayIndex = true;

        [Tooltip("The Unity display that this camera will render to. This will be overwritten to always be display 1 if \"Force Display Index\" is toggled on.")]
        [SerializeField] internal DisplayTarget targetDisplay;

        //NOTE: Data duplication below for lkgName and lkgIndex, because we need to save (serialize) them! (Because the calibration data is NOT serialized)
        [Tooltip("The serial of the Looking Glass (LKG) device that this component is connected with.\n\n" +
            "A " + nameof(Camera) + " component is only connected to 1 device at a time.")]
        [SerializeField] internal string targetLKG;

        [Tooltip("The type of device that is being emulated.\n\n" +
            "Only appears in the inspector if there are no Looking Glass displays plugged in or recognized, otherwise, the HologramCamera will target device settings from one of the connected LKG devices instead.")]
        [SerializeField] internal LKGDeviceType emulatedDevice = LKGDeviceType.PortraitGen2;

        [Tooltip("Allows you to preview your content in 2D instead of 3D. This can be useful for debugging.")]
        [SerializeField] private bool preview2D = false;

        [Tooltip("Does this camera show a hologram preview in the inspector and scene view?\n" +
            "Note that this only has an effect in the Unity editor, not in builds.")]
        [SerializeField] private bool showHologramPreview = false;

        [Tooltip("Should post-processing be enabled after rendering the quilt?\n\n" +
            "This only effects URP.")]
        [SerializeField] internal bool urpPostProcessing = true;
//#if UNITY_6000_0_OR_NEWER
        [Tooltip("urp renderer\n\n" + "This only effects URP.")]
        [SerializeField] internal int urpScriptableRenderer = 0;
//#endif
        [Tooltip("The realtime quilt rendered by this Holoplay capture.")]
        [SerializeField] internal RenderTexture quiltTexture;

        [Tooltip("If set to true, QuiltTexture will save to an asset in your project (useful for debugging).")]
        [SerializeField] private bool useQuiltAsset;

        [Tooltip("Determines whether or not to use automatic/default quilt settings for the currently-targeted Looking Glass display, or use custom arbitrary quilt settings instead.\n\n" +
            "(See the " + nameof(LKGDeviceType) + " enum for all possible types of Looking Glass displays).")]
        [SerializeField] internal bool automaticQuiltPreset = true;
        [SerializeField] internal QuiltPreset quiltPreset;

        [Tooltip("Defines a sequence of rendering commands that can be mixed together in the form of quilt textures.\n\n" +
            "This can be useful, for example, if you wish to mix 2D cameras, pre-rendered quilts, or quilt videos with the LookingGlass capture's realtime renderer.")]
        [SerializeField] internal RenderStack renderStack = new RenderStack();


        [FormerlySerializedAs("cameraData")]
        [SerializeField, HideInInspector] private HologramCameraProperties cameraProperties = new HologramCameraProperties();
        [SerializeField, HideInInspector] private HologramCameraGizmos gizmos = new HologramCameraGizmos();
        [SerializeField, HideInInspector] private HologramCameraEvents events = new HologramCameraEvents();
        [SerializeField, HideInInspector] private OptimizationProperties optimization = new OptimizationProperties();
        [SerializeField, HideInInspector] private HologramCameraDebugging debugging = new HologramCameraDebugging();


        //NOTE: Duplicate logic with QuiltCapture.initialized
        /// <summary>
        /// Allows us to initialize immediately during Awake,
        /// and re-initialize on every subsequence OnEnable call after being disabled and re-enabled.
        /// </summary>
        private bool initializationStarted = false;
        private bool initialized = false;
        private TaskCompletionSource<bool> initializationTcs = new();

        private Camera singleViewCamera;
        private Camera postProcessCamera;
        private Camera finalScreenCamera;
        private
#if UNITY_EDITOR
            new
#endif
            MultiViewRenderer renderer;

        [NonSerialized] private bool hadPreview2D = false; //Used for detecting changes in the editor
        [NonSerialized] private bool wasSavingQuilt;

        internal Material lenticularMaterial;

        //NOTE: This is because Unity does NOT support null values for inline serialization, so lkgDisplay will never be null as long as Unity is serializing it.
        [SerializeField] internal bool isEmulatingDevice;
        [SerializeField] internal ToolkitDisplay lkgDisplay = new();
        [SerializeField] internal LKGDeviceTemplate emulatedDeviceTemplate = new();

        [NonSerialized] internal bool isUsingCustomLenticularRegion = false;
        [NonSerialized] internal ScreenRect lenticularRegion;

        private bool frameRendered;
        private bool frameRendered2DPreview;
        private bool debugInfo;

        private GraphicsFormat quiltTextureOriginalFormatUsed;
        private RenderTexture preview2DRT;
        private RenderTexture singleViewRT;
        internal List<Func<bool>> renderBlackIfAny = new();
#if UNITY_EDITOR || !UNITY_IOS
        private QuiltCapture currentCapture;
#endif

        private List<Component> hideFlagsObjects = new List<Component>();

        private RenderTexture depthQuiltTexture;
        internal bool clearDirtyFlag = false;

        internal SubpixelCell[] normalizedSubpixelCells;
        internal ComputeBuffer subpixelCellBuffer;

        public event Action onTargetDisplayChanged;
        public event Action onQuiltChanged;
        public event Action onQuiltSettingsChanged;
        public event Action onCalibrationChanged;

        public bool Preview2D {
            get { return preview2D; }
            set {
                hadPreview2D = preview2D = value;

                //If we need anything to change immediately when setting Preview2D, we can do that here
            }
        }

        public bool ShowHologramPreview {
            get { return showHologramPreview; }
            set {
                showHologramPreview = value;
#if UNITY_EDITOR
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
#endif
            }
        }

        public bool URPPostProcessing {
            get { return urpPostProcessing; }
            set { urpPostProcessing = value; }
        }

        public RenderStack RenderStack => renderStack;

        public HologramCameraProperties CameraProperties => cameraProperties;
        public HologramCameraGizmos Gizmos => gizmos;
        public HologramCameraEvents Events => events;
        public OptimizationProperties Optimization => optimization;
        public HologramCameraDebugging Debugging => debugging;
        public IEnumerable<PropertyGroup> PropertyGroups {
            get {
                yield return cameraProperties;
                yield return gizmos;
                yield return events;
                yield return optimization;
                yield return debugging;
            }
        }

        public bool ForceDisplayIndex {
            get { return forceDisplayIndex; }
        }

        public DisplayTarget TargetDisplay {
            get { return targetDisplay; }
            set {
                targetDisplay = value;
                if (finalScreenCamera != null)
                    finalScreenCamera.targetDisplay = (int)targetDisplay;
                onTargetDisplayChanged?.Invoke();
            }
        }

        public bool HasTargetDevice => !string.IsNullOrWhiteSpace(targetLKG);

        public string TargetLKG {
            get { return targetLKG; }
            set {
                targetLKG = value;
                UseDefaultFilterMode();
                UpdateCalibration();
            }
        }

        /// <summary>
        /// <para>
        /// The type of device that is being emulated.
        /// </para>
        /// <para>
        /// Only appears in the inspector if there are no Looking Glass displays plugged in or recognized, otherwise, the HologramCamera will target device settings from one of the connected LKG devices instead.
        /// </para>
        /// </summary>
        public LKGDeviceType EmulatedDevice {
            get { return emulatedDevice; }
            set {
                emulatedDevice = value;
                UseDefaultFilterMode();
                UseAutomaticQuiltSettings();
                UpdateCalibration();
            }
        }

        /// <summary>
        /// The type of Looking Glass display that is being emulated or targeted (currently identified based on its Calibration serial field).
        /// </summary>
        public LKGDeviceType DeviceType => isEmulatingDevice ? emulatedDevice : lkgDisplay.calibration.GetDeviceType(); // TODO why we are not using Calibration.GetDeviceType()?

        /// <summary>
        /// <para>
        /// Determines whether or not to use automatic/default quilt settings for the currently-targeted Looking Glass display, or use custom arbitrary quilt settings instead.
        /// </para>
        /// <para>
        /// To change this programmatically, use one of the following:
        /// <list type="bullet">
        /// <item><see cref="UseAutomaticQuiltSettings"/></item>
        /// <item><see cref="UseCustomQuiltSettings(QuiltSettings)"/></item>
        /// <item><see cref="SetQuiltPreset(bool, QuiltPreset)"/></item>
        /// </list>
        /// </para>
        /// </summary>
        public bool AutomaticQuiltPreset => automaticQuiltPreset;

        /// <summary>
        /// Same as <see cref="QuiltSettings"/>, but has extra fields to determine if the quilt settings are the default for the targeted or emulated device, or if they are arbitrary, custom settings.
        /// </summary>
        public QuiltPreset QuiltPreset => quiltPreset;

        /// <summary>
        /// Defines the resolution and layout of the quilt texture that this camera is rendering to as output.
        /// </summary>
        public QuiltSettings QuiltSettings => quiltPreset.QuiltSettings;

        /// <summary>
        /// The realtime quilt rendered by this HologramCamera.
        /// </summary>
        public RenderTexture QuiltTexture {
            get {
                if (isActiveAndEnabled) {
                    if (NeedsQuiltResetup())
                        SetupQuilt();
                }
                return quiltTexture;
            }
        }

        /// <summary>
        /// The material with the lenticular shader, used in the final graphics blit to the screen.
        /// It accepts the quilt texture as its main texture.
        /// </summary>
        public Material LenticularMaterial {
            get {
                if (lenticularMaterial == null)
                    MultiViewRendering.CreateLenticularMaterial(ref lenticularMaterial, Debugging.OverrideLenticularShader);
                return lenticularMaterial;
            }
        }

        public bool UseQuiltAsset {
            get { return useQuiltAsset; }
            set {
                wasSavingQuilt = useQuiltAsset = value;
#if UNITY_EDITOR
                SetupQuilt();
                if (useQuiltAsset)
                    SaveOrUseQuiltAsset();
#endif
            }
        }

        public bool Initialized => initialized;
        public Task WaitForInitialization() => initializationTcs.Task;

        //How the cameras work:
        //1. The finalScreenCamera begins rendering automatically, since it is enabled.
        //2. The singleViewCamera renders into RenderTextures,
        //        either for rendering the quilt, or the 2D preview.
        //3. Then, the postProcessCamera is set to render no Meshes, and discards its own RenderTexture source.
        //        INSTEAD, it takes a RenderTexture (quiltRT) from LookingGlass.cs and blits it with the lenticular shader back into the RenderTexture.
        //4. Finally, the finalScreenCamera blits the result ONTO THE SCREEN.(A camera required for that), since its targetTexture is always null.

        /// <summary>
        /// <para>Renders individual views of the scene, where each view may be composited into the <see cref="LookingGlass"/> quilt.</para>
        /// <para>When in 2D preview mode, only 1 view is rendered directly to the screen.</para>
        /// <para>This camera is not directly used for rendering to the screen. The results of its renders are used as intermediate steps in the rendering process.</para>
        /// </summary>
        public Camera SingleViewCamera => singleViewCamera;

        /// <summary>
        /// <para>The <see cref="Camera"/> used apply final post-processing to a single view of the scene, or a quilt of the scene.</para>
        /// <para>This camera is not directly used for rendering to the screen. It is only used for applying graphical changes in internal <see cref="RenderTexture"/>s.</para>
        /// </summary>
        public Camera PostProcessCamera => postProcessCamera;

        /// <summary>
        /// The camera used for blitting the final <see cref="RenderTexture"/> to the screen.<br />
        /// In Unity, the easiest and best-supported way to do this is by using a Camera directly.
        /// </summary>
        internal Camera FinalScreenCamera => finalScreenCamera;

        internal MultiViewRenderer Renderer => renderer;

        public RenderTexture Preview2DRT {
            get {
                if (preview2DRT == null || !frameRendered2DPreview)
                    RenderPreview2D();
                return preview2DRT;
            }
        }

        /// <summary>
        /// <para>
        /// Contains Looking Glass display-specific data required for accurately rendering to your given display.
        /// </para>
        /// <para>
        /// If you are emulating a display, a default, template calibration is returned.
        /// </para>
        /// </summary>
        public Calibration Calibration {
            get {
                // TODO confirm this with Carlos
                if (Debugging.ManualCalibrationMode != ManualCalibrationMode.None)
                    return manualCalibration;
                if (isEmulatingDevice)
                    return emulatedDeviceTemplate.calibration;
                return lkgDisplay.calibration;
            }
        }

        /// <summary>
        /// Returns a copy of the Display info of the currently connected and targeted Looking Glass display, if any, or
        /// <c>null</c> if the HologramCamera is not targeting a connected display and is emulating a display instead.
        /// </summary>
        public ToolkitDisplay DisplayInfo {
            get {
                if (isEmulatingDevice)
                    return null;
                return new ToolkitDisplay(lkgDisplay);
            }
        }
        public ScreenRect DisplayRect => GetFullScreenRect();


        /// <summary>
        /// <para>
        /// Defines whether or not this camera is rendering to a different region than the native LKG display's entire screen (native resolution).<br />
        /// The area of the screen this camera is rendering to is defined by <see cref="LenticularRegion"/>.<br />
        /// This feature is used to render the preview window in the Unity editor, due to the following:
        /// <list type="bullet">
        /// <item>The Unity editor window's title bar can't easily/consistently be hidden across Windows, MacOS, and Linux.</item>
        /// <item>The OS task bar may prevent the window from becoming full-screen native resolution on the LKG display.</item>
        /// </list>
        /// </para>
        ///
        /// See also:
        /// <list type="bullet">
        /// <item>See also: <seealso cref="Calibration.screenW"/></item>
        /// <item>See also: <seealso cref="Calibration.screenH"/></item>
        /// <item>See also: <seealso cref="ToolkitDisplay.hardwareInfo"/></item>
        /// <item>See also: <seealso cref="LookingGlass.Toolkit.DisplayInfo.windowCoordX"/></item>
        /// <item>See also: <seealso cref="LookingGlass.Toolkit.DisplayInfo.windowCoordY"/></item>
        /// </list>
        /// </summary>
        public bool IsUsingCustomResolution => isUsingCustomLenticularRegion;

        /// <summary>
        /// <para>
        /// The area of the screen this camera is rendering to, measured in pixels according to
        /// the OS virtual screen coordinates, which typically start at (0, 0) on the top-left of your primary display.
        /// </para>
        /// See: <see cref="IsUsingCustomResolution"/>
        /// </summary>
        public ScreenRect LenticularRegion => lenticularRegion;

        /// <summary>
        /// <para>
        /// Determines whether or not a solid black color is rendered.
        /// This overrides all rendering, and is used for better UX in the Unity editor (to avoid seeing inaccurate frames while Unity GUI is setting up).
        /// </para>
        /// <para>This always return <c>false</c> when <see cref="AreQuiltSettingsLockedForRecording"/> is <c>true</c>, so that quilt recordings do not accidentally contain black frames.</para>
        /// </summary>
        internal bool RenderBlack {
            get {
                if (AreQuiltSettingsLockedForRecording)
                    return false;
                foreach (Func<bool> possibility in renderBlackIfAny)
                    if (possibility != null && possibility())
                        return true;
                return false;
            }
        }

        internal RenderTexture DepthQuiltTexture {
            get { return depthQuiltTexture; }
            set { depthQuiltTexture = value; }
        }

        public bool AreQuiltSettingsLockedForRecording =>
#if UNITY_EDITOR || !UNITY_IOS
            currentCapture != null;
#else
            false;
#endif
        public bool IsSameDevice(HologramCamera other) {
            if (other == null)
                return false;
            if (isEmulatingDevice || isEmulatingDevice)
                return false;
            return Calibration.IsSameDevice(other.Calibration);
        }

        #region Unity Messages
        internal void OnValidate() {
#if UNITY_EDITOR
            //NOTE: We delay this because changed events may be called, causing re-renders, which are not allowed during OnValidate
            EditorApplication.delayCall += () => {
                //NOTE: If entering/exiting playmode, make sure we skip the delayed OnValidate if our MonoBehaviour object no longer exists from before
                if (this == null)
                    return;

                if (preview2D != hadPreview2D)
                    Preview2D = preview2D;
                if (useQuiltAsset != wasSavingQuilt)
                    UseQuiltAsset = useQuiltAsset;
            };
#endif
        }

        //WARNING: When clicking on the HologramCamera prefab, it does NOT get Awake/OnEnable calls to initialize itself!
        private void Awake() {
            initializationStarted = true;
            Initialize();
        }

        private void OnEnable() {
            if (!initializationStarted) {
                initializationStarted = true;
                Initialize();
            }
        }

        private void OnDisable() {
            initializationStarted = false;
            initialized = false;
            initializationTcs = new();
            debugging.onShowAllObjectsChanged -= SetAllObjectHideFlags;

            UnregisterFromList(this);

            if (lenticularMaterial != null)
                DestroyImmediate(lenticularMaterial);
            if (quiltTexture != null)
                quiltTexture.Release();
            if (preview2DRT != null)
                preview2DRT.Release();

            //NOTE: We don't destroy the post-process camera because the PostProcessLayer component requires it stays on
            if (singleViewCamera != null)
                DestroyImmediate(singleViewCamera.gameObject);
            if (finalScreenCamera != null)
                DestroyImmediate(finalScreenCamera.gameObject);

            if (depthQuiltTexture != null)
                depthQuiltTexture.Release();

            singleViewCamera = finalScreenCamera = null;

            if (subpixelCellBuffer != null) {
                subpixelCellBuffer.Dispose();
                subpixelCellBuffer = null;
            }
        }

        private void Update() {
            if (!Initialized)
                return;

            finalScreenCamera.clearFlags = cameraProperties.ClearFlags;
            finalScreenCamera.backgroundColor = finalScreenCamera.clearFlags == CameraClearFlags.Depth ? Color.clear : cameraProperties.BackgroundColor;

            frameRendered = false;
            frameRendered2DPreview = false;

            bool shiftF8 = false;
            bool esc = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = InputSystem.GetDevice<Keyboard>();
            if (keyboard != null) {
                if (keyboard.shiftKey.isPressed && keyboard.f8Key.wasPressedThisFrame)
                    shiftF8 = true;
                if (keyboard.escapeKey.wasPressedThisFrame)
                    esc = true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKeyDown(KeyCode.F8))
                shiftF8 = true;
            if (Input.GetKeyDown(KeyCode.Escape))
                esc = true;
#endif

            if (shiftF8)
                debugInfo = !debugInfo;
            if (esc)
                debugInfo = false;
        }

        private void LateUpdate() {
            if (!frameRendered)
                PrepareFieldsBeforeRendering();
        }

        private void OnGUI() {
            if (debugInfo) {
                Color previousColor = GUI.color;

                // start drawing stuff
                int unitDiv = 20;
                int unit = Mathf.Min(Screen.width, Screen.height) / unitDiv;
                Rect rect = new Rect(unit, unit, unit * (unitDiv - 2), unit * (unitDiv - 2));

                GUI.color = Color.black;
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                rect = new Rect(unit * 2, unit * 2, unit * (unitDiv - 4), unit * (unitDiv - 4));

                GUILayout.BeginArea(rect);
                GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.fontSize = unit;
                GUI.color = new Color(0.5f, 0.8f, 0.5f, 1);

                GUILayout.Label("LookingGlass SDK " + Version.Value, labelStyle);
                GUILayout.Space(unit);
                GUI.color = (!isEmulatingDevice) ? new Color(0.5f, 1, 0.5f) : new Color(1, 0.5f, 0.5f);
                GUILayout.Label("calibration: " + (!isEmulatingDevice ? "loaded" : "not found, but emulating"), labelStyle);

                //TODO: This is giving a false positive currently
                //GUILayout.Space(unit);
                //GUI.color = new Color(0.5f, 0.5f, 0.5f, 1);
                //GUILayout.Label("lkg display: " + (loadResults.lkgDisplayFound ? "found" : "not found"), labelStyle);

                GUILayout.EndArea();

                GUI.color = previousColor;
            }
        }

        private void OnDrawGizmos() {
            if (Initialized)
                gizmos.DrawGizmos(this);
        }
        #endregion

        private void Initialize() {
            try {
                InitSections();
                RegisterToList(this);

                //NOTE: This was accidentally serialized before.
                //To make sure Unity doesn't grab it from serialized data again, we'll explicitly reset it here on initialization:
                isUsingCustomLenticularRegion = false;

                UpdateCalibration();
                _ = InitializeAfterCalibrationAsync();
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        private async Task InitializeAfterCalibrationAsync() {
            bool loadAsync =
#if !UNITY_IOS && !UNITY_ANDROID
                true;
#else
                false;
#endif

            try {
                if (loadAsync) {
                    if (LKGDisplaySystem.IsLoading)
                        await LKGDisplaySystem.WaitForCalibrations();
                    if (this == null || !isActiveAndEnabled)
                        return;
                }

                SetupQuilt();

                if (lenticularMaterial == null)
                    MultiViewRendering.CreateLenticularMaterial(ref lenticularMaterial, Debugging.OverrideLenticularShader);

                SetupAllCameras();
                PrepareFieldsBeforeRendering();
                SetupRenderer();
                Preview2D = preview2D;

                string persistentDataJSONPath = UserStorage.PersistentDataJSONPath;

                if (loadAsync)
                    SetupScreenResolution();
#if UNITY_IOS && !UNITY_EDITOR
                else if (File.Exists(persistentDataJSONPath))
                    LoadManualCalibration();
#endif
                debugging.onShowAllObjectsChanged -= SetAllObjectHideFlags;
                debugging.onShowAllObjectsChanged += SetAllObjectHideFlags;

                initialized = true;
                renderStack.RenderToQuilt(this);

                onAnyInitialized?.Invoke(this);
                initializationTcs.TrySetResult(true);
            } catch (Exception e) {
                Debug.LogError("Failed to fully initialize " + this + "!");
                Debug.LogException(e);
            }
        }

        private void SetupScreenResolution() {
            if (!Application.isEditor) {
                //Unity Bug Report IN-18776
                //  - https://unity3d.atlassian.net/servicedesk/customer/portal/2/IN-18776
                //  - https://issuetracker.unity3d.com/product/unity/issues/guid/UUM-21974
                //Display.SetParams(...) causes crashes on OpenGLCore.
                //According to Unity, "Multi-display is only supported for DirectX graphics APIs".
                GraphicsDeviceType graphicsAPI = SystemInfo.graphicsDeviceType;

                if (graphicsAPI == GraphicsDeviceType.OpenGLCore) {
                    if (!SuppressDisplayWindowingErrors)
                        Debug.LogError("Failed to target your Looking Glass display!\n" +
                            "Multi-display applications is only supported for DirectX graphics APIs in Unity.\n" +
                            "Display.SetParams(...) crashes in OpenGLCore. (see the following for more information: https://issuetracker.unity3d.com/product/unity/issues/guid/UUM-21974).\n");
                } else {
                    //NOTE: This is REQUIRED for using Display.SetParams(...)!
                    //See Unity docs on this at: https://docs.unity3d.com/ScriptReference/Display.SetParams.html

                    //NOTE: WITHOUT this line, subsequent calls to Display.displays[0].SetParams(...) HAVE NO EFFECT!
                    UnityEngine.Display.displays[0].Activate();
#if UNITY_STANDALONE_WIN
                    if (ForceDisplayIndex)
                        UnityEngine.Display.displays[0].SetParams(lenticularRegion.Width, lenticularRegion.Height, lenticularRegion.left, lenticularRegion.top);
#elif UNITY_STANDALONE_OSX
                    StartCoroutine(MacWindowing.SetupMacWindowing(lkgDisplay));
#endif
                }
            }

#if !UNITY_STANDALONE_OSX
            //This sets up the window to play on the looking glass,
            //NOTE: This must be executed after display reposition
            //YAY! This FIXED the issue with cal.screenHeight or 0 as the SetParams height making the window only go about half way down the screen!
            //This also lets the lenticular shader render properly!
            Screen.SetResolution(lenticularRegion.Width, lenticularRegion.Height, true);
#endif
        }

        /// <summary>
        /// Loads calibration from a "visual.json" file from the persistent data path.
        /// </summary>
        public Calibration LoadManualCalibration() {
            string persistentDataJSONPath = UserStorage.PersistentDataJSONPath;
            string json = File.ReadAllText(persistentDataJSONPath);

#if HAS_NEWTONSOFT_JSON
            Calibration hardcodedCalibration = LookingGlass.Toolkit.Calibration.Parse(json);
            Debugging.ManualCalibration = hardcodedCalibration;
            Debugging.ManualCalibrationMode = ManualCalibrationMode.UseManualSettings;

            if (AutomaticQuiltPreset) {
                LKGDeviceType deviceType = hardcodedCalibration.GetDeviceType();
                Debug.Log("Detected " + nameof(AutomaticQuiltPreset) + " set to true while loading visual.json (deviceType = " + deviceType + ")");
                UseAutomaticQuiltSettings();
                Debug.Log("Updated quilt preset and quilt settings for iOS based on automatic:\n" + JsonUtility.ToJson(QuiltPreset, true));
            }
#endif
            return Debugging.ManualCalibration;
        }

        private void SetupRenderer() {
            MultiViewRenderer.Next = this;
            renderer = finalScreenCamera.gameObject.AddComponent<MultiViewRenderer>();
        }

        internal void InitSections() {
            foreach (PropertyGroup group in PropertyGroups)
                group.Init(this);
        }

        internal void OnQuiltSettingsChanged() {
            onAnyQuiltSettingsChanged?.Invoke(this);
            onQuiltSettingsChanged?.Invoke();
#if UNITY_EDITOR
            //NOTE: Delaying because Unity was complaining about recursive GUI calls upon stopping recording
            EditorApplication.delayCall += () => GameViewExtensions.RepaintAllViewsImmediately();
#endif
        }

        internal void OnCalibrationChanged() {
            onAnyCalibrationReloaded?.Invoke(this);
            onCalibrationChanged?.Invoke();
#if UNITY_EDITOR
            GameViewExtensions.UpdateUserGameViews();
            EditorApplication.delayCall += () => GameViewExtensions.RepaintAllViewsImmediately();
#endif
        }

        private void ValidateCanChangeQuiltSettings() {
#if UNITY_EDITOR || !UNITY_IOS
            if (AreQuiltSettingsLockedForRecording)
                throw new InvalidOperationException("You cannot set quilt settings during recording! Please use " + nameof(QuiltCapture) + "'s override settings instead.");
#endif
        }

        internal static HologramCamera GetLastForDevice(string serial) {
            HologramCamera last = null;
            foreach (HologramCamera other in All)
                if (last == null || (other != last && other.IsSameDevice(last) && other.cameraProperties.Depth > last.cameraProperties.Depth))
                    last = other;
            return last;
        }

        internal bool IsLastForDevice() {
            foreach (HologramCamera other in All)
                if (other != this && other.IsSameDevice(this) && other.cameraProperties.Depth > cameraProperties.Depth)
                    return false;
            return true;
        }

        public void UseDefaultFilterMode() {
            renderStack.FilterMode = QuiltFilterMode.PointVirtualPixelAA;
        }

        public void UseAutomaticQuiltSettings() => SetQuiltPreset(true, new QuiltPreset(DeviceType));
        public void UseCustomQuiltSettings(QuiltSettings settings) => SetQuiltPreset(false, new QuiltPreset(settings));
        public void SetQuiltPreset(bool automaticQuiltPreset, QuiltPreset quiltPreset) {
            ValidateCanChangeQuiltSettings();

            this.automaticQuiltPreset = automaticQuiltPreset;
            this.quiltPreset = quiltPreset;

            if (this.automaticQuiltPreset)
#if UNITY_IOS
            {
                this.quiltPreset.UseDefaultFrom(Calibration.GetDeviceType());
            }
#else
                this.quiltPreset.UseDefaultFrom(DeviceType);
#endif

            SetupQuilt();
            OnQuiltSettingsChanged();
        }

        //NOTE: Only the finalScreenCamera is set with enabled = true, because it's the only camera here meant to write to the screen.
        //Thus, its targetTexture is null, and it's enabled to call OnRenderImage(...) and write each frame to the screen.
        //These other cameras are just for rendering intermediate results.
        private void SetupAllCameras() {
            singleViewCamera = new GameObject(SingleViewCameraName).AddComponent<Camera>();
            singleViewCamera.transform.SetParent(transform, false);
            singleViewCamera.enabled = false;
#if !HAS_URP
            singleViewCamera.stereoTargetEye = StereoTargetEyeMask.None; //NOTE: This is needed for better XR support
#endif
            SetHideFlagsOnObject(singleViewCamera);

            if (UsePostProcessing?.Invoke(this) ?? false) {
                //NOTE: The post-processing camera (if we use one) should be on the LookingGlass GameObject,
                //so that a PostProcessLayer (or other post-processing components) put on the LookingGlass object itself will have an effect!
                //(Better UX for the user for integrating post-processing with LookingGlass components)
                if (!TryGetComponent(out postProcessCamera))
                    postProcessCamera = gameObject.AddComponent<Camera>();
                postProcessCamera.enabled = false;
                postProcessCamera.stereoTargetEye = StereoTargetEyeMask.None;
                SetHideFlagsOnObject(postProcessCamera);
            } else {
                if (TryGetComponent(out Camera c))
                    SetHideFlagsOnObject(c);
            }


            finalScreenCamera = new GameObject(FinalScreenCameraName).AddComponent<Camera>();
            finalScreenCamera.transform.SetParent(transform, false);

#if UNITY_2017_3_OR_NEWER
            finalScreenCamera.allowDynamicResolution = false;
#endif
            finalScreenCamera.allowHDR = false;
            finalScreenCamera.allowMSAA = false;
            finalScreenCamera.cullingMask = 0;
            finalScreenCamera.clearFlags = CameraClearFlags.Nothing;
            finalScreenCamera.targetDisplay = (int)targetDisplay;
#if !HAS_URP
            finalScreenCamera.stereoTargetEye = StereoTargetEyeMask.None;
#endif
            SetHideFlagsOnObject(finalScreenCamera);
        }

        private void SetAllObjectHideFlags() {
            for (int i = hideFlagsObjects.Count - 1; i >= 0; i--) {
                if (hideFlagsObjects[i] == null) {
                    hideFlagsObjects.RemoveAt(i);
                    continue;
                }
                SetHideFlagsOnObject(hideFlagsObjects[i]);
            }
        }

        /// <summary>
        /// <para>Sets the hide flags on a temporary object used by this <see cref="Camera"/> script.</para>
        /// <para>If the <paramref name="tempComponent"/> is on the same <see cref="GameObject"/> as this script, it sets the component's hide flags.<br />
        /// When <paramref name="tempComponent"/> on a different game object from this <see cref="Camera"/> script, <paramref name="tempComponent"/>'s game object's hide flags are set instead.</para>
        /// </summary>
        internal HideFlags SetHideFlagsOnObject(Component tempComponent, bool skipRegistration = false) {
            HideFlags hideFlags = HideFlags.None;
            if (tempComponent == null)
                return hideFlags;

            bool isOnCurrentGameObject = tempComponent.gameObject == gameObject;
            bool hide = !debugging.ShowAllObjects;

            if (isOnCurrentGameObject && tempComponent is Camera) {
                //We WANT to save a Camera component if it's on the same LookingGlass game object,
                //So that PostProcessLayers don't complain with warning logs about adding a Camera before us...
            } else {
                hideFlags |= HideFlags.DontSave;
            }

            //NOTE: HideInHierarchy on a Component allows us to hide that specific Component's gizmos!
            if (hide)
                hideFlags |= HideFlags.HideInHierarchy | HideFlags.HideInInspector;

            if (isOnCurrentGameObject)
                tempComponent.hideFlags = hideFlags;
            else
                tempComponent.gameObject.hideFlags = hideFlags;

            if (!skipRegistration && !hideFlagsObjects.Contains(tempComponent))
                hideFlagsObjects.Add(tempComponent);

            return hideFlags;
        }

        internal void ResetCameras() {
            if (finalScreenCamera != null)
                finalScreenCamera.depth = cameraProperties.Depth;

            if (singleViewCamera != null) {
                Matrix4x4 test = singleViewCamera.projectionMatrix;
                cameraProperties.SetCamera(singleViewCamera);
                singleViewCamera.clearFlags = clearFlags;
                singleViewCamera.backgroundColor = background;

                //TODO: [CRT-3174] Look into whether or not we should set the singleViewCamera's clear flags to nothing or not..
                //      And we should investigate into how to fix the depthiness slider's Skybox bug..
                //This was our previous logic:
                // switch (cameraProperties.ClearFlags) {
                //     case CameraClearFlags.Depth:
                //     case CameraClearFlags.SolidColor:
                //     case CameraClearFlags.Nothing:
                //         //IMPORTANT: The single-view camera MUST clear after each render, or else there will be
                //         //ghosting of previous single-view renders left in the next quilt views to render (getting more and more worse as more views are rendered)

                //         //HOWEVER, it should be left on Color.clear for the background color, because the quilt is already cleared with
                //         //our background color before any of the single-views are copied into it.
                //         singleViewCamera.clearFlags = CameraClearFlags.SolidColor;
                //         singleViewCamera.backgroundColor = Color.clear;
                //         break;
                // }

                // BF: Regarding note above, what is the skybox depth issue?

                singleViewCamera.ResetWorldToCameraMatrix();
                singleViewCamera.ResetProjectionMatrix();
                Matrix4x4 centerViewMatrix = singleViewCamera.worldToCameraMatrix;
                Matrix4x4 centerProjMatrix = singleViewCamera.projectionMatrix;

                if (cameraProperties.TransformMode == TransformMode.Volume)
                    centerViewMatrix.m23 -= focalPlane;

                float aspect = Calibration.ScreenAspect;
                if (cameraProperties.UseFrustumTarget) {
                    Vector3 targetPos = -cameraProperties.FrustumTarget.localPosition;
                    centerViewMatrix.m03 += targetPos.x;
                    centerProjMatrix.m02 += targetPos.x / (size * aspect);
                    centerViewMatrix.m13 += targetPos.y;
                    centerProjMatrix.m12 += targetPos.y / size;
                } else {
                    if (cameraProperties.HorizontalFrustumOffset != 0) {
                        float offset = focalPlane * Mathf.Tan(Mathf.Deg2Rad * cameraProperties.HorizontalFrustumOffset);
                        centerViewMatrix.m03 += offset;
                        centerProjMatrix.m02 += offset / (size * aspect);
                    }
                    if (cameraProperties.VerticalFrustumOffset != 0) {
                        float offset = focalPlane * Mathf.Tan(Mathf.Deg2Rad * cameraProperties.VerticalFrustumOffset);
                        centerViewMatrix.m13 += offset;
                        centerProjMatrix.m12 += offset / size;
                    }
                }

                singleViewCamera.worldToCameraMatrix = centerViewMatrix;
                singleViewCamera.projectionMatrix = centerProjMatrix;

#if HAS_URP
                if (RenderPipelineUtil.IsURP) {
                    singleViewCamera.GetUniversalAdditionalCameraData().renderPostProcessing = urpPostProcessing;

//#if UNITY_6000_0_OR_NEWER
                    singleViewCamera.GetUniversalAdditionalCameraData().SetRenderer(urpScriptableRenderer);
//#endif
                }

#endif
                }
            }

        public void UpdateLenticularMaterial() => MultiViewRendering.SetLenticularMaterialSettings(this, LenticularMaterial);

        internal void UseCustomLenticularRegion(ScreenRect region) {
            UseCustomLenticularRegionInternal(region);
            foreach (HologramCamera other in All) {
                if (other != this && other.IsSameDevice(this))
                    other.UseCustomLenticularRegionInternal(region);
            }
        }

        internal void ClearCustomLenticularRegion() {
            ClearCustomLenticularRegionInternal();
            foreach (HologramCamera other in All)
                if (other != this && other.IsSameDevice(this))
                    other.ClearCustomLenticularRegionInternal();
        }

        private void UseCustomLenticularRegionInternal(ScreenRect region) {
            isUsingCustomLenticularRegion = true;
            lenticularRegion = region;
        }

        private void ClearCustomLenticularRegionInternal() {
            isUsingCustomLenticularRegion = false;
            lenticularRegion = GetFullScreenRect();
        }

        private ScreenRect GetFullScreenRect() {
            ScreenRect rect = new();
            if (!isEmulatingDevice) {
                rect.left = lkgDisplay.hardwareInfo.windowCoordX;
                rect.top = lkgDisplay.hardwareInfo.windowCoordY;
                rect.right = rect.left + lkgDisplay.calibration.screenW;
                rect.bottom = rect.top + lkgDisplay.calibration.screenH;
                //Debug.Log("rect of lkg display " + rect);
            } else {
                if (emulatedDeviceTemplate != null) {
                    //REVIEW: [CRT-4039] Does this work as (and when) intended?
                    rect.left = 0;
                    rect.top = 0;
                    rect.right = rect.left + emulatedDeviceTemplate.calibration.screenW;
                    rect.bottom = rect.top + emulatedDeviceTemplate.calibration.screenH;
                } else {
                    Debug.LogError("Retrieved empty calibration, since it failed to load in any fashion!");
                }
            }
            return rect;
        }

        private bool ApplyManualCalibrationSettings() {
            Calibration c;
            switch (Debugging.ManualCalibrationMode) {
                case ManualCalibrationMode.UseCalibrationTextAsset:
                    TextAsset file = Debugging.CalibrationTextAsset;
                    if (file == null)
                        return false;
#if HAS_NEWTONSOFT_JSON
                    JObject root = JObject.Parse(file.text);
                    c = Calibration.Parse(root);
                    manualCalibration = c;
#else
                    return false;
#endif
                    break;
                case ManualCalibrationMode.UseManualSettings:
                    c = Debugging.ManualCalibration;
                    break;
                default:
                    return false;
            }

            if (isEmulatingDevice) {
                //NOTE: The manual calibration might technically be a different type of device,
                //  so let's force the emulatedDevice to match our provided manual calibration:
                emulatedDevice = c.GetDeviceType();
                emulatedDeviceTemplate.calibration = c;
            } else {
                lkgDisplay.calibration = c;
            }
            return true;
        }

        /// <summary>
        /// Note: This does NOT call <see cref="LKGDisplaySystem.ReloadCalibrations"/>. This just reads the currently-available calibration data.
        /// </summary>
        /// <returns></returns>
        public void UpdateCalibration() {
            try {
                QuiltSettings previousQuiltSettings = QuiltSettings;
                bool setDirty = false;
                string prevTargetName = targetLKG;
                bool foundCalibration = false;
                lkgDisplay = null; //WARNING: Because this is serialized, Unity WILL initialize it with a new() instance with zeroed out values.

                isEmulatingDevice = true;
                // below is not working on mobile yet
                foreach (ToolkitDisplay display in LKGDisplaySystem.LKGDisplays) {
                    if (targetLKG == display.calibration.serial) {
                        lkgDisplay = display;
                        isEmulatingDevice = false;
                        foundCalibration = true;
                        break;
                    }
                }

                if (!foundCalibration) {
                    if (LKGDisplaySystem.LKGDisplayCount > 0) {
                        lkgDisplay = LKGDisplaySystem.Get(0);
                        isEmulatingDevice = false;
                        setDirty = true;
                        foundCalibration = true;
                    }

                    if (!foundCalibration) {
                        ILKGDeviceTemplateSystem system = LookingGlass.Toolkit.ServiceLocator.Instance.GetSystem<ILKGDeviceTemplateSystem>();
                        if (system != null) {
                            LKGDeviceTemplate template = system.GetTemplate(emulatedDevice);
                            if (template != null) {
                                emulatedDeviceTemplate = template;
                                foundCalibration = true;
                            }
                        }
                        if (!foundCalibration) {
                            Debug.LogError("Something is critically wrong with the LKG UnityPlugin! Please contact Looking Glass support at support@lookingglassfactory.com for assistance.");
                        }
                    }
                }

                ApplyManualCalibrationSettings();
                targetLKG = Calibration.serial;

#if UNITY_EDITOR
                if (setDirty && !Application.IsPlaying(this)) {
                    //NOTE: EditorSceneManager.MarkSceneDirty(...) was mysteriously returning false, maybe cause this is called during Awake()?
                    //      So just wait 1 editor frame:
                    EditorApplication.delayCall += () => {
                        Scene scene = gameObject.scene;
                        if (scene.IsValid()) {
                            EditorSceneManager.MarkSceneDirty(scene);
                        } else {
                            string prefabPath = AssetDatabase.GetAssetPath(this);
                            EditorUtility.SetDirty(gameObject);
                        }
                    };
                }
#endif

                if (!foundCalibration)
                    Debug.LogWarning("Unable to find calibration");

                SetQuiltPreset(automaticQuiltPreset, quiltPreset);

                if (!isUsingCustomLenticularRegion)
                    lenticularRegion = GetFullScreenRect();

                UpdateLenticularMaterial();
                OnCalibrationChanged();
            } catch (Exception e) {
                Debug.LogError("Error occurred during " + (Application.isPlaying ? "Playmode" : "Editmode"));
                Debug.LogException(e);
            }
        }

#if UNITY_EDITOR || !UNITY_IOS
        internal void LockRenderSettingsForRecording(QuiltCapture capture) {
            Assert.IsNotNull(capture);
            Assert.IsNull(currentCapture);
            currentCapture = capture;
        }

        internal void UnlockRenderSettingsFromRecording(QuiltCapture capture) {
            Assert.AreEqual(currentCapture, capture);
            currentCapture = null;
        }
#endif
        private GraphicsFormat GetQuiltFormat() {
            if (allowHDR)
                return GraphicsFormat.R16G16B16A16_SFloat;
            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
                return GraphicsFormat.R8G8B8A8_SRGB;
            return GraphicsFormat.R8G8B8A8_UNorm;
        }

        private bool NeedsQuiltResetup() => NeedsQuiltResetup(QuiltSettings);
        private bool NeedsQuiltResetup(QuiltSettings renderSettings) {
            if (!isActiveAndEnabled)
                return false;

            RenderTexture quilt = quiltTexture;
            if (quilt == null)
                return true;

            if (quilt.width != renderSettings.quiltWidth || quilt.height != renderSettings.quiltHeight)
                return true;

            //WARNING: It'd be dangerous to compare the quilt.format, because RenderTexture.Default and RenderTexture.DefaultHDR evaluate to something that's NOT equal to themselves,
            //so this would constantly say "YES! The quilt needs to be re-setup every frame!" and allocate your system out of RAM/VRAM!
            if (/*quilt.format*/ quiltTextureOriginalFormatUsed != GetQuiltFormat())
                return true;
            return false;
        }

        public RenderTexture SetupQuilt() => SetupQuilt(QuiltSettings);

        /// <summary>
        /// <para>Sets up the quilt and the quilt <see cref="RenderTexture"/>.</para>
        /// <para>This should be called after modifying custom quilt settings.</para>
        /// </summary>
        public RenderTexture SetupQuilt(QuiltSettings quiltSettings) {
            //Assert.IsTrue(quiltSettings.quiltX > 0, nameof(LookingGlass.Toolkit.QuiltSettings.quiltX) + " must be greater than zero! (" + quiltSettings.quiltX + " was given instead!)");
            //Assert.IsTrue(quiltSettings.quiltY > 0, nameof(LookingGlass.Toolkit.QuiltSettings.quiltY) + " must be greater than zero! (" + quiltSettings.quiltY + " was given instead!)");

            RenderTexture quilt = quiltTexture;
            if (quilt != null) {
                if (quilt == RenderTexture.active)
                    RenderTexture.active = null;
                quilt.Release();
            }

            quiltTextureOriginalFormatUsed = GetQuiltFormat();
            int depthBits = (RenderPipelineUtil.IsURP) ? 32 : 0; //TODO: Why does URP in newer versions (like in Unity 6) suddenly require depth bits now? Aren't we NOT rendering directly from the camera to the quilt? (We're rendering directly to single-view textures, then blitting copies into the quilt view-by-view...)
            if (quiltSettings.quiltWidth >= QuiltSettings.MinSize && quiltSettings.quiltHeight >= QuiltSettings.MinSize) {
                quilt = new RenderTexture(quiltSettings.quiltWidth, quiltSettings.quiltHeight, depthBits, quiltTextureOriginalFormatUsed) {
                    filterMode = FilterMode.Point,
                    hideFlags = (useQuiltAsset) ? HideFlags.None : HideFlags.DontSave
                };

                quilt.name = "LookingGlass Quilt";
                quilt.enableRandomWrite = true;
                quilt.Create();
            } else {
                quilt = null;
            }
            quiltTexture = quilt;

#if UNITY_EDITOR
            if (quilt != null && useQuiltAsset)
                SaveOrUseQuiltAsset();
#endif

            UpdateLenticularMaterial();

            //Pass some stuff globally for post-processing
            Shader.SetGlobalVector("hp_quiltViewSize", new Vector4(
                (quiltSettings.quiltWidth >= QuiltSettings.MinSize) ? (float)quiltSettings.TileWidth / quiltSettings.quiltWidth : 0,
                (quiltSettings.quiltHeight >= QuiltSettings.MinSize) ? (float)quiltSettings.TileHeight / quiltSettings.quiltHeight : 0,
                quiltSettings.TileWidth,
                quiltSettings.TileHeight
            ));
            onQuiltChanged?.Invoke();
            return quilt;
        }

#if UNITY_EDITOR
        public void SaveOrUseQuiltAsset() {
            quiltTexture.hideFlags &= ~HideFlags.DontSave;
            var existingPath = AssetDatabase.GetAssetPath(quiltTexture);
            string quiltPath = "Assets/quilt_tex.renderTexture";
            if (File.Exists(quiltPath)) {
                quiltTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(quiltPath);
            } else if (existingPath == null || existingPath == "") {
                AssetDatabase.CreateAsset(quiltTexture, quiltPath);
                AssetDatabase.Refresh();
                EditorApplication.RepaintProjectWindow();
            } else {
                quiltTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(existingPath);
            }
        }
#endif

        private void ClearTexturesAfterClearFlagsChanged() {
            MultiViewRendering.Clear(QuiltTexture, CameraClearFlags.SolidColor, Color.clear);
            MultiViewRendering.Clear(Preview2DRT, CameraClearFlags.SolidColor, Color.clear);
        }

        private void PrepareFieldsBeforeRendering() {
            ResetCameras();
            if (Initialized)
                UpdateLenticularMaterial();
            cameraProperties.UpdateAutomaticFields();
        }

        public void RenderQuilt(bool forceRender = false, bool ignorePostProcessing = false) {
            RenderQuiltLayer(forceRender, ignorePostProcessing);
            renderStack.RenderToQuilt(this);
        }

        public void RenderQuiltLayer(bool forceRender = false, bool ignorePostProcessing = false) {
            if (!forceRender && frameRendered)
                return;
            frameRendered = true;
            PrepareFieldsBeforeRendering();

            if (clearDirtyFlag) {
                clearDirtyFlag = false;
                ClearTexturesAfterClearFlagsChanged();
            }
            MultiViewRendering.ClearBeforeRendering(quiltTexture, this);

            if (!Initialized || RenderBlack) {
                Graphics.Blit(Util.OpaqueBlackTexture, quiltTexture);
                return;
            }

            MultiViewRendering.RenderQuilt(this, ignorePostProcessing, (int viewIndex) => {
                events.OnViewRendered?.Invoke(this, viewIndex);
            });
        }

        public RenderTexture RenderPreview2D(bool forceRender = false, bool ignorePostProcessing = false) {
            if (!forceRender && frameRendered2DPreview)
                return preview2DRT;
            frameRendered2DPreview = true;
            PrepareFieldsBeforeRendering();

            if (clearDirtyFlag) {
                clearDirtyFlag = false;
                ClearTexturesAfterClearFlagsChanged();
            }

            if (!Initialized || RenderBlack) {
                //TODO: Create a method similar to SetupQuilt(...) but for the Preview2D texture..
                RenderTexture t = Preview2DRT;
                if (t != null) {
                    Graphics.Blit(Util.OpaqueBlackTexture, t);
                    return t;
                }
            }

            RenderTexture next = MultiViewRendering.RenderPreview2D(this, ignorePostProcessing);
            if (next != preview2DRT) {
                if (preview2DRT != null) {
                    if (Application.IsPlaying(gameObject))
                        Destroy(preview2DRT);
                    else
                        DestroyImmediate(preview2DRT);
                }
                preview2DRT = next;
            }
            return preview2DRT;
        }
    }
}
