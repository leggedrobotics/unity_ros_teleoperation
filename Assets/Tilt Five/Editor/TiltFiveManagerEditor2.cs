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
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace TiltFive
{
    [CustomEditor(typeof(TiltFiveManager2))]
    public class TiltFiveManagerEditor2 : Editor
    {
        #region Properties

        SerializedProperty allPlayerSettingsProperty,
            playerOneSettingsProperty,
            playerTwoSettingsProperty,
            playerThreeSettingsProperty,
            playerFourSettingsProperty,
            spectatorSettingsProperty,
            logSettingsProperty,
            graphicsSettingsProperty,
            editorSettingsProperty;

        SerializedProperty glassesSettingsProperty,
            scaleSettingsProperty,
            gameboardSettingsProperty,
            leftWandSettingsProperty,
            rightWandSettingsProperty;

        //string[] playerIndexDisplayNames;
        System.Array playerIndices;
        PlayerIndex previousSelectedPlayer = PlayerIndex.None;
        List<SerializedProperty> playerProperties = new List<SerializedProperty>();

        #endregion


        #region Constants

        public string WARNING_NO_GAMEBOARD_ASSIGNED = "No gameboard assigned.";

        #endregion


        #region Private Fields

        private static bool playerSettingsVisible = true;

        #endregion


        #region Unity Functions

        public void OnEnable()
        {
            allPlayerSettingsProperty = serializedObject.FindProperty("allPlayerSettings");
            playerOneSettingsProperty = allPlayerSettingsProperty.GetArrayElementAtIndex(0);
            playerTwoSettingsProperty = allPlayerSettingsProperty.GetArrayElementAtIndex(1);
            playerThreeSettingsProperty = allPlayerSettingsProperty.GetArrayElementAtIndex(2);
            playerFourSettingsProperty = allPlayerSettingsProperty.GetArrayElementAtIndex(3);
            spectatorSettingsProperty = serializedObject.FindProperty("spectatorSettings");
            logSettingsProperty = serializedObject.FindProperty("logSettings");
            graphicsSettingsProperty = serializedObject.FindProperty("graphicsSettings");
            editorSettingsProperty = serializedObject.FindProperty("editorSettings");

            playerIndices = System.Enum.GetValues(typeof(PlayerIndex));
            var selectedPlayerProperty = editorSettingsProperty.FindPropertyRelative("selectedPlayer");
            var selectedPlayer = (PlayerIndex)selectedPlayerProperty.enumValueIndex;
            if (selectedPlayer == PlayerIndex.None)
            {
                selectedPlayer = PlayerIndex.One;
                EditorGUI.BeginChangeCheck();
                selectedPlayerProperty.enumValueIndex = (int) selectedPlayer;
                serializedObject.ApplyModifiedProperties();
            }

            ResetPlayerSubsettings(selectedPlayer);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            DrawSelectedPlayer();

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        #endregion


        #region Panel Drawing

        private void DrawSelectedPlayer()
        {
            var selectedPlayerProperty = editorSettingsProperty.FindPropertyRelative("selectedPlayer");
            playerProperties.Clear();

            foreach (PlayerIndex playerIndex in playerIndices)
            {
                if(playerIndex == PlayerIndex.None)
                {
                    continue;
                }
                var playerSettingsProperty = GetPlayerSettings(playerIndex);
                if(playerSettingsProperty != null)
                {
                    playerProperties.Add(playerSettingsProperty);
                }
            }

            var boldFoldoutStyle = EditorStyles.foldout;
            boldFoldoutStyle.fontStyle = FontStyle.Bold;
            boldFoldoutStyle.active = boldFoldoutStyle.normal;
            boldFoldoutStyle.focused = boldFoldoutStyle.normal;

            GUILayout.Space(8);

            playerSettingsVisible = EditorGUILayout.Foldout(playerSettingsVisible, "Per-Player Settings", true, boldFoldoutStyle);

            if(!playerSettingsVisible)
            {
                GlobalSettingsDrawer.Draw(playerProperties, spectatorSettingsProperty, logSettingsProperty, graphicsSettingsProperty);
                return;
            }

            GUILayout.Space(8);

            // If the developer switches between player tabs, set the various subsettings to the new player's settings
            var selectedPlayer = (PlayerIndex)selectedPlayerProperty.enumValueIndex;
            var supportedPlayerCountProperty = serializedObject.FindProperty("supportedPlayerCount");

            GUIContent[] playerTabLabels = new GUIContent[supportedPlayerCountProperty.intValue];

            foreach (PlayerIndex playerIndex in playerIndices)
            {
                if (playerIndex == PlayerIndex.None || (int) playerIndex > supportedPlayerCountProperty.intValue)
                {
                    continue;
                }

                var playerSettingsProperty = GetPlayerSettings(playerIndex);
                if (playerSettingsProperty != null)
                {
                    bool attentionNeeded = false;
                    string tooltip = $"Settings for Player {System.Enum.GetName(typeof(PlayerIndex), playerIndex)}";
                    string warningFormat = System.Environment.NewLine + "    - {0}";

                    if (playerSettingsProperty.FindPropertyRelative("gameboardSettings").FindPropertyRelative("currentGameBoard").objectReferenceValue == null)
                    {
                        attentionNeeded = true;
                        tooltip += string.Format(warningFormat, WARNING_NO_GAMEBOARD_ASSIGNED);
                    }
                    var warningStyle = EditorGUIUtility.IconContent("console.warnicon.sml");
                    var labelText = $"{(int)playerIndex}";
                    playerTabLabels[(int)playerIndex - 1] = attentionNeeded
                        ? new GUIContent(warningStyle) { text = " " + labelText, tooltip = tooltip }
                        : new GUIContent(labelText, tooltip);
                }
            }

            EditorGUILayout.LabelField("Player", GUILayout.Width(40));
            using (new GUILayout.HorizontalScope())
            {
                // Use offsets of +/- 1 to account for PlayerIndex.None preceding One, Two, etc.
                selectedPlayerProperty.enumValueIndex =
                    GUILayout.Toolbar(selectedPlayerProperty.enumValueIndex - 1, playerTabLabels, GUILayout.ExpandWidth(true), GUILayout.Height(25)) + 1;

                var plusButtonContent = EditorGUIUtility.IconContent("d_Toolbar Plus", "|Increase the number of players");
                var minusButtonContent = EditorGUIUtility.IconContent("d_Toolbar Minus", "|Decrease the number of players");
                var buttonWidth = 25;
                var buttonHeight = 25;
                var plusButtonStyle = new GUIStyle(GUI.skin.button) { margin = new RectOffset(0, -1, 2, 0), fixedWidth = buttonWidth, fixedHeight = buttonHeight };
                var minusButtonStyle = new GUIStyle(GUI.skin.button) { margin = new RectOffset(-1, 0, 2, 0), fixedWidth = buttonWidth, fixedHeight = buttonHeight };

                using (new EditorGUI.DisabledGroupScope(supportedPlayerCountProperty.intValue > 3))
                {
                    if (GUILayout.Button(plusButtonContent, plusButtonStyle))
                    {
                        supportedPlayerCountProperty.intValue++;
                    }
                }

                using (new EditorGUI.DisabledGroupScope(supportedPlayerCountProperty.intValue < 2))
                {
                    if (GUILayout.Button(minusButtonContent, minusButtonStyle))
                    {
                        supportedPlayerCountProperty.intValue--;
                    }
                }
            }

            if (previousSelectedPlayer != selectedPlayer && selectedPlayer != PlayerIndex.None)
            {
                ResetPlayerSubsettings(selectedPlayer);
                previousSelectedPlayer = selectedPlayer;
            }

            DrawActivePanel();

            GUILayout.Space(8);

            // Draw Global Settings
            GlobalSettingsDrawer.Draw(playerProperties, spectatorSettingsProperty, logSettingsProperty, graphicsSettingsProperty);
        }

        private SerializedProperty GetPlayerSettings(PlayerIndex playerIndex)
        {
            switch (playerIndex)
            {
                case PlayerIndex.One:
                    return playerOneSettingsProperty;
                case PlayerIndex.Two:
                    return playerTwoSettingsProperty;
                case PlayerIndex.Three:
                    return playerThreeSettingsProperty;
                case PlayerIndex.Four:
                    return playerFourSettingsProperty;
                case PlayerIndex.None:
                    // Fall through to the default case
                default:
                    return null;
            }
        }

        private void DrawActivePanel()
        {
            int padding = 5;
            var playerSettingsBackgroundStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(padding, padding, padding, padding),
                margin = new RectOffset(padding, padding, padding, 0)
            };

            using (var verticalScope = new EditorGUILayout.VerticalScope(playerSettingsBackgroundStyle))
            {
                var selectedPlayerSettingsPanelProperty = editorSettingsProperty.FindPropertyRelative("selectedPlayerSettingsPanel");
                var activePanel = (EditorSettings2.PlayerSettingsPanel)selectedPlayerSettingsPanelProperty.enumValueIndex;

                var warningIconContent = EditorGUIUtility.IconContent("console.warnicon.sml");

                var glassesLabel = new GUIContent("Glasses");

                var gameBoardLabel = gameboardSettingsProperty.FindPropertyRelative("currentGameBoard").objectReferenceValue == null
                    ? new GUIContent(warningIconContent) { text = " Gameboard", tooltip = WARNING_NO_GAMEBOARD_ASSIGNED }
                    : new GUIContent("Gameboard");

                // Draw a 2x2 grid of buttons for the Glasses, Scale, Wand, and Gameboard tabs.
                selectedPlayerSettingsPanelProperty.enumValueIndex =
                    GUILayout.SelectionGrid(selectedPlayerSettingsPanelProperty.enumValueIndex, new GUIContent[]
                    {
                        glassesLabel,
                        new GUIContent("Content Scale"),
                        new GUIContent("Wand"),
                        gameBoardLabel
                    },
                    2,  // 2 columns
                    new GUIStyle(GUI.skin.button)
                    {
                        // Keep the selection button heights consistent when warning icons aren't present
                        fixedHeight = 20
                    });

                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(4));
                GUILayout.Space(8);

                var selectedPlayerProperty = editorSettingsProperty.FindPropertyRelative("selectedPlayer");
                var selectedPlayer = (PlayerIndex)selectedPlayerProperty.enumValueIndex;

                switch (activePanel)
                {
                    case EditorSettings2.PlayerSettingsPanel.ScaleConfig:
                        DrawScaleSettings(selectedPlayer);
                        break;
                    case EditorSettings2.PlayerSettingsPanel.GameboardConfig:
                        DrawGameBoardSettings(selectedPlayer);
                        break;
                    case EditorSettings2.PlayerSettingsPanel.WandConfig:
                        DrawWandSettings();
                        break;
                    default:    // GlassesConfig
                        DrawGlassesSettings(selectedPlayer);
                        break;
                }
            }
        }

        private void ResetPlayerSubsettings(PlayerIndex selectedPlayer)
        {
            if(selectedPlayer == PlayerIndex.None)
            {
                return;
            }

            var playerSettingsProperty = GetPlayerSettings(selectedPlayer);

            glassesSettingsProperty = playerSettingsProperty.FindPropertyRelative("glassesSettings");
            scaleSettingsProperty = playerSettingsProperty.FindPropertyRelative("scaleSettings");
            gameboardSettingsProperty = playerSettingsProperty.FindPropertyRelative("gameboardSettings");
            leftWandSettingsProperty = playerSettingsProperty.FindPropertyRelative("leftWandSettings");
            rightWandSettingsProperty = playerSettingsProperty.FindPropertyRelative("rightWandSettings");
        }

        private void DrawPlayerSettingsPanelButton(SerializedProperty selectedPlayerSettingsPanelProperty, EditorSettings2.PlayerSettingsPanel playerSettingsPanel, GUIContent label)
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

            var activePanel = (EditorSettings2.PlayerSettingsPanel)selectedPlayerSettingsPanelProperty.enumValueIndex;

            if (GUILayout.Toggle(activePanel == playerSettingsPanel, label, buttonStyle))
            {
                selectedPlayerSettingsPanelProperty.enumValueIndex = (int)playerSettingsPanel;
            }
        }

        private void DrawPlayerSettingsPanelButton(SerializedProperty selectedPlayerSettingsPanelProperty, EditorSettings2.PlayerSettingsPanel playerSettingsPanel, string label)
        {
            DrawPlayerSettingsPanelButton(selectedPlayerSettingsPanelProperty, playerSettingsPanel, new GUIContent(label));
        }

        #endregion


        #region Glasses Settings

        private void DrawGlassesSettings(PlayerIndex selectedPlayer)
        {
            // Handle Player One a little differently from other players due to the settings copying logic
            if(selectedPlayer == PlayerIndex.One)
            {
                GlassesSettingsDrawer.DrawPlayerOne(playerOneSettingsProperty.FindPropertyRelative("glassesSettings"),
                    playerTwoSettingsProperty.FindPropertyRelative("glassesSettings"),
                    playerThreeSettingsProperty.FindPropertyRelative("glassesSettings"),
                    playerFourSettingsProperty.FindPropertyRelative("glassesSettings"));
                return;
            }

            // Otherwise proceed as normal
            GlassesSettingsDrawer.DrawRemainingPlayers(glassesSettingsProperty, playerOneSettingsProperty.FindPropertyRelative("glassesSettings"));
        }

        #endregion


        #region Scale Settings

        private void DrawScaleSettings(PlayerIndex selectedPlayer)
        {
            // Handle Player One a little differently from other players due to the settings copying logic
            if (selectedPlayer == PlayerIndex.One)
            {
                ScaleSettingsDrawer.Draw(playerOneSettingsProperty.FindPropertyRelative("scaleSettings"),
                    playerTwoSettingsProperty.FindPropertyRelative("scaleSettings"),
                    playerThreeSettingsProperty.FindPropertyRelative("scaleSettings"),
                    playerFourSettingsProperty.FindPropertyRelative("scaleSettings"));
                return;
            }

            // Otherwise proceed as normal
            ScaleSettingsDrawer.Draw(scaleSettingsProperty, playerOneSettingsProperty.FindPropertyRelative("scaleSettings"));
        }

        #endregion


        #region Game Board Settings

        private void DrawGameBoardSettings(PlayerIndex selectedPlayer)
        {
            if(selectedPlayer == PlayerIndex.One)
            {
                GameBoardSettingsDrawer.Draw(playerOneSettingsProperty.FindPropertyRelative("gameboardSettings"),
                    playerTwoSettingsProperty.FindPropertyRelative("gameboardSettings"),
                    playerThreeSettingsProperty.FindPropertyRelative("gameboardSettings"),
                    playerFourSettingsProperty.FindPropertyRelative("gameboardSettings"));
                return;
            }

            // Otherwise proceed as normal
            GameBoardSettingsDrawer.Draw(gameboardSettingsProperty, playerOneSettingsProperty.FindPropertyRelative("gameboardSettings"));
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

        #endregion
    }
}
