using UnityEngine;
using LookingGlass.Toolkit;

#if UNITY_EDITOR
    using UnityEditor;
#endif

namespace LookingGlass {
    public partial class HologramCamera {
        #region CameraData
        [Tooltip("Defines how the transform controls the shape of the viewing volume." +
            "\n\n• In " + nameof (TransformMode.Volume) + " mode, the transform position defines the focal plane." +
            "\n• In " + nameof(TransformMode.Camera) + " mode, the transform position defines the camera position.")]
        [SerializeField] internal TransformMode transformMode;

        [Range(0.01f, 5)]
        [Tooltip("Scales the nearClipPlane away from the focal plane." +
            "\n\nThe relationship can be mathematically written as the following:" +
            "\nnearClipPlane = max(0.01, focalPlane - size * nearClipFactor * depthiness)")]
        [SerializeField] internal float nearClipFactor = DefaultNearClipFactor;

        [Range(0.01f, 40)]
        [Tooltip("Scales the farClipPlane away from the focal plane." +
            "\n\nThe relationship can be mathematically written as the following:" +
            "\nfarClipPlane = max(0.01, focalPlane + size * farClipFactor * depthiness)")]
        [SerializeField] internal float farClipFactor = DefaultFarClipFactor;

        [Tooltip("The distance from the camera to the near clipping plane, in world-space units along the camera's forward axis." +
            "\n\nThis field corresponds to Unity's " + nameof(HologramCamera) + "." + nameof(HologramCamera.nearClipPlane) + " property.")]
        [SerializeField] internal float nearClipPlane = 0.01f;

        [Tooltip("The distance from the camera to the far clipping plane, in world-space units along the camera's forward axis." +
            "\n\nThis field corresponds to Unity's " + nameof(HologramCamera) + "." + nameof(HologramCamera.farClipPlane) + " property.")]
        [SerializeField] internal float farClipPlane = 45;

        [Tooltip("The distance from the camera to the focal plane, in world-space units along the camera's forward axis." +
            "\n\nNote that the visual content rendered by " + nameof(HologramCamera) + " is the most in focus and sharpest near the focal plane." +
            "\nThe value must be between the " + nameof(nearClipPlane) + " and " + nameof(farClipPlane) + ".")]
        [SerializeField] internal float focalPlane = 10;

        [Min(0.01f)]
        [Tooltip("Half of the height of the camera frustum at the focal plane." +
            "\nWhen using nearClipFactor and farClipFactor, this value also scales the nearClipPlane and farClipPlane away from the focal plane." +
            "\nNOTE: This is not the same as Unity's " + nameof(UnityEngine.Camera) + "." + nameof(UnityEngine.Camera.orthographicSize) + " property.")]
        [SerializeField] internal float size = 5;
        [SerializeField] internal SizeMode sizeMode;

        [UnityImitatingClearFlags]
        [Tooltip("Determines how the camera clears the render target's background each frame.")]
        [SerializeField] internal CameraClearFlags clearFlags = CameraClearFlags.Color;
        [Tooltip("The background color of the render target when using Solid Color clear flags." +
            "\nNOTE: Unlike Unity's camera background color, the alpha channel is used to blend " +
            "with any render steps in the stack that come before the Current LookingGlass step.")]
        [SerializeField] internal Color background = Color.black;
        [SerializeField] internal LayerMask cullingMask = -1;

        [Range(5, 120)]
        [Tooltip("The vertical field of view of the camera in degrees.")]
        [SerializeField] internal float fov = 14;
        [Tooltip("Sets the " + nameof(finalScreenCamera) + "'s depth, " +
            "which determines the rendering order (relative to all other cameras in the scene) " +
            "of the final " + nameof(HologramCamera) + " rendering result.")]
        [SerializeField] internal float depth = 0;

        [Tooltip("The rendering path to use for rendering each of the single-views." +
            "\n\nYou may choose to use the player settings, or explicitly use deferred or forward rendering.")]
        [SerializeField] internal RenderingPath renderingPath = RenderingPath.UsePlayerSettings;

        [SerializeField] internal bool useOcclusionCulling = true;
        [SerializeField] internal bool allowHDR = true;
        [SerializeField] internal bool allowMSAA = true;
        [SerializeField] internal bool allowDynamicResolution = false;

        [Tooltip("Determines whether or not the frustum target will be used.")]
        [SerializeField] internal bool useFrustumTarget;
        [SerializeField] internal Transform frustumTarget;

