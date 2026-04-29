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
using UnityEditor;

namespace TiltFive
{
    public class GlobalSettingsDrawer
    {
        static bool globalSettingsVisible = true;

        private enum SelectedTab
        {
            SpectatingSettings,
            GizmoSettings,
            TrackingSettings,
            LoggingSettings,
            GraphicsSettings    // TODO: Discuss naming, think about other settings that might fit here besides the 60fps lock toggle
        }

        static SelectedTab selectedTab = SelectedTab.SpectatingSettings;
        private static bool spectatingSettingsVisible => selectedTab == SelectedTab.SpectatingSettings;
        private static bool gizmoSettingsVisible => selectedTab == SelectedTab.GizmoSettings;
        private static bool trackingSettingsVisible => selectedTab == SelectedTab.TrackingSettings;
        private static bool loggingSettingsVisible => selectedTab == SelectedTab.LoggingSettings;
        private static bool graphicsSettingsVisible => selectedTab == SelectedTab.GraphicsSettings;

        public static void Draw(List<SerializedProperty> playerSettingsProperties, 
            SerializedProperty spectatorSettingsProperty, SerializedProperty logSettingsProperty, SerializedProperty graphicsSettingsProperty)
        {
            if (playerSettingsProperties == null || playerSettingsProperties.Count == 0)
            {
                return;
            }

            var boldFoldoutStyle = EditorStyles.foldout;
            boldFoldoutStyle.fontStyle = FontStyle.Bold;
            boldFoldoutStyle.active = boldFoldoutStyle.normal;
            boldFoldoutStyle.focused = boldFoldoutStyle.normal;

            globalSettingsVisible = EditorGUILayout.Foldout(globalSettingsVisible, "Global Settings", true, boldFoldoutStyle);

            if(!globalSettingsVisible)
            {
                return;
            }

            int padding = 5;
            var playerSettingsBackgroundStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(padding, padding, padding, padding),
                margin = new RectOffset(padding, padding, padding, padding)
            };

            GUILayout.Space(8);

            var firstPlayerSettingsProperty = playerSettingsProperties[0];

            int mirrorMode = 0, gameboardTypeOverride = 0, glassesTrackingFailureMode = 0, wandTrackingFailureMode = 0;
            bool usePreviewPose = true;
            Transform previewPose = null;

            bool spectatorCameraAssigned = spectatorSettingsProperty.FindPropertyRelative("spectatorCamera").objectReferenceValue;

            var settingsLabels = new GUIContent[5]
            {
                spectatorCameraAssigned ? new GUIContent("Spectating")
                    : new GUIContent(EditorGUIUtility.IconContent("console.warnicon.sml"))
                    { text = " Spectating", tooltip = "No spectator camera assigned." },
                new GUIContent("Gizmo"),
                new GUIContent("Tracking"),
                new GUIContent("Logging"),
                new GUIContent("Graphics")
            };

            using (new EditorGUILayout.VerticalScope(playerSettingsBackgroundStyle))
            {
                selectedTab = (SelectedTab)GUILayout.SelectionGrid((int)selectedTab, settingsLabels, 2);
                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(4));
                GUILayout.Space(8);

                // Spectating Settings
                if (spectatingSettingsVisible)
                {
                    var spectatorCameraProperty = spectatorSettingsProperty.FindPropertyRelative("spectatorCamera");
                    var mirrorModeProperty = spectatorSettingsProperty.FindPropertyRelative("glassesMirrorMode");
                    var spectatedPlayerProperty = spectatorSettingsProperty.FindPropertyRelative("spectatedPlayer");

                    // Draw spectatorCamera field
                    bool hasCamera = spectatorCameraProperty.objectReferenceValue;
                    if(!hasCamera)
                    {
                        EditorGUILayout.HelpBox("An active Spectator Camera assignment is required.", MessageType.Warning);
                    }
                    EditorGUILayout.PropertyField(spectatorCameraProperty, new GUIContent("Spectator Camera", "The Camera driven by the glasses head tracking system."));

                    // Draw mirror mode field
                    mirrorMode = EditorGUILayout.Popup(
                        new GUIContent("Mirror Mode",
                            "The perspective that will be mirrored to the onscreen preview. " +
                            "'None' results in a smoothed camera view that fits the screen resolution / aspect ratio, " +
                            "which is recommended for release builds."),
                        mirrorModeProperty.enumValueIndex, mirrorModeProperty.enumDisplayNames);
                    mirrorModeProperty.enumValueIndex = mirrorMode;

                    // Enumerate player selection dropdown display names
                    var supportedPlayers = spectatedPlayerProperty.serializedObject.FindProperty("supportedPlayerCount").intValue;
                    var enumDisplayNames = spectatedPlayerProperty.enumDisplayNames;

                    // Trim any player indices above the number of supported players.
                    System.Array.Resize<string>(ref enumDisplayNames, supportedPlayers + 1);

                    // Draw player selection dropdown
                    spectatedPlayerProperty.enumValueIndex = EditorGUILayout.Popup(
                        new GUIContent("Spectated Player", "The player that will be followed by the spectator camera."),
                        spectatedPlayerProperty.enumValueIndex,
                        enumDisplayNames);

                    GUILayout.Space(4);
                    GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(4));
                    GUILayout.Space(4);

