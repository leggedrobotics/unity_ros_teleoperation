// Inspired by Unity Recorder editor
#if UNITY_EDITOR || !UNITY_IOS
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using LookingGlass.Editor.EditorPropertyGroups;

namespace LookingGlass.Editor {
    [CustomEditor(typeof(QuiltCapture))]
    public class QuiltCaptureEditor : UnityEditor.Editor {
        private static Texture2D openPathIcon;
        private static int padding = 30;

        private bool updateCustomSettings = false;
        private bool isPresetSettingsExpanded = false;

        private QuiltCapture quiltCapture;
        private SerializedProperty captureModeProperty;
        private SerializedProperty startFrameProperty;
        private SerializedProperty endFrameProperty;
        private SerializedProperty startTimeProperty;
        private SerializedProperty endTimeProperty;
        private SerializedProperty exitPlayModeOnStopProperty;
        private SerializedProperty captureOnStartProperty;
        private SerializedProperty syncedVideoClipProperty;
        private SerializedProperty customRecordingSettingsProperty;
        private SerializedProperty customScreenshotSettingsProperty;

        private static class Styles {
            internal static readonly GUIContent CaptureModeLabel = new GUIContent("Capture Mode", "Manual:\nUse the “Record” and “Stop” buttons during Play mode.\n\nFrame Interval:\nSet Start and End frames\n\nTime Interval:\nSet Start and End times (seconds)\n\nClip Length (Automatic):\nSelect a Video Player with the “Synced Video” field.  Upon play, Quilt Capture will start and stop a recording to fit the beginning and end of the video clip\n\nSingle Frame:\nSave a single frame as .png");
            internal static readonly GUIContent StartLabel = new GUIContent("Start");
            internal static readonly GUIContent EndLabel = new GUIContent("End");
        }

        public static readonly Color RedRecordingColor = new Color(1, 0.5f, 0.5f, 1);
        public static readonly Color BlueStartColor = new Color(0.5f, 0.75f, 1, 1);

        private void OnEnable() {
            if (openPathIcon == null) {
                string iconName = "popout_icon";
                if (EditorGUIUtility.isProSkin)
                    iconName = "d_" + iconName;

                openPathIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/LookingGlass/Textures/" + iconName + ".png");
            }

            SerializedObject serializedObject = this.serializedObject;
            //Initialize the take number upon opening this inspector
            quiltCapture = (QuiltCapture) target;
            serializedObject.Update();

            captureModeProperty = serializedObject.FindProperty(nameof(QuiltCapture.captureMode));
            startFrameProperty = serializedObject.FindProperty(nameof(QuiltCapture.startFrame));
            endFrameProperty = serializedObject.FindProperty(nameof(QuiltCapture.endFrame));
            startTimeProperty = serializedObject.FindProperty(nameof(QuiltCapture.startTime));
            endTimeProperty = serializedObject.FindProperty(nameof(QuiltCapture.endTime));
            exitPlayModeOnStopProperty = serializedObject.FindProperty(nameof(QuiltCapture.exitPlayModeOnStop));
            captureOnStartProperty = serializedObject.FindProperty(nameof(QuiltCapture.recordOnStart));
            syncedVideoClipProperty = serializedObject.FindProperty(nameof(quiltCapture.syncedVideoPlayer));
            customRecordingSettingsProperty = serializedObject.FindProperty(nameof(QuiltCapture.customRecordingSettings));
            customScreenshotSettingsProperty = serializedObject.FindProperty(nameof(QuiltCapture.customScreenshotSettings));

            customRecordingSettingsProperty.isExpanded = true;
            customScreenshotSettingsProperty.isExpanded = true;
            customScreenshotSettingsProperty.FindPropertyRelative(nameof(QuiltCaptureOverrideSettings.quiltSettings)).isExpanded = true;
        }

        private void TryOpenInFileBrowser(string path) {
            try {
                //This does nothing if the directory already exists,
                //And creates it if it doesn't yet exist.
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            } catch (Exception) {
                // An error occured, most likely because the path was null
                Debug.LogWarning($"Error opening location {path} in the file browser.");
            }
            OpenInFileBrowser.Open(path);
        }