        [Tooltip("Offsets the cycle of horizontal views based on the observer's viewing angle, represented as a percentage on a scale of [-0.5, 0.5].\n\n" +
            "Only applies in the Unity editor as it is typically used to correct for center distortion from the taskbar being visible, ignored in builds.\n\n" +
            "The default value is 0.")]
        [Range(-0.5f, 0.5f)]
        [SerializeField] internal float centerOffset;
        [Range(-90, 90)]
        [SerializeField] internal float horizontalFrustumOffset;
        [Range(-90, 90)]
        [SerializeField] internal float verticalFrustumOffset;

        [Tooltip("Scales the z-axis between the view and projection matrices.\n\n" +
            "This is useful when the scene has too much depth, creating an apparent blur due to large discrepancies in the interlaced images.\n" +
            "Scaling Z down maintains parallax and relational depth, but adds visual clarity to near and far objects at the cost of geometric accuracy.\n\n" +
            "Default value is 1.")]
        [Range(0.01f, 3)]
        [SerializeField] internal float depthiness = 1;
        #endregion

        #region Gizmos
        [SerializeField] internal bool drawHandles = true;
        [SerializeField] internal Color frustumColor = new Color32(0, 255, 0, 255);
        [SerializeField] internal Color middlePlaneColor = new Color32(150, 50, 255, 255);
        [SerializeField] internal Color handleColor = new Color32(75, 100, 255, 255);
        #endregion

        #region Events
        [Tooltip("A callback that gets called before each individual view is rendered.\n\n" +
            "This is called with viewIndex values ranging within [0, numViews].\n" +
            "Note that the upper bound is inclusive, for an extra callback in case cleanup is needed.")]
        [SerializeField] internal ViewRenderEvent onViewRendered;
        #endregion

        #region Optimization
        [SerializeField] internal HologramViewInterpolation viewInterpolation = HologramViewInterpolation.None;
        [SerializeField] internal bool reduceFlicker;
        [SerializeField] internal bool fillGaps;
        [SerializeField] internal bool blendViews;
        #endregion

        #region Debugging
        [Tooltip("When set to true, this reveals hidden objects used by this " +
            nameof(HologramCamera) + " component, such as the cameras used for rendering.")]
        [SerializeField] internal bool showAllObjects = false;

        [Min(-1)]
        [Tooltip("Forces the rendering of only one view in the quilt,\n" +
            "where this value represents the single-view's view index.")]
        [SerializeField] internal int onlyShowView = -1;

        [Tooltip("When " + nameof(onlyShowView) + " is set to a valid index, this does the following:\n\n" +
            "    - When set to true, this causes the single-view to only render to its tile in the quilt; the rest of the quilt will remain empty.\n" +
            "    - When set to false, the single-view will be copied into every tile in the quilt.\n\n" +
            "This does nothing when " + nameof(onlyShowView) + " is -1.")]
        [SerializeField] internal bool onlyRenderOneView = false;

        [Tooltip("Normally when in Unity BiRP, our rendering logic will set the single-view camera's render target via Camera.SetTargetBuffers(RenderBuffer color, RenderBuffer depth).\n\n" +
            "It doesn't work in SRPs like URP and HDRP, but even in BiRP, this may not work with all effects.\n\n" +
            "Set this to true to force setting the camera's render target via Camera.targetTexture instead. This does not render out the depth quilt, however. This area needs further investigation.")]
        [SerializeField] internal bool fallbackCameraTargetTexture = false;

        [Space(20)]
        [SerializeField] internal ManualCalibrationMode manualCalibrationMode;
        [SerializeField] internal TextAsset calibrationTextAsset;

#if UNITY_EDITOR
        [ContextMenuItem("Reset", nameof(ResetManualCalibration))]
        [ContextMenuItem("Reset To Current", nameof(ResetManualCalibrationToCurrent))]
#endif
        [SerializeField] internal Calibration manualCalibration = default;

        [SerializeField] internal Shader overrideLenticularShader = null;
        #endregion

#if UNITY_EDITOR
        /// <summary>
        /// Resets the manual calibration to zeroes, and sets the <see cref="HologramCamera"/> to NOT use manual calibration at all.
        /// </summary>
        internal void ResetManualCalibration() {
            manualCalibration = default;
            manualCalibrationMode = ManualCalibrationMode.None;
            UpdateCalibration();
        }

        /// <summary>
        /// Resets the manual calibration back to the calibration of the connected display, or emulated device template.
        /// </summary>
        internal void ResetManualCalibrationToCurrent() {
            ManualCalibrationMode previous = manualCalibrationMode;
            try {
                manualCalibrationMode = ManualCalibrationMode.None;
                UpdateCalibration();
                manualCalibration = Calibration;
            } finally {
                manualCalibrationMode = previous;
            }
        }
#endif
    }
}