                    DrawSpectatorSettings(spectatorSettingsProperty);
                }

                // Gizmo Settings
                if (gizmoSettingsVisible)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        // Gameboard
                        //  - Draw Gameboard Gizmo Override
                        var displayedGameboardTypeOverrideProperty =
                            firstPlayerSettingsProperty.FindPropertyRelative("gameboardSettings").FindPropertyRelative("gameboardTypeOverride");
                        gameboardTypeOverride =
                            EditorGUILayout.Popup(new GUIContent("Gizmo Override", "Forces the gameboard gizmo to reflect the selected gameboard configuration." +
                            System.Environment.NewLine + System.Environment.NewLine +
                            "If GameboardType_None is selected, the gizmo automatically reflects the gameboard configuration reported by the Tilt Five plugin."),
                            displayedGameboardTypeOverrideProperty.enumValueIndex, displayedGameboardTypeOverrideProperty.enumDisplayNames);
                    }
                }

                // Tracking Settings
                if (trackingSettingsVisible)
                {
                    EditorGUILayout.LabelField("Tracking Failure Mode");

                    using (new EditorGUI.IndentLevelScope())
                    {
                        // Glasses - draw Tracking Failure Mode
                        var glassesSettingsProperty = firstPlayerSettingsProperty.FindPropertyRelative("glassesSettings");
                        var displayedGlassesTrackingFailureModeProperty = glassesSettingsProperty.FindPropertyRelative("FailureMode");
                        glassesTrackingFailureMode = EditorGUILayout.Popup("Glasses",
                            displayedGlassesTrackingFailureModeProperty.enumValueIndex,
                            displayedGlassesTrackingFailureModeProperty.enumDisplayNames);

                        if ((TrackableSettings.TrackingFailureMode)displayedGlassesTrackingFailureModeProperty.enumValueIndex == TrackableSettings.TrackingFailureMode.SnapToDefault)
                        {
                            var usePreviewPoseProperty = glassesSettingsProperty.FindPropertyRelative("usePreviewPose");
                            var previewPoseProperty = glassesSettingsProperty.FindPropertyRelative("previewPose");

                            usePreviewPose = EditorGUILayout.Toggle(
                                new GUIContent("Use Preview Pose",
                                    "If enabled, the head pose camera pose will be set to match " +
                                    "that of the Preview Pose GameObject if the glasses are no longer looking at the gameboard." +
                                    System.Environment.NewLine +
                                    "If disabled, it is up to the developer to set the head pose position until head tracking resumes." +
                                    "It is also up to the developer to stop driving the head pose position once head tracking resumes."),
                                usePreviewPoseProperty.boolValue);

                            if(usePreviewPose)
                            {
                                if (!previewPoseProperty.objectReferenceValue)
                                {
                                    EditorGUILayout.HelpBox("No Transform assigned to Preview Pose." +
                                    System.Environment.NewLine + System.Environment.NewLine +
                                    "If the \"Use Preview Pose\" flag is enabled, but no Transform is assigned, " +
                                    "the head pose camera will not be updated at all if the glasses lose tracking.", MessageType.Warning);
                                }
                                EditorGUILayout.PropertyField(previewPoseProperty, new GUIContent("Preview Pose",
                                    "A reference pose for the head pose camera to use while the user is looking away from the gameboard."));
                                previewPose = previewPoseProperty.objectReferenceValue as Transform;
                            }
                        }

                        // Wands - draw Tracking Failure Mode
                        var displayedWandTrackingFailureModeProperty = firstPlayerSettingsProperty.FindPropertyRelative("rightWandSettings").FindPropertyRelative("FailureMode");
                        wandTrackingFailureMode = EditorGUILayout.Popup(
                                "Wand",
                                displayedWandTrackingFailureModeProperty.enumValueIndex,
                                displayedWandTrackingFailureModeProperty.enumDisplayNames);
                    }
                }

                // Logging Settings
                if (loggingSettingsVisible)
                {
                    // Logging
                    //  - Draw Logging Settings
                    LogSettingsDrawer.Draw(logSettingsProperty);
                }

                // Bonus: Draw labels for connected players, glasses, wands

                if(graphicsSettingsVisible)
                {
                    GraphicsSettingsDrawer.Draw(graphicsSettingsProperty);
                }

                // Since we don't actually have a standalone "GlobalSettings" class, and since moving all these settings into one
                // would be an API break, instead we have to reach down into the various settings classes and set them all at once.
                for (int i = 0; i < playerSettingsProperties.Count; i++)
                {
                    var playerSettingsProperty = playerSettingsProperties[i];

                    if (gizmoSettingsVisible)
                    {
                        var gameboardTypeOverrideProperty =
                            playerSettingsProperty.FindPropertyRelative("gameboardSettings").FindPropertyRelative("gameboardTypeOverride");
                        gameboardTypeOverrideProperty.enumValueIndex = gameboardTypeOverride;
                    }

                    if (spectatingSettingsVisible)
                    {
                        var glassesMirrorModeProperty =
                            playerSettingsProperty.FindPropertyRelative("glassesSettings").FindPropertyRelative("glassesMirrorMode");
                        glassesMirrorModeProperty.enumValueIndex = mirrorMode;
                    }

                    if (trackingSettingsVisible)
                    {
                        var glassesSettingsProperty = playerSettingsProperty.FindPropertyRelative("glassesSettings");
                        var glassesTrackingFailureModeProperty =
                            glassesSettingsProperty.FindPropertyRelative("FailureMode");
                        glassesTrackingFailureModeProperty.enumValueIndex = glassesTrackingFailureMode;

                        var useGlassesPreviewPoseProperty =
                            glassesSettingsProperty.FindPropertyRelative("usePreviewPose");
                        useGlassesPreviewPoseProperty.boolValue = usePreviewPose;

                        var glassesPreviewPoseProperty =
                            glassesSettingsProperty.FindPropertyRelative("previewPose");
                        glassesPreviewPoseProperty.objectReferenceValue = previewPose;

                        // Wands
                        var rightWandTrackingFailureModeProperty =
                            playerSettingsProperty.FindPropertyRelative("rightWandSettings").FindPropertyRelative("FailureMode");
                        var currentLeftWandTrackingFailureModeProperty =
                            playerSettingsProperty.FindPropertyRelative("leftWandSettings").FindPropertyRelative("FailureMode");
                        rightWandTrackingFailureModeProperty.enumValueIndex = currentLeftWandTrackingFailureModeProperty.enumValueIndex = wandTrackingFailureMode;
                    }
                }
            }
            GUILayout.Space(8);
        }

        public static void DrawSpectatorSettings(SerializedProperty spectatorSettingsProperty)
        {
            var cullingMaskProperty = spectatorSettingsProperty.FindPropertyRelative("cullingMask");
            var fieldOfViewProperty = spectatorSettingsProperty.FindPropertyRelative("fieldOfView");
            var nearClipPlaneProperty = spectatorSettingsProperty.FindPropertyRelative("nearClipPlane");
            var farClipPlaneProperty = spectatorSettingsProperty.FindPropertyRelative("farClipPlane");
            var viewportRectProperty = spectatorSettingsProperty.FindPropertyRelative("rect");
            var targetTextureProperty = spectatorSettingsProperty.FindPropertyRelative("targetTexture");

            // Draw culling mask field
            EditorGUILayout.PropertyField(
                cullingMaskProperty,
                new GUIContent("Culling Mask",
                    "The culling mask to be used by the spectator camera while it isn't being overwritten by the eye camera(s)."));

            // Draw FOV field
            fieldOfViewProperty.floatValue = EditorGUILayout.Slider(
                new GUIContent("Field of View",
                    "The field of view to be used by the spectator camera while it isn't being overwritten by the eye camera(s)."),
                fieldOfViewProperty.floatValue, float.Epsilon, 179f);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(new GUIContent("Clipping Planes", "The clipping planes to be used by the spectator camera while it isn't being overwritten by the eye camera(s)."));
                using (new EditorGUILayout.VerticalScope())
                {
                    var oldLabelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 35;

                    // Draw near clip plane field
                    nearClipPlaneProperty.floatValue = Mathf.Clamp(
                    EditorGUILayout.FloatField(
                        new GUIContent("Near",
                            "The near clip plane to be used by the spectator camera while it isn't being overwritten by the eye camera(s)."),
                        nearClipPlaneProperty.floatValue),
                    0.1f, float.MaxValue);

                    // Draw far clip plane field
                    farClipPlaneProperty.floatValue = Mathf.Clamp(
                    EditorGUILayout.FloatField(
                        new GUIContent("Far",
                            "The far clip plane to be used by the spectator camera while it isn't being overwritten by the eye camera(s)."),
                        farClipPlaneProperty.floatValue),
                    0.1f, float.MaxValue);

                    EditorGUIUtility.labelWidth = oldLabelWidth;
                }
            }
            // Draw viewport rect field
            EditorGUILayout.PropertyField(
                viewportRectProperty,
                new GUIContent("Viewport Rect",
                    "The viewport rect to be used by the spectator camera while it isn't being overwritten by the eye camera(s)."));

            // Draw the target texture field
            EditorGUILayout.PropertyField(
                targetTextureProperty,
                new GUIContent("Target Texture",
                    "The target texture to be used by the spectator camera while it isn't being overwritten by the eye camera(s)."));
        }
    }
}
