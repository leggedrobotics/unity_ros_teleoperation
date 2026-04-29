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
using UnityEngine;
using UnityEditor;


namespace TiltFive
{
    [CustomEditor(typeof(TiltFiveManager))]
    public class TiltFiveManagerEditor : Editor
    {
        #region Properties

        SerializedProperty glassesSettingsProperty,
            scaleSettingsProperty,
            gameBoardSettingsProperty,
            rightWandSettingsProperty,
            leftWandSettingsProperty,
            logSettingsProperty,
            graphicsSettingsProperty,
            editorSettingsProperty,
            spectatorSettingsProperty;

        #endregion


        #region Unity Functions

        public void OnEnable()
        {
            glassesSettingsProperty = serializedObject.FindProperty("glassesSettings");
            scaleSettingsProperty = serializedObject.FindProperty("scaleSettings");
            gameBoardSettingsProperty = serializedObject.FindProperty("gameBoardSettings");
            // TODO: update me once the members in TiltFiveManager are changed
            rightWandSettingsProperty = serializedObject.FindProperty("primaryWandSettings");
            leftWandSettingsProperty = serializedObject.FindProperty("secondaryWandSettings");
            logSettingsProperty = serializedObject.FindProperty("logSettings");
            graphicsSettingsProperty = serializedObject.FindProperty("graphicsSettings");
            editorSettingsProperty = serializedObject.FindProperty("editorSettings");
            spectatorSettingsProperty = serializedObject.FindProperty("spectatorSettings");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            DrawActivePanel();

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        #endregion


        #region Panel Drawing

        private void DrawActivePanel()
        {
            var activePanelProperty = editorSettingsProperty.FindPropertyRelative("activePanel");
            var warningStyle = EditorGUIUtility.IconContent("console.warnicon.sml");

            var glassesLabel = glassesSettingsProperty.FindPropertyRelative("headPoseCamera").objectReferenceValue == null
                ? new GUIContent(warningStyle) { text = " Glasses", tooltip = "No head pose camera assigned." }
                : new GUIContent("Glasses");

            var gameBoardLabel = gameBoardSettingsProperty.FindPropertyRelative("currentGameBoard").objectReferenceValue == null
                ? new GUIContent(warningStyle) { text = " Gameboard", tooltip = "No gameboard assigned." }
                : new GUIContent("Gameboard");

            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            DrawButton(activePanelProperty, EditorSettings.PanelView.GlassesConfig, glassesLabel);
            DrawButton(activePanelProperty, EditorSettings.PanelView.GameBoardConfig, gameBoardLabel);
            DrawButton(activePanelProperty, EditorSettings.PanelView.WandConfig, "Wand");
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();
            DrawButton(activePanelProperty, EditorSettings.PanelView.ScaleConfig, "Content Scale");
            DrawButton(activePanelProperty, EditorSettings.PanelView.LogConfig, "Logging");
            DrawButton(activePanelProperty, EditorSettings.PanelView.SpectatorConfig, "Spectator");
            EditorGUILayout.EndHorizontal();

            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(4));
            GUILayout.Space(8);

            var activePanel = (EditorSettings.PanelView)activePanelProperty.enumValueIndex;

            switch (activePanel)
            {
                case EditorSettings.PanelView.ScaleConfig:
                    DrawScaleSettings();
                    break;
                case EditorSettings.PanelView.GameBoardConfig:
                    DrawGameBoardSettings();
                    break;
                case EditorSettings.PanelView.WandConfig:
                    DrawWandSettings();
                    break;
                case EditorSettings.PanelView.LogConfig:
                    DrawLogSettings();
                    break;
                case EditorSettings.PanelView.SpectatorConfig:
                    DrawSpectatorSettings();
                    break;
                default:    // GlassesConfig
                    DrawGlassesSettings();
                    break;
            }

            GUILayout.Space(5);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(4));

