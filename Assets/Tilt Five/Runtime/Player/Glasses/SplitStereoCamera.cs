/*
 * Copyright (C) 2020-2023 Tilt Five, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TiltFive.Logging;

#if TILT_FIVE_SRP
using UnityEngine.Rendering;
#endif

using AREyes = TiltFive.Glasses.AREyes;

namespace TiltFive
{

    /// <summary>
    /// Display settings constants.
    /// </summary>
    [System.Serializable]
    public class DisplaySettings
    {
        private static DisplaySettings instance;
        private static DisplaySettings Instance
        {
            get
            {
                if(instance == null)
                {
                    instance = new DisplaySettings();
                }
                return instance;
            }
            set => instance = value;
        }

        private DisplaySettings()
        {
            if(!Display.GetDisplayDimensions(ref defaultDimensions))
            {
                Log.Warn("Could not retrieve display settings from the plugin.");
            }
        }

        /// <summary> The display width for a single eye. </summary>
        public static int monoWidth => (stereoWidth / 2);
        /// <summary> The display width for two eyes. </summary>
        public static int stereoWidth => Instance.defaultDimensions.x;
        /// <summary> The display height. </summary>
        public static int height => Instance.defaultDimensions.y;
        /// <summary> The display aspect ratio. </summary>
        public static float monoWidthToHeightRatio => (float) monoWidth / height;
        /// <summary> The double-width display aspect ratio. </summary>
        public static float stereoWidthToHeightRatio => (float) stereoWidth / height;
        /// <summary> The depth buffer's precision. </summary>
        public const int depthBuffer = 24;

        // Provide a texture format compatible with the glasses.
        public const RenderTextureFormat nativeTextureFormat = RenderTextureFormat.ARGB32;

        // Provide a hardcoded default resolution if the plugin is somehow unavailable.
        private readonly Vector2Int defaultDimensions = new Vector2Int(2432, 768);
    }

    [RequireComponent(typeof(Camera))]
    public partial class SplitStereoCamera : MonoBehaviour
    {
        [HideInInspector]
        internal GlassesHandle glassesHandle;

        internal SpectatorSettings spectatorSettings = null;
        private Camera spectatorCamera => spectatorSettings?.spectatorCamera;

        internal GlassesSettings glassesSettings = null;
        /// <summary> The Camera used as a template when creating the eye cameras.</summary>
        public Camera cameraTemplate => glassesSettings?.cameraTemplate;
        /// <summary> The head pose GameObject property. </summary>
        public GameObject headPose = null;
        private bool useSpectatorCamera;
        internal bool UseSpectatorCamera
        {
            get => useSpectatorCamera;
            set
            {
                if (useSpectatorCamera != value)
                {
                    // If we set a new value here, force the letterboxing/pillarboxing to be redrawn,
                    // since this is an implicit mirror mode transition.
                    clearsForMirroring = 0;
                }
                useSpectatorCamera = value;
            }
        }

        // When the onscreen preview format changes, we need to clear the screen backbuffer in case
        // the aspect ratio means the whole screen won't be used, and the number of times we need
        // to do this is dependent on the graphics API and its configuration.
        private uint requiredClearsForMirroring = 1;
        private uint clearsForMirroring = 0;

        private IEnumerator presentStereoImagesCoroutine;

        /// <summary> The name assigned to the dynamically created camera used for rendering the left eye. </summary>
        private const string LEFT_EYE_CAMERA_NAME = "Left Eye Camera";
        /// <summary> The left eye camera GameObject. </summary>
        private GameObject leftEye;
        /// <summary> The left eye Camera property. </summary>
        public Camera leftEyeCamera { get { return eyeCameras[AREyes.EYE_LEFT]; } }

        /// <summary> The name assigned to the dynamically created camera used for rendering the right eye. </summary>
        private const string RIGHT_EYE_CAMERA_NAME = "Right Eye Camera";
        /// <summary> The right eye camera GameObject. </summary>
        private GameObject rightEye;
        /// <summary> The right eye Camera property. </summary>
        public Camera rightEyeCamera { get { return eyeCameras[AREyes.EYE_RIGHT]; } }

        /// <summary> In-editor toggle for displaying the eye cameras in the runtime Hierarchy. </summary>
        public bool showCameras = true;
        /// <summary> The Camera objects. </summary>
        private Dictionary<AREyes, Camera> eyeCameras = new Dictionary<AREyes, Camera>()
        {
            { AREyes.EYE_LEFT, null },
            { AREyes.EYE_RIGHT, null }
        };

        /// <summary>
        /// The position of the game board reference frame w.r.t. the Unity
        /// world-space reference frame.
        /// </summary>
        public Vector3 posUGBD_UWRLD = Vector3.zero;

        /// <summary>
        /// The rotation taking points from the Unity world-space reference
        /// frame to the game board reference frame.
        /// </summary>
        public Quaternion rotToUGBD_UWRLD = Quaternion.identity;

        /// <summary>
        /// The uniform scale factor that takes points from the Unity
        /// world-space to the game board reference frame.
        /// </summary>
        public float scaleToUGBD_UWRLD = 1.0f;

        /// <summary> The name of the custom shader that blits the rendertextures to the backbuffer. </summary>
        private const string SHADER_DISPLAY_BLIT = "Tilt Five/Simple Blend Shader";
        /// <summary> The Material used to store/reference the shader. </summary>
        private Material displayBlitShader;

        private GlassesMirrorMode glassesMirrorMode => spectatorSettings.glassesMirrorMode;
        private GlassesMirrorMode previousMirrorMode = GlassesMirrorMode.None;
        private SplitStereoTextures splitStereoTextures = new SplitStereoTextures();

#if TILT_FIVE_SRP
        private CommandBuffer commandBuffer;
#endif

        /// <summary> The Cameras' field of view property. </summary>
        [System.Obsolete("fieldOfView is deprecated, please use GlassesSettings' fieldOfView instead.")]
        public float fieldOfView
        {
            get { return spectatorCamera.fieldOfView; }
            set { rightEyeCamera.fieldOfView = leftEyeCamera.fieldOfView = spectatorCamera.fieldOfView = value; }
        }

        /// <summary> The Cameras' near clip plane property. </summary>
        [System.Obsolete("nearClipPlane is deprecated, please use GlassesSettings' nearClipPlane instead.")]
        public float nearClipPlane
        {
            get { return spectatorCamera.nearClipPlane; }
            set { rightEyeCamera.nearClipPlane = leftEyeCamera.nearClipPlane = spectatorCamera.nearClipPlane = value; }
        }

        /// <summary> The Cameras' far clip plane property. </summary>
        [System.Obsolete("farClipPlane is deprecated, please use GlassesSettings' farClipPlane instead.")]
        public float farClipPlane
        {
            get { return spectatorCamera.farClipPlane; }
            set { rightEyeCamera.farClipPlane = leftEyeCamera.farClipPlane = spectatorCamera.farClipPlane = value; }
        }

        /// <summary> The Cameras' aspect ratio property. </summary>
        [System.Obsolete("aspectRatio is deprecated, please use DisplaySettings.monoWidthToHeightRatio instead.")]
        public float aspectRatio
        {
            get { return spectatorCamera.aspect; }
            set { spectatorCamera.aspect = value; }
        }

        /// <summary>
        /// Awake this instance.
        /// </summary>
        private void Awake()
        {
            enabled = false;
            useSpectatorCamera = false;
            clearsForMirroring = 0;

            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Vulkan) {
                // Unlike other graphics APIs, Vulkan does not have a "default framebuffer" concept.
                // Unfortunately, Unity only sort of hides that it is doing this, so when calling
                // GL.Clear, it only affects the current buffer, making it necessary to do multiple
                // calls in a row to ensure that all of the swapchains have received a clear. It is
                // typical to do triple buffering with Vulkan to provide the best framerate,
                // especially on more resource-constrained mobile devices. For this reason, Unity
                // defaults to setting the number of swapchain buffers to 3. This can be changed to
                // 2 under Project Settings -> Player -> Vulkan Settings -> Number of swapchain
                // buffers, but this is not recommended because it will generally be worse for
                // performance.
                //
                // Having 3 here will still work even if you reduce the number of buffers, making
                // this a pretty safe default. If you increase the number of swapchain buffers, this
                // value would need to be increased, but you really don't want do do that anyway.
                requiredClearsForMirroring = 3;
            }
        }

        public void Initialize(GameObject headPoseRoot, GlassesSettings glassesSettings, SpectatorSettings spectatorSettings)
        {
            if(headPoseRoot == null || spectatorSettings == null)
            {
                Log.Error("Arguments cannot be null");
                return;
            }

            headPose = headPoseRoot;
            this.glassesSettings = glassesSettings;
            this.spectatorSettings = spectatorSettings;

#if TILT_FIVE_SRP
            commandBuffer = new CommandBuffer() { name = "Onscreen Preview" };
#endif

            // For this mode, we need the headPose Camera to be enabled, as it is the
            // primary Camera for blitting to the backbuffer.
            if(spectatorCamera != null)
            {
                spectatorCamera.enabled = true;
            }

            if(cameraTemplate != null)
            {
                InstantiateEyeCameras(out leftEye, out rightEye);
            }
            else
            {
                GenerateEyeCameras(out leftEye, out rightEye);
            }
            ConfigureEyeCameras();

            // Load the blitting shader to copy the the left & right render textures
            // into the backbuffer
            displayBlitShader = new Material(Shader.Find(SHADER_DISPLAY_BLIT));
            // Did we find it?
            if (null == displayBlitShader)
            {
                Log.Error("Failed to load Shader '{0}'", SHADER_DISPLAY_BLIT);
            }

            SyncFields();
            SyncTransform();
            ShowHideCameras();

            enabled = true;
        }

        private void InstantiateEyeCameras(out GameObject leftEye, out GameObject rightEye)
        {
            var cloneCameraTemplateChildren = glassesSettings.cloneCameraTemplateChildren;

            // When we clone the head pose camera using Instantiate, we may not want to clone its children.
            // If this is the case, detach the children and reparent them under a placeholder/babysitter GameObject
            GameObject placeholder = cloneCameraTemplateChildren ? null : new GameObject("Placeholder");
            if (!cloneCameraTemplateChildren)
            {
                placeholder.transform.parent = headPose.transform.parent;
                while (cameraTemplate.transform.childCount > 0)
                {
                    cameraTemplate.transform.GetChild(0).parent = placeholder.transform;
                }
            }

            // Instantiate left and right eye cameras from the camera template.
            leftEye = Instantiate(cameraTemplate.gameObject, headPose.transform.position, headPose.transform.rotation, headPose.transform);
            leftEye.name = LEFT_EYE_CAMERA_NAME;
            eyeCameras[AREyes.EYE_LEFT] = leftEye.GetComponent<Camera>();

            rightEye = Instantiate(cameraTemplate.gameObject, headPose.transform.position, headPose.transform.rotation, headPose.transform);
            rightEye.name = RIGHT_EYE_CAMERA_NAME;
            eyeCameras[AREyes.EYE_RIGHT] = rightEye.GetComponent<Camera>();

            var splitStereoCamerasLeft = leftEye.GetComponents<SplitStereoCamera>();
            for (int i = 0; i < splitStereoCamerasLeft.Length; i++)
            {
                Destroy(splitStereoCamerasLeft[i]);
            }

            var splitStereoCamerasRight = rightEye.GetComponents<SplitStereoCamera>();
            for (int i = 0; i < splitStereoCamerasRight.Length; i++)
            {
                Destroy(splitStereoCamerasRight[i]);
            }


            if (!cloneCameraTemplateChildren)
            {
                // Reclaim the head pose camera's children from the placeholder/babysitter GameObject
                while (placeholder.transform.childCount > 0)
                {
                    placeholder.transform.GetChild(0).parent = cameraTemplate.transform;
                }
                Destroy(placeholder);
            }
        }

        private void GenerateEyeCameras(out GameObject leftEye, out GameObject rightEye)
        {
            leftEye = new GameObject(LEFT_EYE_CAMERA_NAME, typeof(Camera));
            rightEye = new GameObject(RIGHT_EYE_CAMERA_NAME, typeof(Camera));

            eyeCameras[AREyes.EYE_LEFT] = leftEye.GetComponent<Camera>();
            eyeCameras[AREyes.EYE_RIGHT] = rightEye.GetComponent<Camera>();

            leftEye.transform.parent = headPose.transform;
            rightEye.transform.parent = headPose.transform;

            leftEye.transform.SetPositionAndRotation(headPose.transform.position, headPose.transform.rotation);
            rightEye.transform.SetPositionAndRotation(headPose.transform.position, headPose.transform.rotation);
        }

        private void ConfigureEyeCameras()
        {
            // Use the head pose camera's preferred texture format, rather than forcing it to render in LDR
            splitStereoTextures.Initialize();

            // Configure the left eye camera's render target
            RenderTexture leftTex = splitStereoTextures.LeftTexture_GLS;
            if (leftEyeCamera.allowMSAA && QualitySettings.antiAliasing > 1)
            {
                leftTex.antiAliasing = QualitySettings.antiAliasing;

                // Ensure that the preview textures' antiAliasing settings match.
                // Otherwise, Unity 2020.3 complains during Graphics.CopyTexture about the mismatch,
                // resulting in a broken onscreen preview (manifesting as a black screen).
                splitStereoTextures.MonoPreviewTex.antiAliasing = QualitySettings.antiAliasing;
                splitStereoTextures.StereoPreviewTex.antiAliasing = QualitySettings.antiAliasing;
            }

            leftEyeCamera.targetTexture = leftTex;
            leftEyeCamera.depth = spectatorCamera.depth - 1;

            // Configure the right eye camera's render target
            RenderTexture rightTex = splitStereoTextures.RightTexture_GLS;
            if (rightEyeCamera.allowMSAA && QualitySettings.antiAliasing > 1)
            {
                rightTex.antiAliasing = QualitySettings.antiAliasing;
            }

            rightEyeCamera.targetTexture = rightTex;
            rightEyeCamera.depth = spectatorCamera.depth - 1;
        }

        /// <summary>
        /// EDITOR-ONLY: Syncs the eye Cameras' transform to the Head Pose
        /// when tracking is not available.
        /// </summary>
        void SyncTransform()
        {

#if UNITY_EDITOR
            // We move the eye Cameras in the Editor to emulate head pose and eye movement.
            // In builds, we only set the camera transforms with Glasses tracking data.

            if (null == cameraTemplate || !Player.TryGetPlayerIndex(glassesHandle, out PlayerIndex playerIndex))
                return;

            if (!Glasses.IsTracked(playerIndex))
            {
                GameObject pose = headPose;
                // left eye copy and adjust
                leftEye.transform.position = pose.transform.position;
                leftEye.transform.localPosition = pose.transform.localPosition;
                leftEye.transform.rotation = pose.transform.rotation;
                leftEye.transform.localRotation = pose.transform.localRotation;
                leftEye.transform.localScale = pose.transform.localScale;
                leftEye.transform.Translate(-leftEye.transform.right.normalized * (cameraTemplate.stereoSeparation * 0.5f));

                //right eye copy and adjust
                rightEye.transform.position = pose.transform.position;
                rightEye.transform.localPosition = pose.transform.localPosition;
                rightEye.transform.rotation = pose.transform.rotation;
                rightEye.transform.localRotation = pose.transform.localRotation;
                rightEye.transform.localScale = headPose.transform.localScale;
                rightEye.transform.Translate(rightEye.transform.right.normalized * (cameraTemplate.stereoSeparation * 0.5f));
            }
#endif
        }

        void OnEnable()
        {
            leftEye.SetActive(true);
            rightEye.SetActive(true);
            leftEyeCamera.enabled = true;
            rightEyeCamera.enabled = true;
            presentStereoImagesCoroutine = PresentStereoImagesCoroutine();
            StartCoroutine(presentStereoImagesCoroutine);

#if TILT_FIVE_SRP
            if(Application.isPlaying)
            {
                RenderPipelineManager.beginFrameRendering += OnBeginFrameRendering;
                RenderPipelineManager.endFrameRendering += OnEndFrameRendering;
            }
#endif
        }

        private void OnDisable()
        {
            if (clearsForMirroring > 0)
            {
                clearsForMirroring = 0;
                // If OnDisable() is being called from Destroy(), then the children may no longer
                // exist. Check before acting on them.
                if (leftEye)
                {
                    leftEye.SetActive(false);
                    leftEyeCamera.enabled = false;
                }
                if (rightEye)
                {
                    rightEye.SetActive(false);
                    rightEyeCamera.enabled = false;
                }
            }

            if(presentStereoImagesCoroutine != null)
            {
                StopCoroutine(presentStereoImagesCoroutine);
            }
#if TILT_FIVE_SRP
            RenderPipelineManager.beginFrameRendering -= OnBeginFrameRendering;
            RenderPipelineManager.endFrameRendering -= OnEndFrameRendering;
#endif
        }

#if TILT_FIVE_SRP
        /// <summary>
        /// Configure rendering parameters for the upcoming frame.
        /// </summary>
        /// <remarks>This function primarily handles invalidated render textures due to fullscreen, alt+tabbing, etc.</remarks>
        private void OnBeginFrameRendering(ScriptableRenderContext context, Camera[] cameras)
        {
            // TODO: Determine whether this is necessary, or even permitted; the docs on RenderTexture.IsCreated() are lacking.
            // We want to check for invalidated render textures before rendering,
            // and this event should occur at the beginning of RenderPipeline.Render
            // before any actual render passes occur, so in principle this should work as a substitute for OnPreRender.

            // Check whether the left/right render textures' states have been invalidated,
            // and reset the cached texture handles if so. See the longer explanation below in Update()
            splitStereoTextures.ValidateNativeTexturePointers();
        }
#endif

        /// <summary>
        /// Configure rendering parameters for the upcoming frame.
        /// </summary>
        void OnPreRender()
        {
            splitStereoTextures.ValidateNativeTexturePointers();

            if(!UseSpectatorCamera)
            {
                return;
            }

            spectatorCamera.targetTexture = null;

            // If the screen mirror mode changes, junk data will be displayed
            // in the black bars unless we clear the screen buffer.
            //
            // This has to be done before we adjust the headpose camera rect,
            // since GL.Clear's effect is limited by the active viewport.
            if (glassesMirrorMode != previousMirrorMode)
            {
                clearsForMirroring = 0;
                previousMirrorMode = glassesMirrorMode;
            }

            if (clearsForMirroring < requiredClearsForMirroring)
            {
                // Before calling GL.Clear(), we need to reset the viewport.
                // Otherwise, we may not clear the entire screen in some cases.
                GL.Viewport(spectatorCamera.pixelRect);
                GL.Clear(true, true, Color.black);
                clearsForMirroring += 1;
            }

            if(glassesMirrorMode == GlassesMirrorMode.None)
            {
                return;
            }

            spectatorCamera.cullingMask = 0;    // Cull all layers, render nothing.
            spectatorCamera.fieldOfView = glassesSettings.fieldOfView;
            spectatorCamera.nearClipPlane = glassesSettings.nearClipPlane / scaleToUGBD_UWRLD;
            spectatorCamera.farClipPlane = glassesSettings.farClipPlane / scaleToUGBD_UWRLD;

            // Lock the aspect ratio and add pillarboxing/letterboxing as needed.
            float screenRatio = Screen.width / (float)Screen.height;
            float targetRatio = glassesMirrorMode == GlassesMirrorMode.Stereoscopic
                ? DisplaySettings.stereoWidthToHeightRatio
                : DisplaySettings.monoWidthToHeightRatio;

            if(screenRatio > targetRatio) {
                // Screen or window is wider than the target: pillarbox.
                float normalizedWidth = targetRatio / screenRatio;
                float barThickness = (1f - normalizedWidth) / 2f;
                spectatorCamera.rect = new Rect(barThickness, 0, normalizedWidth, 1);
            }
            else {
                // Screen or window is narrower than the target: letterbox.
                float normalizedHeight = screenRatio / targetRatio;
                float barThickness = (1f - normalizedHeight) / 2f;
                spectatorCamera.rect = new Rect(0, barThickness, 1, normalizedHeight);
            }
        }


#if TILT_FIVE_SRP
        /// <summary>
        /// Apply post processing effects to the frame after it's finished rendering.
        /// </summary>
        /// <remarks>This function primarily updates the onscreen preview to reflect the glasses mirror mode.</remarks>
        /// <param name="context"></param>
        /// <param name="cameras"></param>
        void OnEndFrameRendering(ScriptableRenderContext context, Camera[] cameras)
        {
            if (this == null || !enabled || glassesMirrorMode == GlassesMirrorMode.None || !UseSpectatorCamera)
            {
                spectatorCamera.rect = spectatorSettings.rect;
                return;
            }

            // OnEndFrameRendering isn't picky about the camera(s) that finished rendering.
            // This includes the scene view and/or material preview cameras.
            // We need to make sure we only run the code in this function when we're performing stereoscopic rendering.
            bool currentlyRenderingEyeCameras = false;

            for (int i = 0; i < cameras.Length; i++)
            {
#if UNITY_EDITOR
                if (cameras[i].Equals(UnityEditor.SceneView.lastActiveSceneView.camera))
                {
                    return;
                }
#endif
                if (cameras[i].Equals(leftEyeCamera) || cameras[i].Equals(rightEyeCamera) || cameras[i].Equals(spectatorCamera))
                {
                    currentlyRenderingEyeCameras = true;
                }
            }
            if (!currentlyRenderingEyeCameras)
            {
                return;
            }

            // Determine the aspect ratio to enable pillarboxing/letterboxing.
            float screenRatio = Screen.width / (float)Screen.height;
            float targetRatio = glassesMirrorMode == GlassesMirrorMode.Stereoscopic
                ? DisplaySettings.stereoWidthToHeightRatio
                : DisplaySettings.monoWidthToHeightRatio;
            Vector2 frameScale = Vector2.one;

            if (screenRatio != targetRatio)
            {
                frameScale = screenRatio > targetRatio
                    ? new Vector2(screenRatio / targetRatio, 1f)
                    : new Vector2(1f, targetRatio / screenRatio);
            }

            splitStereoTextures.SubmitPreviewTexturesSRP(glassesMirrorMode, spectatorCamera, commandBuffer, frameScale);

            context.ExecuteCommandBuffer(commandBuffer);
            context.Submit();
            commandBuffer.Clear();

            spectatorSettings.ResetSpectatorCamera();
        }
#endif

        /// <summary>
        /// Apply post-processing effects to the final image before it is
        /// presented.
        /// </summary>
        /// <param name="src">The source render texture.</param>
        /// <param name="dst">The destination render texture.</param>
        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            // If we're not supposed to spectate due to the spectated player being set to None
            // or to a player that isn't connected, have the first SplitStereoCamera attached to
            // the SpectatorCamera perform a blit.
            // TODO: Consider adding a spectator setting to define fallback behavior when the specified player isn't connected.
            // The current behavior is indistinguishable from the PlayerIndex.None case, and perhaps it'd be desirable
            // to spectate the next available player instead, if there is one?
            if (spectatorSettings.spectatedPlayer == PlayerIndex.None || !Player.IsConnected(spectatorSettings.spectatedPlayer))
            {
                var splitStereoCameras = spectatorCamera.GetComponents<SplitStereoCamera>();
                if(splitStereoCameras != null && splitStereoCameras.Length > 0  // These two checks should be redundant
                    && splitStereoCameras[0].Equals(this))  // This is the important one
                {
                    Graphics.Blit(src, null as RenderTexture);
                    return;
                }
                // Any SplitStereoCameras that call OnRenderImage after the first one should return, otherwise they'll clear the screen
                return;
            }

            if(!UseSpectatorCamera)
            {
                return;
            }

            if (glassesMirrorMode != GlassesMirrorMode.None)
            {
                splitStereoTextures.SubmitPreviewTextures(glassesMirrorMode);

                var previewTex = glassesMirrorMode == GlassesMirrorMode.Stereoscopic
                    ? splitStereoTextures.StereoPreviewTex
                    : splitStereoTextures.MonoPreviewTex;

                // Blitting is required when overriding OnRenderImage().
                // Setting the blit destination to null is the same as blitting to the screen backbuffer.
                // This will effectively render previewTex to the screen.
                Graphics.Blit(previewTex,
                    null as RenderTexture,
                    Vector2.one,
                    Vector2.zero);
            }
            else Graphics.Blit(src, null as RenderTexture);

            // We're done with our letterboxing/pillarboxing now that we've blitted to the screen.
            // If the SplitStereoCamera gets disabled next frame, ensure that the original behavior returns.
            spectatorSettings.ResetSpectatorCamera();
        }

        IEnumerator PresentStereoImagesCoroutine()
        {
            // WaitForEndOfFrame() will let us wait until the last possible moment to send frames to the glasses.
            // This allows the results of rendering, postprocessing, and even GUI to be displayed.
            var cachedWaitForEndOfFrame = new WaitForEndOfFrame();

            while (enabled)
            {
                yield return cachedWaitForEndOfFrame;

                PresentStereoImages();
            }
        }

        private void PresentStereoImages()
        {
            splitStereoTextures.GetNativeTexturePointers(out var leftTexHandle, out var rightTexHandle);

            var leftTargetTex = splitStereoTextures.LeftTexture_GLS;
            var rightTargetTex = splitStereoTextures.RightTexture_GLS;

            bool isSrgb = leftTargetTex.sRGB;

            Vector3 posOfULVC_UWRLD = leftEyeCamera.transform.position;
            Quaternion rotToUWRLD_ULVC = leftEyeCamera.transform.rotation;
            Vector3 posOfURVC_UWRLD = rightEyeCamera.transform.position;
            Quaternion rotToUWRLD_URVC = rightEyeCamera.transform.rotation;

            Vector3 posOfULVC_UGBD = rotToUGBD_UWRLD * (scaleToUGBD_UWRLD * (posOfULVC_UWRLD - posUGBD_UWRLD));
            Quaternion rotToUGBD_ULVC = rotToUGBD_UWRLD * rotToUWRLD_ULVC;

            Vector3 posOfURVC_UGBD = rotToUGBD_UWRLD * (scaleToUGBD_UWRLD * (posOfURVC_UWRLD - posUGBD_UWRLD));
            Quaternion rotToUGBD_URVC = rotToUGBD_UWRLD * rotToUWRLD_URVC;


            Display.PresentStereoImages(glassesHandle,
                                       leftTexHandle, rightTexHandle,
                                       leftTargetTex.width, rightTargetTex.height,
                                       isSrgb,
                                       glassesSettings.fieldOfView,
                                       DisplaySettings.monoWidthToHeightRatio,
                                       rotToUGBD_ULVC,
                                       posOfULVC_UGBD,
                                       rotToUGBD_URVC,
                                       posOfURVC_UGBD);
        }

        /// <summary>
        /// Syncs the Cameras' fields to the settings.
        /// </summary>
        private void SyncFields()
        {
            if (glassesSettings == null)
            {
                return;
            }
            if (leftEyeCamera != null)
            {
                leftEyeCamera.fieldOfView = glassesSettings.fieldOfView;
                leftEyeCamera.nearClipPlane = glassesSettings.nearClipPlane;
                leftEyeCamera.farClipPlane = glassesSettings.farClipPlane;
                leftEyeCamera.aspect = DisplaySettings.monoWidthToHeightRatio;
            }
            if (rightEyeCamera != null)
            {
                rightEyeCamera.fieldOfView = glassesSettings.fieldOfView;
                rightEyeCamera.nearClipPlane = glassesSettings.nearClipPlane;
                rightEyeCamera.farClipPlane = glassesSettings.farClipPlane;
                rightEyeCamera.aspect = DisplaySettings.monoWidthToHeightRatio;
            }
        }

        /// <summary>
        /// EDITOR-ONLY
        /// </summary>
        void OnValidate()
        {

#if UNITY_EDITOR
            if (false == UnityEditor.EditorApplication.isPlaying)
                return;
#endif
            if (null == spectatorCamera)
                return;

            if (null != leftEye && null != rightEye)
                ShowHideCameras();

            SyncFields();
            SyncTransform();
        }

        /// <summary>
        /// Show/hide to the eye camerasin the hierarchy.
        /// </summary>
        void ShowHideCameras()
        {
            if (showCameras)
            {
                leftEye.hideFlags = HideFlags.None;
                rightEye.hideFlags = HideFlags.None;
            }
            else
            {
                leftEye.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                rightEye.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            }
        }
    }
}
