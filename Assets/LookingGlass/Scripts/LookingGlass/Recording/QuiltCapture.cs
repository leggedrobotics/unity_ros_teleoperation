//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

// Based on MIT licensed FFmpegOut - FFmpeg video encoding plugin for Unity
// https://github.com/keijiro/FFmpegOut

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Video;

using FFmpegOut;

using Hjg.Pngcs;
using LookingGlass.Toolkit;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LookingGlass {
    /// <summary>
    /// Provides a way to record quilt videos from within Unity scenes.
    /// </summary>
    [HelpURL("https://look.glass/unitydocs")]
    [RequireComponent(typeof(HologramCamera))]
    public sealed class QuiltCapture : MonoBehaviour {
#if ENABLE_INPUT_SYSTEM
        [Serializable]
        private struct ShortcutKeys {
            public Key screenshot2D;
            public Key screenshot3D;
        }
#endif

#if !ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
        [Serializable]
        private struct LegacyShortcutKeys {
            public KeyCode screenshot2D;
            public KeyCode screenshot3D;
        }
#endif

        [Serializable]
        private struct OutputFilePath {
            public static OutputFilePath UseDefault => new OutputFilePath { value = null, useDefault = true };
            public static OutputFilePath FromValue(string filePath) {
                Assert.IsFalse(string.IsNullOrWhiteSpace(filePath), "The given file path is assumed to be valid, because we set " + nameof(useDefault) + " to false!");
                return new OutputFilePath {
                    value = filePath,
                    useDefault = false
                };
            }

            public string value;
            public bool useDefault;
        }

        /// <summary>
        /// The key name corresponding to the take number value stored in <see cref="PlayerPrefs"/>.
        /// </summary>
        internal const string TakeNumberKey = "takeNumber";
        internal const string TakeNumberTooltip = "Counting the index of current video. Will be used for naming if file name includes '" + TakeVariablePattern + "'";

        private const string TakeVariablePattern = "${take}";
        private static readonly Lazy<Regex> DefaultFileNamePattern = new Lazy<Regex>(() =>
            new Regex("^((Screenshot)|(Recording))(\\${take})$"));

        /// <summary>
        /// Contains extra metadata that is added to captured videos through FFmpeg.
        /// </summary>
        private static MediaMetadataPair[] GetMediaMetadata() {
            return new MediaMetadataPair[] {
                new MediaMetadataPair("CAPTURED_BY", "Unity"),
                new MediaMetadataPair("UNITY_VERSION", Application.unityVersion),
                new MediaMetadataPair("LOOKINGGLASS_UNITY_PLUGIN_VERSION", HologramCamera.Version.ToString())
            };
        }

        [Tooltip("File name of the output video. If it's empty, it will be set to the default (see QuiltCapture.DefaultFileName).")]
        [SerializeField] internal string fileName = "Recording" + TakeVariablePattern;
        [SerializeField] internal OutputFolder folderPath = new OutputFolder();

        [SerializeField] internal QuiltCaptureMode captureMode = QuiltCaptureMode.SingleFrame;
        [SerializeField] internal QuiltScreenshotPreset screenshotPreset;
        [SerializeField] internal QuiltRecordingPreset recordingPreset;

        [SerializeField, HideInInspector] internal int startFrame;
        [SerializeField, HideInInspector] internal int endFrame = 30;
        [SerializeField, HideInInspector] internal float startTime;
        [SerializeField, HideInInspector] internal float endTime = 1;

        [SerializeField, HideInInspector] internal bool recordOnStart = false;
        [Tooltip("When set to true, play mode will exit when the recording is stopped.")]
        [SerializeField, HideInInspector] internal bool exitPlayModeOnStop = true;

        [SerializeField] internal QuiltRecordingSettings customRecordingSettings = QuiltRecordingSettings.GetSettings(QuiltRecordingPreset.LookingGlassGo);

        [Tooltip("A collection of settings that may be applied to single frame capture/screenshots.")]
        [SerializeField] internal QuiltCaptureOverrideSettings customScreenshotSettings = new QuiltCaptureOverrideSettings();

        [Tooltip("Set this to reference a VideoPlayer if you wish to end the recording immediately when a given VideoPlayer finishes playback.")]
        [SerializeField] internal VideoPlayer syncedVideoPlayer;

#if ENABLE_INPUT_SYSTEM
        [SerializeField] private ShortcutKeys shortcuts = new ShortcutKeys {
            screenshot2D = Key.F9,
            screenshot3D = Key.F10
        };
#endif

#if !ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
        [SerializeField] private LegacyShortcutKeys legacyShortcuts = new LegacyShortcutKeys {
            screenshot2D = KeyCode.F9,
            screenshot3D = KeyCode.F10
        };
#endif


        //NOTE: Duplicate logic with LookingGlass.initialized
        /// <summary>
        /// Allows us to initialize immediately during Awake,
        /// and re-initialize on every subsequence OnEnable call after being disabled and re-enabled.
        /// </summary>
        private bool initialized = false;

        private HologramCamera hologramCamera;
        private SyncedVideoPlayerCollection syncedCollection;
        public VideoPlayer[] SyncedVideoPlayers => syncedCollection.GetAll().ToArray();
        public void AddVideoPlayerToSync(VideoPlayer videoPlayer, bool freezeOnAdd = false) => syncedCollection.AddVideoPlayer(videoPlayer, freezeOnAdd);
        public void AddVideoPlayersToSync(IEnumerable<VideoPlayer> videoPlayers, bool freezeOnAdd = false) => syncedCollection.AddVideoPlayers(videoPlayers, freezeOnAdd);
        public void RemoveVideoPlayerFromSync(VideoPlayer videoPlayer, bool restoreOnRemove = true) => syncedCollection.RemoveVideoPlayer(videoPlayer, restoreOnRemove);
        public void RemoveVideoPlayersFromSync(IEnumerable<VideoPlayer> videoPlayers, bool restoreOnRemove = true) => syncedCollection.RemoveVideoPlayers(videoPlayers, restoreOnRemove);

#if UNITY_EDITOR || !UNITY_IOS
        private FFmpegSession session;
#endif
        private QuiltCaptureState state;

        internal bool overridesAreInEffect = false;
        private int previousCaptureFramerate;
        private bool previousAutomaticQuiltPreset;
        private QuiltPreset previousPreset;
        private bool previousPreview2D;
        private float previousAspect;
        private float previousNearClip;

        private RecorderTiming timing;
#if UNITY_EDITOR
        private bool alreadyImportedFFmpegShader = false;
#endif

        public event Action<QuiltCaptureState> onStateChanged;
        internal event Action<RenderTexture> onBeforePushFrame;

        public string FileName {
            get { return fileName; }
            set { fileName = value; }
        }

        public OutputFolder FolderPath {
            get { return folderPath; }
        }

        public int TakeNumber {
            get { return PlayerPrefs.GetInt(TakeNumberKey, 0); }
            set { PlayerPrefs.SetInt(TakeNumberKey, Mathf.Max(0, value)); }
        }

        public QuiltCaptureMode CaptureMode {
            get { return captureMode; }
            set {
                if (state != QuiltCaptureState.NotRecording)
                    throw new InvalidOperationException("Cannot change capture mode during recording!");
                captureMode = value;

                if (IsDefaultFileName(fileName))
                    fileName = GetDefaultFileName(CaptureMode);
            }
        }
        public QuiltScreenshotPreset ScreenshotPreset {
            get { return screenshotPreset; }
            set {
                if (State != QuiltCaptureState.NotRecording)
                    throw new InvalidOperationException("Cannot change screenshot preset during recording!");
                screenshotPreset = value;
            }
        }
        public QuiltRecordingPreset RecordingPreset {
            get { return recordingPreset; }
            set {
                if (State != QuiltCaptureState.NotRecording)
                    throw new InvalidOperationException("Cannot change recording preset during recording!");
                recordingPreset = value;
            }
        }

        public int StartFrame {
            get { return startFrame; }
            set {
                if (State != QuiltCaptureState.NotRecording)
                    throw new InvalidOperationException("Cannot change start frame during recording!");
                startFrame = Mathf.Max(0, value);
            }
        }

        public int EndFrame {
            get { return endFrame; }
            set {
                if (State != QuiltCaptureState.NotRecording)
                    throw new InvalidOperationException("Cannot change end frame during recording!");
                Mathf.Max(1, endFrame = value);
            }
        }

        public float StartTime {
            get { return startTime; }
            set {
                if (State != QuiltCaptureState.NotRecording)
                    throw new InvalidOperationException("Cannot change start time during recording!");
                startTime = value;
            }
        }

        public float EndTime {
            get { return endTime; }
            set {
                if (State != QuiltCaptureState.NotRecording)
                    throw new InvalidOperationException("Cannot change end time during recording!");
                endTime = value;
            }
        }

        public bool RecordOnStart {
            get { return recordOnStart; }
            set { recordOnStart = value; }
        }

        public bool ExitPlayModeOnStop {
            get { return exitPlayModeOnStop; }
            set { exitPlayModeOnStop = value; }
        }

        /// <summary>
        /// <para>The settings that will be used for recordings when <see cref="RecordingPreset"/> is set to <see cref="QuiltRecordingPreset.Custom"/>.</para>
        /// <para>The aspect returned is always greater than zero, substituted using the same logic as <see cref="HologramCamera.Aspect"/> for values less than or equal to zero.</para>
        /// </summary>
        public QuiltRecordingSettings CustomRecordingSettings {
            get {
                QuiltRecordingSettings result = customRecordingSettings;
                //if (result.cameraOverrideSettings.quiltSettings.aspect <= 0)
                //    result.cameraOverrideSettings.quiltSettings.aspect = HologramCamera.Aspect;
                return result;
            }
            set {
                if (state != QuiltCaptureState.NotRecording)
                    throw new InvalidOperationException("Cannot change settings during recording!");
                customRecordingSettings = value;
            }
        }

        /// <summary>
        /// <para>The settings that will be used for recordings.</para>
        /// <para>The aspect returned is always greater than zero, substituted using the same logic as <see cref="HologramCamera.Aspect"/> for values less than or equal to zero.</para>
        /// </summary>
        public QuiltRecordingSettings RecordingSettings {
            get {
                QuiltRecordingSettings settings;

                switch (recordingPreset) {
                    case QuiltRecordingPreset.Custom:
                        settings = CustomRecordingSettings;
                        break;
                    case QuiltRecordingPreset.Automatic:
                        QuiltRecordingPreset partial;
                        switch (HologramCamera.Calibration.GetDeviceType()) {
                            case LKGDeviceType.GoPortrait:
                            default:
                                partial = QuiltRecordingPreset.LookingGlassGo;
                                break;
                            case LKGDeviceType.PortraitGen2:
                                partial = QuiltRecordingPreset.LookingGlassPortrait;
                                break;
                            case LKGDeviceType._16inLandscapeGen3:
                                partial = QuiltRecordingPreset.LookingGlass16Landscape;
                                break;
                            case LKGDeviceType._16inPortraitGen3:
                                partial = QuiltRecordingPreset.LookingGlass16Portrait;
                                break;
                            case LKGDeviceType._32inLandscapeGen3:
                                partial = QuiltRecordingPreset.LookingGlass32Landscape;
                                break;
                            case LKGDeviceType._32inPortraitGen3:
                                partial = QuiltRecordingPreset.LookingGlass32Portrait;
                                break;
                            case LKGDeviceType._65inLandscapeGen2:
                                partial = QuiltRecordingPreset.LookingGlass65;
                                break;
                            case LKGDeviceType._16inGen2:
                                partial = QuiltRecordingPreset.LookingGlass16Gen2;
                                break;
                            case LKGDeviceType._32inGen2:
                                partial = QuiltRecordingPreset.LookingGlass32Gen2;
                                break;
                        }
                        settings = QuiltRecordingSettings.GetSettings(partial);
                        settings.cameraOverrideSettings = new QuiltCaptureOverrideSettings(HologramCamera);
                        break;
                    default:
                        settings = QuiltRecordingSettings.GetSettings(recordingPreset);
                        break;
                }

                if (captureMode == QuiltCaptureMode.ClipLength && syncedVideoPlayer != null)
                    settings.frameRate = (float) Math.Round(syncedVideoPlayer.frameRate * 100) / 100;
                //if (settings.cameraOverrideSettings.quiltSettings.aspect <= 0)
                //    settings.cameraOverrideSettings.quiltSettings.aspect = HologramCamera.Aspect;

                return settings;
            }
        }

        /// <summary>
        /// <para>The settings that will be used for screenshots when <see cref="ScreenshotPreset"/> is set to <see cref="QuiltScreenshotPreset.Custom"/>.</para>
        /// <para>The aspect returned is always greater than zero, substituted using the same logic as <see cref="HologramCamera.Aspect"/> for values less than or equal to zero.</para>
        /// </summary>
        public QuiltCaptureOverrideSettings CustomScreenshotSettings {
            get {
                QuiltCaptureOverrideSettings result = customScreenshotSettings;
                //REVIEW: [CRT-4039] Do we NEED -1 quiltAspect support?
                //Can we change it to have the field always, prevent it from negative values, and maybe have a small inline "Reset" button to reset to HologramCamera?
                //if (result.quiltSettings.quiltAspect <= 0)
                //    result.quiltSettings.quiltAspect = HologramCamera.QuiltSettings.quiltAspect;
                return result;
            }
            set {
                if (state != QuiltCaptureState.NotRecording)
                    throw new InvalidOperationException("Cannot change settings during recording!");
                customScreenshotSettings = value;
            }
        }

        /// <summary>
        /// <para>The settings that will be used for screenshots.</para>
        /// <para>The aspect returned is always greater than zero, substituted using the same logic as <see cref="HologramCamera.Aspect"/> for values less than or equal to zero.</para>
        /// </summary>
        public QuiltCaptureOverrideSettings ScreenshotSettings {
            get {
                QuiltCaptureOverrideSettings result;
                if (screenshotPreset == QuiltScreenshotPreset.Custom)
                    result = CustomScreenshotSettings;
                else if (screenshotPreset == QuiltScreenshotPreset.Automatic)
                    result = new QuiltCaptureOverrideSettings(HologramCamera);
                else
                    result = QuiltScreenshot.GetSettings(screenshotPreset);

                //if (result.quiltSettings.aspect <= 0)
                //    result.quiltSettings.aspect = HologramCamera.Aspect;
                return result;
            }
        }

        public QuiltCaptureOverrideSettings OverrideSettings
            => (CaptureMode == QuiltCaptureMode.SingleFrame) ? ScreenshotSettings : RecordingSettings.cameraOverrideSettings;

        private bool MatchVideoDuration => captureMode == QuiltCaptureMode.ClipLength;

        public QuiltCaptureState State {
            get { return state; }
            private set {
#if UNITY_EDITOR || !UNITY_IOS
                if (state == value)
                    return;
                QuiltCaptureState prevState = state;

                state = value;
                switch (state) {
                    case QuiltCaptureState.Recording:
                        timing = new RecorderTiming(RecordingSettings.frameRate);
                        UseOverrideSettings();
                        break;
                    default:
                        if (state == QuiltCaptureState.NotRecording)
                            TakeNumber++;
                        timing = new RecorderTiming();
                        ReleaseOverrideSettings();
                        break;
                }
                onStateChanged?.Invoke(state);
#else
                LogUnsupportedError();
#endif
            }
        }

        /// <summary>
        /// <para>Contains runtime data about the current frame's timing in the recording.
        /// NOTE: This is only valid during multi-frame recordings, and will be zeroed out otherwise.</para>
        /// <para>See also: <seealso cref="RecorderTiming"/></para>
        /// </summary>
        public RecorderTiming Timing {
            get {
                if (State == QuiltCaptureState.NotRecording || CaptureMode == QuiltCaptureMode.SingleFrame)
                    Debug.LogWarning(this + " is not currently multi-frame recording. The timing data will be all default values.");
                return timing;
            }
        }
        public HologramCamera HologramCamera {
            get {
                if (hologramCamera == null)
                    hologramCamera = GetComponent<HologramCamera>();
                return hologramCamera;
            }
        }

#if UNITY_EDITOR || !UNITY_IOS
        #region Unity Messages
        private void OnValidate() {
            CheckToLogClipLengthWarning();

            if (IsDefaultFileName(fileName))
                fileName = GetDefaultFileName(CaptureMode);
        }

        private void Awake() {
            initialized = true;
            Initialize();
        }

        private void OnEnable() {
            if (initialized)
                return;
            initialized = true;
            Initialize();
        }

        private void OnDisable() {
            initialized = false;
            StopRecording();
        }

        private void Update() {
            QuiltCaptureState state = State;
            if (state == QuiltCaptureState.NotRecording) {
                bool screenshot2D = false;
                bool screenshot3D = false;
#if ENABLE_INPUT_SYSTEM
                Keyboard keyboard = InputSystem.GetDevice<Keyboard>();
                if (keyboard != null) {
                    if (keyboard[shortcuts.screenshot2D].wasPressedThisFrame)
                        screenshot2D = true;
                    if (keyboard[shortcuts.screenshot3D].wasPressedThisFrame)
                        screenshot3D = true;
                }
#endif

#if !ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
                if (Input.GetKeyDown(legacyShortcuts.screenshot2D))
                    screenshot2D = true;
                if (Input.GetKeyDown(legacyShortcuts.screenshot3D))
                    screenshot3D = true;
#endif

                if (screenshot2D)
                    _ = Screenshot2D();
                else if (screenshot3D)
                    _ = Screenshot3D();
            }

            if (state == QuiltCaptureState.NotRecording || CaptureMode == QuiltCaptureMode.SingleFrame)
                return;

            HologramCamera hologramCamera = HologramCamera;
            RenderTexture quilt = hologramCamera.QuiltTexture;

            if (!hologramCamera) {
                Debug.LogWarning("[LookingGlass] Failed to record because no LookingGlass Capture instance exists.");
                return;
            }

            float gap = Time.time - timing.FrameTime;
            float delta = 1 / RecordingSettings.frameRate;
            bool pushedThisFrame = true;

            if (gap < 0 || state == QuiltCaptureState.Paused) {
                pushedThisFrame = false;
                // Update without frame data.
                session.PushFrame(null);
            } else if (gap < delta) {
                // Single-frame behind from the current time:
                // Push the current frame to FFmpeg.
                onBeforePushFrame?.Invoke(quilt);
                session.PushFrame(quilt);
                timing.OnFramePushed();
            } else if (gap < delta * 2) {
                // Two-frame behind from the current time:
                // Push the current frame twice to FFmpeg. Actually this is not
                // an efficient way to catch up. We should think about
                // implementing frame duplication in a more proper way. #fixme

                onBeforePushFrame?.Invoke(quilt);
                session.PushFrame(quilt);
                timing.OnFramePushed();

                onBeforePushFrame?.Invoke(quilt);
                session.PushFrame(quilt);
                timing.OnFramePushed();
            } else {
                // Show a warning message about the situation.
                timing.OnFrameDropped();

                // Push the current frame to FFmpeg.
                onBeforePushFrame?.Invoke(quilt);
                session.PushFrame(quilt);

                // Compensate the time delay.
                timing.CatchUp(gap);
            }

            if (pushedThisFrame)
                syncedCollection.StepAll();
        }
        #endregion

        private void Initialize() {
            //Store all video players and their playback speeds
            syncedCollection = new SyncedVideoPlayerCollection(FindObjectsOfType<VideoPlayer>());

            // To begin record, make component enable
            if (captureMode == QuiltCaptureMode.Manual && recordOnStart)
                StartRecordingInternal(OutputFilePath.UseDefault);
            else {
                State = QuiltCaptureState.NotRecording;
                if (captureMode == QuiltCaptureMode.FrameInterval) {
                    _ = StartRecordingFrames(startFrame, endFrame);
                } else if (captureMode == QuiltCaptureMode.TimeInterval) {
                    _ = StartRecording(startTime, endTime);
                } else if (MatchVideoDuration) {
                    if (CheckToLogClipLengthWarning()) {
#if UNITY_EDITOR
                        if (ExitPlayModeOnStop)
                            EditorApplication.isPlaying = false;
#endif
                        return;
                    }
                    _ = StartRecordingFrames(0, (int) syncedVideoPlayer.clip.frameCount);
                }
            }

            StartCoroutine(SyncFFmpegCoroutine());
        }

        private IEnumerator SyncFFmpegCoroutine() {
            YieldInstruction wait = new WaitForEndOfFrame();

            yield return wait;
            while (isActiveAndEnabled) {
                if (session != null)
                    session.CompletePushFrames();
                yield return wait;
            }
        }

        private bool CheckToLogClipLengthWarning() {
            if (MatchVideoDuration && (syncedVideoPlayer == null || syncedVideoPlayer.clip == null)) {
                Debug.LogWarning("No synced video player or video clip referenced. Cannot match recording duration.");
                return true;
            }
            return false;
        }

        private void ValidateIsManualOrCanStartManual() {
            if (CaptureMode != QuiltCaptureMode.Manual && State != QuiltCaptureState.NotRecording)
                throw new InvalidOperationException("Cannot use manual recording methods! You must stop recording, or already be in the manual recording mode.");
        }

        private void ValidateNonDefaultFilePath(in OutputFilePath outputFilePath) {
            if (!outputFilePath.useDefault) {
                if (outputFilePath.value == null)
                    throw new ArgumentNullException(nameof(outputFilePath));
                if (string.IsNullOrWhiteSpace(outputFilePath.value))
                    throw new ArgumentException("The output file path cannot be empty or whitespace.", nameof(outputFilePath));
            }
        }

        public void UseOverrideSettings() {
            if (overridesAreInEffect)
                return;

            HologramCamera hologramCamera = HologramCamera;
            if (!hologramCamera) {
                Debug.LogWarning("[LookingGlass] Failed to set up quilt settings because no LookingGlass Capture instance exists");
                return;
            }

            overridesAreInEffect = true;
            previousAutomaticQuiltPreset = hologramCamera.AutomaticQuiltPreset;
            previousPreset = hologramCamera.QuiltPreset;
            previousPreview2D = hologramCamera.Preview2D;
            previousNearClip = hologramCamera.CameraProperties.NearClipFactor;

            //REVIEW: [CRT-4039] I deleted setting hologramCamera.cal.aspect -- rendering should be based off of the native ScreenAspect and the QuiltSettings.quiltAspect instead, right?
            QuiltCaptureOverrideSettings overrideSettings = OverrideSettings;
            hologramCamera.SetQuiltPreset(false, new QuiltPreset(overrideSettings.quiltSettings));
            hologramCamera.Preview2D = false;
            hologramCamera.CameraProperties.NearClipFactor = overrideSettings.nearClipFactor; //TODO: We need to handle the case of LookingGlassTransformMode.Camera using nearClipPlane instead!
            hologramCamera.LockRenderSettingsForRecording(this);
            hologramCamera.RenderQuilt(true);
        }

        public void ReleaseOverrideSettings() {
            if (!overridesAreInEffect)
                return;

            HologramCamera hologramCamera = HologramCamera;
            if (!hologramCamera) {
                Debug.LogWarning("[LookingGlass] Failed to restore quilt settings because no LookingGlass Capture instance exists");
                return;
            }

            overridesAreInEffect = false;
            hologramCamera.UnlockRenderSettingsFromRecording(this);
            hologramCamera.Preview2D = previousPreview2D;
            hologramCamera.SetQuiltPreset(previousAutomaticQuiltPreset, previousPreset);
            hologramCamera.CameraProperties.NearClipFactor = previousNearClip;
        }
#endif

        #region File Naming
        public string GetDefaultPrefix(QuiltCaptureMode captureMode) => captureMode == QuiltCaptureMode.SingleFrame ? "Screenshot" : "Recording";
        public string GetDefaultFileName(QuiltCaptureMode captureMode) => GetDefaultPrefix(captureMode) + TakeVariablePattern;
        public bool IsDefaultFileName(string fileName) => DefaultFileNamePattern.Value.IsMatch(fileName);

        public string CalculateAutoCorrectPath() => CalculateAutoCorrectPath(CaptureMode);
        private string CalculateAutoCorrectPath(QuiltCaptureMode captureMode) {
            QuiltSettings quiltSettings = OverrideSettings.quiltSettings;
            Assert.IsTrue(quiltSettings.renderAspect > 0);
            return CalculateAutoCorrectPath(captureMode, quiltSettings.columns, quiltSettings.rows, quiltSettings.renderAspect, TakeNumber);
        }
        public string CalculateAutoCorrectPath(QuiltCaptureMode captureMode, int viewColumns, int viewRows, float finalAspect, int takeNumber) {
            if (finalAspect <= 0)
                throw new ArgumentOutOfRangeException(nameof(finalAspect), finalAspect,
                    "The aspect must be greater than zero. See " + nameof(QuiltSettings) + "." + nameof(QuiltSettings.renderAspect) + ".");

            string quiltSuffix = "_qs" + viewColumns + "x" + viewRows + "a" + finalAspect.ToString("F2"); //NOTE: The aspect here is of the LKG device's native screen resolution.
            string fileExtension = captureMode == QuiltCaptureMode.SingleFrame ? ".png" : RecordingSettings.codec.GetFileExtension();

            string fileName = this.fileName;
            if (IsDefaultFileName(fileName))
                fileName = GetDefaultFileName(captureMode);
            fileName = fileName.Replace(TakeVariablePattern, takeNumber.ToString()) + quiltSuffix + fileExtension;

            string outputPath = Path.Combine(folderPath.GetFullPath(), fileName).Replace("\\", "/");
            return outputPath;
        }
        #endregion

        #region Recording Methods
#if UNITY_EDITOR || !UNITY_IOS
        /// <summary>
        /// <para>Starts a recording session that will output a video file to a file at the default path.</para>
        /// <para>See also: <seealso cref="CalculateAutoCorrectPath"/></para>
        /// </summary>
        public void StartRecording() {
            ValidateIsManualOrCanStartManual();
            if (CaptureMode != QuiltCaptureMode.Manual)
                CaptureMode = QuiltCaptureMode.Manual;
            StartRecordingInternal(OutputFilePath.UseDefault);
        }

        /// <summary>
        /// Starts a recording session that will output a video file to a file at the given <paramref name="outputFilePath"/>.
        /// </summary>
        public void StartRecording(string outputFilePath) {
            ValidateIsManualOrCanStartManual();
            if (CaptureMode != QuiltCaptureMode.Manual)
                CaptureMode = QuiltCaptureMode.Manual;
            StartRecordingInternal(OutputFilePath.FromValue(outputFilePath));
        }

        public void PauseRecording() {
            ValidateIsManualOrCanStartManual();
            if (CaptureMode != QuiltCaptureMode.Manual)
                CaptureMode = QuiltCaptureMode.Manual;
            PauseRecordingInternal();
        }

        public void ResumeRecording() {
            ValidateIsManualOrCanStartManual();
            if (CaptureMode != QuiltCaptureMode.Manual)
                CaptureMode = QuiltCaptureMode.Manual;
            ResumeRecordingInternal();
        }

        public void StopRecording() {
            if (State == QuiltCaptureState.NotRecording)
                return;

            // set the playback speed back to original
            syncedCollection.RestoreAll();

            if (session != null) {
                Debug.Log("Closing FFmpegSession after " + timing.FrameCount + " frames.");
                session.Close();
                session.Dispose();
                session = null;
            }

            if (GetComponent<FrameRateController>() == null)
                Time.captureFramerate = previousCaptureFramerate;

            State = QuiltCaptureState.NotRecording;

#if UNITY_EDITOR
            if (ExitPlayModeOnStop)
                EditorApplication.isPlaying = false;
#endif
        }

        public async Task StartRecordingFrames(int startFrame, int endFrame) => await StartRecordingFramesInternal(startFrame, endFrame, OutputFilePath.UseDefault);
        public async Task StartRecordingFrames(int startFrame, int endFrame, string outputFilePath) => await StartRecordingFramesInternal(startFrame, endFrame, OutputFilePath.FromValue(outputFilePath));
        public async Task StartRecording(float startTime, float endTime) => await StartRecordingInternal(startTime, endTime, OutputFilePath.UseDefault);
        public async Task StartRecording(float startTime, float endTime, string outputFilePath) => await StartRecordingInternal(startTime, endTime, OutputFilePath.FromValue(outputFilePath));
        public ScreenshotProgress Screenshot2D(bool writePNGMetadata = true) => Screenshot2DInternal(OutputFilePath.UseDefault, writePNGMetadata);
        public ScreenshotProgress Screenshot3D(bool writePNGMetadata = true) => Screenshot3DInternal(OutputFilePath.UseDefault, writePNGMetadata);
        public ScreenshotProgress Screenshot3DLayer(bool writePNGMetadata = true) => Screenshot3DLayerInternal(OutputFilePath.UseDefault, writePNGMetadata);
        public ScreenshotProgress Screenshot2D(string outputFilePath, bool writePNGMetadata = true) => Screenshot2DInternal(OutputFilePath.FromValue(outputFilePath), writePNGMetadata);
        public ScreenshotProgress Screenshot3D(string outputFilePath, bool writePNGMetadata = true) => Screenshot3DInternal(OutputFilePath.FromValue(outputFilePath), writePNGMetadata);
        public ScreenshotProgress Screenshot3DLayer(string outputFilePath, bool writePNGMetadata = true) => Screenshot3DLayerInternal(OutputFilePath.FromValue(outputFilePath), writePNGMetadata);

        private void StartRecordingInternal(OutputFilePath outputFilePath) {
            Assert.AreNotEqual(QuiltCaptureState.Recording, State, this + " is already recording!");
            if (!Application.isPlaying)
                throw new InvalidOperationException("You can only call " + nameof(StartRecording) + " in playmode.");

            ValidateNonDefaultFilePath(outputFilePath);

            //NOTE: We DO NOT set our CaptureMode to Manual here, because our other recording methods
            //rely on this functionality! But that's OK cause this method is just a private helper.
            //The StartRecording(...) method overloads are expected to be called publicly/externally,
            //so they DO set our CaptureMode to Manual.

            if (outputFilePath.useDefault)
                outputFilePath.value = CalculateAutoCorrectPath();

            CheckToLogClipLengthWarning();

            syncedCollection.FreezeAll();

            HologramCamera hologramCamera = HologramCamera;
            if (!hologramCamera) {
                Debug.LogWarning("[LookingGlass] Failed to start recorder because no LookingGlass Capture instance exists.");
            }

            if (session != null)
                session.Dispose();

            string fullPath = Path.GetFullPath(outputFilePath.value);
            string dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            State = QuiltCaptureState.Recording;

            RenderTexture quilt = hologramCamera.QuiltTexture;
            Debug.Log("Creating FFmpeg session with size "
                + quilt.width + "x" + quilt.height + ", will be saved at " + fullPath);

            QuiltRecordingSettings recordingSettings = RecordingSettings;
            string extraFFmpegOptions = "-b:v " + recordingSettings.targetBitrateInMegabits + "M";

#if !UNITY_EDITOR_OSX && !UNITY_STANDALONE_OSX
            switch (recordingSettings.codec) {
                case FFmpegPreset.H264Nvidia:
                case FFmpegPreset.HevcNvidia:
                    extraFFmpegOptions += " -cq:v ";
                    break;
                default:
                    extraFFmpegOptions += " -crf ";
                    break;
            }
            extraFFmpegOptions += recordingSettings.compression;
#endif

#if UNITY_EDITOR
            //This fixes FFmpegSession using Shader.Find("Hidden/FFmpegOut/Preprocess") returning null!
            if (!alreadyImportedFFmpegShader) {
                alreadyImportedFFmpegShader = true;
                string resourcesFolderGuid = "0c36e64b6a30f4a43abc488dc63a3323";
                string resourcesFolderPath = AssetDatabase.GUIDToAssetPath(resourcesFolderGuid);
                AssetDatabase.ImportAsset(resourcesFolderPath, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceSynchronousImport);
            }
#endif

            session = FFmpegSession.CreateWithOutputPath(outputFilePath.value, quilt.width, quilt.height, timing.FrameRate, recordingSettings.codec, extraFFmpegOptions, GetMediaMetadata());

            if (GetComponent<FrameRateController>() == null) {
                previousCaptureFramerate = Time.captureFramerate;
                Time.captureFramerate = Mathf.RoundToInt(timing.FrameRate);
            }
        }

        public void PauseRecordingInternal() {
            if (State == QuiltCaptureState.Recording)
                State = QuiltCaptureState.Paused;
            else
                Debug.LogWarning("[LookingGlass] Can't pause recording when it's not started.");
        }

        public void ResumeRecordingInternal() {
            if (State == QuiltCaptureState.Paused)
                State = QuiltCaptureState.Recording;
            else
                Debug.LogWarning("[LookingGlass] Can't resume recording when it's not paused.");
        }

        private async Task StartRecordingFramesInternal(int startFrame, int endFrame, OutputFilePath outputFilePath) {
            ValidateNonDefaultFilePath(outputFilePath);

            int frameCount = endFrame - startFrame;
            if (frameCount <= 0)
                throw new ArgumentException("The total number of frames to record must be greater than zero!");

            CaptureMode = QuiltCaptureMode.FrameInterval;
            StartFrame = startFrame;
            EndFrame = endFrame;

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            StartCoroutine(FrameIntervalRecordingCoroutine(startFrame, endFrame, tcs, outputFilePath));
            try {
                await tcs.Task;
            } catch (Exception e) {
                Debug.LogException(e);
                throw;
            }
        }

        private async Task StartRecordingInternal(float startTime, float endTime, OutputFilePath outputFilePath) {
            ValidateNonDefaultFilePath(outputFilePath);

            float duration = endTime - startTime;
            if (duration <= 0)
                throw new ArgumentException("The total duration to record for must be greater than zero!");

            CaptureMode = QuiltCaptureMode.TimeInterval;
            StartTime = startTime;
            EndTime = endTime;
            //StartRecordingInternal(outputFilePath);

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            StartCoroutine(TimeIntervalRecordingCoroutine(startTime, endTime, tcs, outputFilePath));
            try {
                await tcs.Task;
            } catch (Exception e) {
                Debug.LogException(e);
                throw;
            }
        }

        private ScreenshotProgress Screenshot2DInternal(OutputFilePath outputFilePath, bool writePNGMetadata = true) {
            ValidateNonDefaultFilePath(outputFilePath);
            if (outputFilePath.useDefault)
                outputFilePath.value = CalculateAutoCorrectPath(QuiltCaptureMode.SingleFrame); //IMPORTANT: We must pass in the capture mode here, because we haven't SET it yet!

            Task<Texture2D> screenshotTask = PerformAfterOverrideSettingsAreInEffect(QuiltCaptureMode.SingleFrame, async () => {
                Util.EncodeToPNGBytes(HologramCamera.RenderPreview2D(true), out Texture2D screenshot, out byte[] bytes);
                await Task.Run(() => SaveScreenshot(outputFilePath.value, bytes));
                return screenshot;
            });
            Task metadataTask = (writePNGMetadata) ? AddPNGMetadataAfterScreenshot(screenshotTask, outputFilePath.value) : null;
            return ScreenshotProgress.Create(false, outputFilePath.value, screenshotTask, metadataTask);
        }

        private ScreenshotProgress Screenshot3DInternal(OutputFilePath outputFilePath, bool writePNGMetadata = true) {
            ValidateNonDefaultFilePath(outputFilePath);
            if (outputFilePath.useDefault)
                outputFilePath.value = CalculateAutoCorrectPath(QuiltCaptureMode.SingleFrame); //IMPORTANT: We must pass in the capture mode here, because we haven't SET it yet!

            Task<Texture2D> screenshotTask = PerformAfterOverrideSettingsAreInEffect(QuiltCaptureMode.SingleFrame, async () => {
                Util.EncodeToPNGBytes(HologramCamera.RenderStack.QuiltMix, out Texture2D screenshot, out byte[] bytes);
                await Task.Run(() => SaveScreenshot(outputFilePath.value, bytes));
                return screenshot;
            });
            Task metadataTask = (writePNGMetadata) ? AddPNGMetadataAfterScreenshot(screenshotTask, outputFilePath.value) : null;
            return ScreenshotProgress.Create(true, outputFilePath.value, screenshotTask, metadataTask);
        }

        private ScreenshotProgress Screenshot3DLayerInternal(OutputFilePath outputFilePath, bool writePNGMetadata = true) {
            ValidateNonDefaultFilePath(outputFilePath);
            if (outputFilePath.useDefault)
                outputFilePath.value = CalculateAutoCorrectPath(QuiltCaptureMode.SingleFrame); //IMPORTANT: We must pass in the capture mode here, because we haven't SET it yet!

            Task<Texture2D> screenshotTask = PerformAfterOverrideSettingsAreInEffect(QuiltCaptureMode.SingleFrame, async () => {
                Util.EncodeToPNGBytes(HologramCamera.QuiltTexture, out Texture2D screenshot, out byte[] bytes);
                await Task.Run(() => SaveScreenshot(outputFilePath.value, bytes));
                return screenshot;
            });
            Task metadataTask = (writePNGMetadata) ? AddPNGMetadataAfterScreenshot(screenshotTask, outputFilePath.value) : null;
            return ScreenshotProgress.Create(true, outputFilePath.value, screenshotTask, metadataTask);
        }

        //TODO: Make this delay logic more standardized and documented
        //For existing explanation, search slack in #software-unity-plugin for "This is a demo of me spam-clicking the SingleFrame capture"
        private async Task<T> PerformAfterOverrideSettingsAreInEffect<T>(QuiltCaptureMode captureMode, Func<Task<T>> callback) {
            try {
                TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

                CaptureMode = captureMode;
                State = QuiltCaptureState.Recording;
                HologramCamera.RenderQuilt(true);

#if UNITY_EDITOR
                GameViewExtensions.RepaintAllViewsImmediately();
                EditorApplication.delayCall += () => {
                    EditorUpdates.Delay(1, async () => {
#else
                        await Task.Delay(100);
#endif
                        if (CaptureMode != captureMode) {
                            tcs.SetException(new TaskCanceledException());
                        } else {
                            //But we NEED to re-render the quilt, or else our quilt texture would be outdated below when we save it to a screenshot!
                            HologramCamera.RenderQuilt(true);
#if UNITY_EDITOR
                            GameViewExtensions.RepaintAllViewsImmediately();
#endif
                            T result = default;
                            Exception exception = null;
                            try {
                                result = await callback();
                            } finally {
                                State = QuiltCaptureState.NotRecording;
                                HologramCamera.RenderQuilt(true);

                                if (exception != null)
                                    tcs.SetException(exception);
                                else
                                    tcs.SetResult(result);
                            }
                        }
#if UNITY_EDITOR
                    });
                };
#endif
                return await tcs.Task;
            } catch (Exception e) {
                Debug.LogException(e);
                throw;
            }
        }

        private void SaveScreenshot(string filePath, byte[] bytes) {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllBytes(filePath, bytes);
        }

        private IEnumerator FrameIntervalRecordingCoroutine(int startFrame, int endFrame, TaskCompletionSource<bool> tcs, OutputFilePath outputFilePath) {
            QuiltCaptureMode captureMode = QuiltCaptureMode.FrameInterval;

            int frameCount = endFrame - startFrame;
            Debug.Log("Recording will start after " + startFrame + " frame(s).");
            for (int i = 0; i < startFrame; i++)
                yield return null;

            if (CaptureMode != captureMode) {
                tcs.SetException(new TaskCanceledException(captureMode + " recording was interrupted by another capture mode: " + CaptureMode + "!"));
                yield break;
            }

            StartRecordingInternal(outputFilePath);


            Debug.Log("Recording will end after " + frameCount + " frame(s).");
            for (int i = 0; timing.FrameCount < frameCount; i++)
                yield return null;

            if (CaptureMode != captureMode) {
                tcs.SetException(new TaskCanceledException(captureMode + " recording was interrupted by another capture mode: " + CaptureMode + "!"));
                yield break;
            }
            StopRecording();
            tcs.SetResult(true);
        }

        private IEnumerator TimeIntervalRecordingCoroutine(float startTime, float endTime, TaskCompletionSource<bool> tcs, OutputFilePath outputFilePath) {
            QuiltCaptureMode captureMode = QuiltCaptureMode.TimeInterval;

            Debug.Log("Recording will start after " + startTime + " second(s).");
            yield return new WaitForSeconds(startTime);

            if (CaptureMode != captureMode) {
                tcs.SetException(new TaskCanceledException(captureMode + " recording was interrupted by another capture mode: " + CaptureMode + "!"));
                yield break;
            }

            StartRecordingInternal(outputFilePath);

            float duration = endTime - startTime;
            Debug.Log("Recording will end after " + duration + " second(s).");
            for (int i = 0; timing.FrameCount < duration * timing.FrameRate; i++)
                yield return null;

            if (CaptureMode != captureMode) {
                tcs.SetException(new TaskCanceledException(captureMode + " recording was interrupted by another capture mode: " + CaptureMode + "!"));
                yield break;
            }
            StopRecording();
            tcs.SetResult(true);
        }

        private async Task AddPNGMetadataAfterScreenshot(Task screenshotTask, string pngFilePath) {
            await screenshotTask;
            await AddPNGMetadata(pngFilePath);
        }
#else
        public void StartRecording() => LogUnsupportedError();
        public void StartRecording(string outputFilePath) => LogUnsupportedError();
        public void PauseRecording() => LogUnsupportedError();
        public void ResumeRecording() => LogUnsupportedError();
        public void StopRecording() => LogUnsupportedError();

        public async Task StartRecordingFrames(int startFrame, int endFrame) => LogUnsupportedError();
        public async Task StartRecordingFrames(int startFrame, int endFrame, string outputFilePath) => LogUnsupportedError();
        public async Task StartRecording(float startTime, float endTime) => LogUnsupportedError();
        public async Task StartRecording(float startTime, float endTime, string outputFilePath) => LogUnsupportedError();
        public ScreenshotProgress Screenshot2D(bool writePNGMetadata = true) { LogUnsupportedError(); return null; }
        public ScreenshotProgress Screenshot3D(bool writePNGMetadata = true) { LogUnsupportedError(); return null; }
        public ScreenshotProgress Screenshot3DLayer(bool writePNGMetadata = true) { LogUnsupportedError(); return null; }
        public ScreenshotProgress Screenshot2D(string outputFilePath, bool writePNGMetadata = true) { LogUnsupportedError(); return null; }
        public ScreenshotProgress Screenshot3D(string outputFilePath, bool writePNGMetadata = true) { LogUnsupportedError(); return null; }
        public ScreenshotProgress Screenshot3DLayer(string outputFilePath, bool writePNGMetadata = true) { LogUnsupportedError(); return null; }

        public void PauseRecordingInternal() => LogUnsupportedError();
        public void ResumeRecordingInternal() => LogUnsupportedError();
#endif
        #endregion

        /// <summary>
        /// <para>Adds <see cref="QuiltCapture"/> metadata to the PNG file at the given <paramref name="pngFilePath"/>.</para>
        /// 
        /// <remarks>
        /// <para>
        /// PNG file metadata is unique to its own binary format, differs from media metadata encoded with FFmpeg, and is incompatible.<br />
        /// Use <a href="https://products.groupdocs.app/metadata/png">this website</a> to inspect PNG file metadata!
        /// </para>
        /// <para>
        /// It seems you can't view the metadata in Windows OS natively without some custom library or program.
        /// </para>
        /// </remarks>
        /// </summary>
        /// <param name="pngFilePath"></param>
        private async Task AddPNGMetadata(string pngFilePath) {
#if UNITY_EDITOR
            int progressId = Progress.Start("Add PNG Metadata", Path.GetFileName(pngFilePath), Progress.Options.Indefinite | Progress.Options.Synchronous);
#endif
            try {
                Task metadataTask = Task.Run(() => {
                    PngReader reader = FileHelper.CreatePngReader(pngFilePath);
                    PngWriter writer = null;
                    try {
                        ImageInfo info = reader.ImgInfo;
                        ImageLines line = reader.ReadRowsByte();
                        writer = FileHelper.CreatePngWriter(pngFilePath, info, true);

                        //NOTE: We might want to add extra standard metadata keys using the constants at PngChunkTextVar.KEY_XXX

                        MediaMetadataPair[] metadata = GetMediaMetadata();
                        for (int i = 0; i < metadata.Length; i++)
                            writer.GetMetadata().SetText(metadata[i].key, metadata[i].value);

                        writer.WriteRowsByte(line.ScanlinesB);
                    } finally {
                        if (writer != null)
                            writer.End();
                        if (reader != null)
                            reader.End();
                    }
                });

#if UNITY_EDITOR
                while (!metadataTask.IsCompleted) {
                    await Task.Delay(30);
                    Progress.Report(progressId, 0, 1, null);
                }
#else
                await metadataTask;
#endif

            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
#if UNITY_EDITOR
                Progress.Finish(progressId);
#endif
            }

        }

#if !UNITY_EDITOR && UNITY_IOS
        private float timeLastAttempted = float.NegativeInfinity;

        private void LogUnsupportedError() {
            timeLastAttempted = Time.time;
            Debug.LogError("Recording is not supported in iOS builds.");
        }

        private void OnGUI() {
            if (Time.time - timeLastAttempted < 5) {
                string warningText = "Recording is not supported in iOS builds.";

                // Define the style for the warning message
                GUIStyle warningStyle = new GUIStyle(GUI.skin.label);
                warningStyle.fontSize = 40;
                warningStyle.fontStyle = FontStyle.Bold;
                warningStyle.normal.textColor = Color.red;

                // Calculate the size of the text
                Vector2 textSize = warningStyle.CalcSize(new GUIContent(warningText));

                // Calculate the position to center the warning message
                float xPos = (Screen.width - textSize.x) / 2;
                float yPos = (Screen.height - textSize.y) / 2;

                // Display the warning message
                GUI.Label(new Rect(xPos, yPos, textSize.x, textSize.y), warningText, warningStyle);
            }
        }
#endif

    }
}