        public override void OnInspectorGUI() {
            // Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
            SerializedObject serializedObject = this.serializedObject;
            serializedObject.Update();

            SectionGUILayout.DrawDefaultInspectorWithSections(serializedObject, null, CustomGUI);

            GUILayout.Space(20);

            bool isPlaying = Application.isPlaying;
            QuiltCaptureMode captureMode = (QuiltCaptureMode) captureModeProperty.enumValueIndex;

            if (captureMode != QuiltCaptureMode.SingleFrame) {
                switch (captureMode) {
                    case QuiltCaptureMode.ClipLength:
                        if (quiltCapture.syncedVideoPlayer == null) {
                            EditorGUILayout.HelpBox("No video player referenced", MessageType.Error);
                        } else if (quiltCapture.syncedVideoPlayer.clip == null) {
                            EditorGUILayout.HelpBox("No video clip referenced", MessageType.Error);
                        } else {
                            EditorGUILayout.HelpBox("Frame rate is overridden by referenced video clip: " + quiltCapture.RecordingSettings.frameRate, MessageType.Info);
                        }
                        break;
                    case QuiltCaptureMode.FrameInterval:
                        if (endFrameProperty.intValue <= startFrameProperty.intValue) {
                            EditorGUILayout.HelpBox("The end frame needs to be greater than start frame", MessageType.Error);
                        }
                        break;
                    case QuiltCaptureMode.TimeInterval:
                        if (endTimeProperty.floatValue <= startTimeProperty.floatValue) {
                            EditorGUILayout.HelpBox("The end time needs to be greater than start time", MessageType.Error);
                        }
                        break;
                }
                bool isManualMode = captureMode == QuiltCaptureMode.Manual;
                if (!isManualMode)
                    EditorGUILayout.HelpBox("You must switch to Manual Mode to manually start recording", MessageType.Warning);
                else if (!isPlaying)
                    EditorGUILayout.HelpBox("You must enter playmode to start recording more than 1 frame.", MessageType.Warning);

#if UNITY_EDITOR_OSX
                EditorGUILayout.HelpBox("Note that capture is not supported in iOS build.", MessageType.Warning);
#endif
                Color prevColor = GUI.color;
                Color prevBackgroundColor = GUI.backgroundColor;
                Color prevContentColor = GUI.contentColor;

                try {
                    using (new EditorGUI.DisabledScope(!isPlaying)) {
                        QuiltCaptureState state = quiltCapture.State;
                        if (state == QuiltCaptureState.Recording || state == QuiltCaptureState.Paused) {
                            GUI.color = RedRecordingColor;
                            if (GUILayout.Button("Stop Recording")) {
                                quiltCapture.StopRecording();
                            }
                        }
                        GUI.color = BlueStartColor;
                        if (state == QuiltCaptureState.NotRecording) {
                            if (GUILayout.Button("Start Recording")) {
                                quiltCapture.StartRecording();
                            }
                        }
                        if (isManualMode) {

                            if (state == QuiltCaptureState.Recording) {
                                if (GUILayout.Button("Pause Recording")) {
                                    quiltCapture.PauseRecording();
                                }
                            }
                            if (state == QuiltCaptureState.Paused) {
                                if (GUILayout.Button("Resume Recording")) {
                                    quiltCapture.ResumeRecording();
                                }
                            }
                        }
                    }
                } finally {
                    GUI.color = prevColor;
                    GUI.backgroundColor = prevBackgroundColor;
                    GUI.contentColor = prevContentColor;
                }
            } else if (captureMode == QuiltCaptureMode.SingleFrame) {
                Color prevColor = GUI.color;
                // we wait 0.1s for the quilt setting up to be active
                using (new EditorGUI.DisabledScope(quiltCapture.State != QuiltCaptureState.NotRecording)) {
                    GUI.color = BlueStartColor;
                    if (GUILayout.Button("Screenshot")) {
                        _ = quiltCapture.Screenshot3D();
                    }
                }

                GUI.color = prevColor;
            }

            //Let's make sure to save here, in case we used any non-SerializedProperty Editor GUI:
            if (serializedObject.ApplyModifiedProperties())
                EditorUtility.SetDirty(quiltCapture);
        }