            bool upgradeButtonPressed = GUILayout.Button(new GUIContent("Upgrade to TiltFiveManager2",
                "Creates a new TiltFiveManager2 component and copies all of the component values from the old TiltFiveManager. " +
                System.Environment.NewLine + System.Environment.NewLine +
                "If a TiltFiveManager2 component already exists on the parent GameObject, developers are given the option " +
                "to copy the TiltFiveManager component values and overwrite the component values on the existing TiltFiveManager2."));
            if(upgradeButtonPressed && serializedObject.targetObject is TiltFiveManager tiltFiveManager)
            {
                TiltFiveManager2.CreateFromTiltFiveManager(tiltFiveManager);
            }
        }

        private void DrawButton(SerializedProperty activePanelProperty, EditorSettings.PanelView panel, GUIContent label)
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.margin = new RectOffset(0, 0, 2, 2);
            buttonStyle.padding = new RectOffset(0, 0, 2, 2);
            buttonStyle.fixedHeight = 22f;
            buttonStyle.border = new RectOffset(
                2, //buttonStyle.border.left,
                2, //buttonStyle.border.right,
                2,//buttonStyle.border.top,
                2);//buttonStyle.border.bottom);

            var activePanel = (EditorSettings.PanelView)activePanelProperty.enumValueIndex;

            if (GUILayout.Toggle(activePanel == panel, label, buttonStyle))
            {
                activePanelProperty.enumValueIndex = (int)panel;
            }
        }

        private void DrawButton(SerializedProperty activePanelProperty, EditorSettings.PanelView panel, string label)
        {
            DrawButton(activePanelProperty, panel, new GUIContent(label));
        }

        #endregion


        #region Glasses Settings

        private void DrawGlassesSettings()
        {
            GlassesSettingsDrawer.DrawSingleplayer(glassesSettingsProperty, graphicsSettingsProperty);
        }

        #endregion


        #region Scale Settings

        private void DrawScaleSettings()
        {
            ScaleSettingsDrawer.Draw(scaleSettingsProperty);
        }

        #endregion


        #region Game Board Settings

        private void DrawGameBoardSettings()
        {
            GameBoardSettingsDrawer.Draw(gameBoardSettingsProperty);
        }

        #endregion


        #region Wand Settings

        private void DrawWandSettings()
        {
            // Right Wand
            DrawRightWandSettings();

            EditorGUILayout.Space();

            // Left Wand
            DrawLeftWandSettings();

            EditorGUILayout.Space();

            // Tracking failure mode
            DrawTrackingFailureMode();
        }

        private void DrawRightWandSettings()
        {
            var controllerIndex = rightWandSettingsProperty.FindPropertyRelative("controllerIndex");
            WandSettingsDrawer.Draw(rightWandSettingsProperty, ControllerIndex.Right);
        }

        private void DrawLeftWandSettings()
        {
            var controllerIndex = leftWandSettingsProperty.FindPropertyRelative("controllerIndex");
            WandSettingsDrawer.Draw(leftWandSettingsProperty, ControllerIndex.Left);
        }

        private void DrawTrackingFailureMode()
        {
            var rightWandTrackingFailureModeProperty = rightWandSettingsProperty.FindPropertyRelative("FailureMode");
            var leftWandTrackingFailureModeProperty = leftWandSettingsProperty.FindPropertyRelative("FailureMode");
            leftWandTrackingFailureModeProperty.enumValueIndex = rightWandTrackingFailureModeProperty.enumValueIndex =
                EditorGUILayout.Popup(
                    "Wand Tracking Failure Mode",
                    rightWandTrackingFailureModeProperty.enumValueIndex,
                    rightWandTrackingFailureModeProperty.enumDisplayNames);
        }

        #endregion


        #region Log Settings

        private void DrawLogSettings()
        {
            LogSettingsDrawer.Draw(logSettingsProperty);
        }

        #endregion

        #region Spectator Settings

        private void DrawSpectatorSettings()
        {
            GlobalSettingsDrawer.DrawSpectatorSettings(spectatorSettingsProperty);
        }

        #endregion
    }
}