        private void RecordingSettingsGUI(QuiltRecordingSettings recordingSettings) {
            EditorGUILayout.LabelField("Codec: ", recordingSettings.codec.ToString());
            EditorGUILayout.LabelField("FrameRate: ", recordingSettings.frameRate.ToString());
            EditorGUILayout.LabelField("Target Bitrate In Megabits: ", recordingSettings.targetBitrateInMegabits.ToString());
            EditorGUILayout.LabelField("Compression: ", recordingSettings.compression.ToString());
            EditorGUILayout.Space();

            OverrideSettingsGUI(recordingSettings.cameraOverrideSettings);
        }

        private void OverrideSettingsGUI(QuiltCaptureOverrideSettings overrideSettings) {
            EditorGUILayoutHelper.ReadOnlyQuiltSettingsGUI(overrideSettings.quiltSettings);
            EditorGUILayout.LabelField("NearClipFactor: ", overrideSettings.nearClipFactor.ToString());
        }

        private bool CustomGUI(SerializedProperty property) {
            switch (property.name) {
                case nameof(QuiltCapture.customRecordingSettings):
                    if (quiltCapture.captureMode != QuiltCaptureMode.SingleFrame) {
                        if (quiltCapture.recordingPreset == QuiltRecordingPreset.Custom)
                            EditorGUILayout.PropertyField(property);
                        else {
                            isPresetSettingsExpanded = EditorGUILayout.Foldout(isPresetSettingsExpanded, "Recording Settings");
                            if (isPresetSettingsExpanded) {
                                EditorGUI.indentLevel++;
                                RecordingSettingsGUI(quiltCapture.RecordingSettings);
                                EditorGUI.indentLevel--;
                            }

                            if (updateCustomSettings) {
                                // set custom setting to the chosen preset
                                Assert.IsTrue(quiltCapture.customRecordingSettings.GetType().IsValueType, "The code below assumes that using the equals operator performs a deep copy (value types only).");
                                quiltCapture.customRecordingSettings = quiltCapture.RecordingSettings;
                                updateCustomSettings = false;
                            }
                        }
                    }
                    return true;
                case nameof(QuiltCapture.customScreenshotSettings):
                    if (quiltCapture.captureMode == QuiltCaptureMode.SingleFrame) {
                        if (quiltCapture.screenshotPreset == QuiltScreenshotPreset.Custom) {
                            EditorGUILayout.PropertyField(property, true);
                        } else {
                            isPresetSettingsExpanded = EditorGUILayout.Foldout(isPresetSettingsExpanded, "Screenshot Settings");
                            if (isPresetSettingsExpanded) {
                                EditorGUI.indentLevel++;
                                OverrideSettingsGUI(quiltCapture.OverrideSettings);
                                EditorGUI.indentLevel--;
                            }
                            if (updateCustomSettings) {
                                // set custom setting to the chosen preset
                                Assert.IsTrue(quiltCapture.customScreenshotSettings.GetType().IsValueType, "The code below assumes that using the equals operator performs a deep copy (value types only).");
                                quiltCapture.CustomScreenshotSettings = quiltCapture.ScreenshotSettings;
                                updateCustomSettings = false;
                            }
                        }
                    }
                    return true;
                case nameof(QuiltCapture.recordingPreset):
                    if (quiltCapture.captureMode != QuiltCaptureMode.SingleFrame) {
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(property);
                        if (EditorGUI.EndChangeCheck()) {
                            // The editor update has delay so we always update custom settings whenever preset is changed
                            updateCustomSettings = true;
                        }
                    }
                    return true;
                case nameof(QuiltCapture.screenshotPreset):
                    if (quiltCapture.captureMode == QuiltCaptureMode.SingleFrame) {
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(property);
                        if (EditorGUI.EndChangeCheck()) {
                            // Quilt need to be set up with new quilt settings when preset is changed under single frame mode
                            updateCustomSettings = true;
                        }
                    }
                    return true;
                case nameof(QuiltCapture.fileName):
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(property);
                    if (EditorGUI.EndChangeCheck()) {
                        //The file name needs to be santinized, otherwise if it is invalid, it won't be previewed correctly
                        property.stringValue = PathUtil.SanitizeFileName(property.stringValue);
                    }
                    return true;
                case nameof(QuiltCapture.syncedVideoPlayer):
                    return true;
                case nameof(QuiltCapture.folderPath):
                    try {
                        EditorGUILayout.PropertyField(property, true);
                    } catch (InvalidOperationException) {
                        //UNITY BUG
                    }

                    int indent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 0;
                    string path = quiltCapture.CalculateAutoCorrectPath();

                    GUIStyle s_PathPreviewStyle = new GUIStyle(GUI.skin.label) {
                        wordWrap = true,
                        stretchHeight = true,
                        stretchWidth = true,
                        padding = new RectOffset(padding, 0, 0, 0),
                        clipping = TextClipping.Overflow
                    };

                    EditorGUILayout.BeginHorizontal();
                    Rect r = GUILayoutUtility.GetRect(new GUIContent(path), s_PathPreviewStyle,
                        GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
                    EditorGUI.SelectableLabel(r, path, s_PathPreviewStyle);

                    GUIStyle openPathButtonStyle = new GUIStyle("minibutton") { fixedWidth = 30 };
                    if (GUILayout.Button(new GUIContent(openPathIcon, "Open the output location in your file browser"),
                    openPathButtonStyle)) {
                        TryOpenInFileBrowser(path);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.indentLevel = indent;
                    return true;
                case nameof(QuiltCapture.captureMode):
                    EditorGUI.BeginChangeCheck();
                    int takeNumber = EditorGUILayout.DelayedIntField(new GUIContent("Take Number", QuiltCapture.TakeNumberTooltip), quiltCapture.TakeNumber);
                    if (EditorGUI.EndChangeCheck()) {
                        quiltCapture.TakeNumber = takeNumber;
                    }

                    EditorGUILayout.PropertyField(captureModeProperty, Styles.CaptureModeLabel);
                    EditorGUI.indentLevel++;
                    switch (quiltCapture.captureMode) {
                        case QuiltCaptureMode.Manual: {
                                EditorGUILayout.PropertyField(captureOnStartProperty);
                                EditorGUILayout.PropertyField(exitPlayModeOnStopProperty);
                                break;
                            }
                        case QuiltCaptureMode.FrameInterval: {
                                int[] outputDimensions = new int[2];
                                outputDimensions[0] = startFrameProperty.intValue;
                                outputDimensions[1] = endFrameProperty.intValue;

                                if (EditorGUILayoutHelper.MultiIntField(GUIContent.none, new[] { Styles.StartLabel, Styles.EndLabel },
                                    outputDimensions)) {
                                    startFrameProperty.intValue = Mathf.Max(outputDimensions[0], 0);
                                    endFrameProperty.intValue = Mathf.Max(outputDimensions[1], startFrameProperty.intValue);
                                }
                                break;
                            }
                        case QuiltCaptureMode.TimeInterval: {
                                float[] outputDimensions = new float[2];
                                outputDimensions[0] = startTimeProperty.floatValue;
                                outputDimensions[1] = endTimeProperty.floatValue;

                                if (EditorGUILayoutHelper.MultiFloatField(GUIContent.none, new[] { Styles.StartLabel, Styles.EndLabel },
                                    outputDimensions)) {
                                    startTimeProperty.floatValue = Mathf.Max(outputDimensions[0], 0);
                                    endTimeProperty.floatValue = Mathf.Max(outputDimensions[1], startTimeProperty.floatValue);
                                }
                                break;
                            }
                        case QuiltCaptureMode.ClipLength: {
                                EditorGUILayout.PropertyField(syncedVideoClipProperty);
                                break;
                            }
                    }
                    EditorGUI.indentLevel--;
                    return true;
            }
            return false;
        }
    }
}
#endif
