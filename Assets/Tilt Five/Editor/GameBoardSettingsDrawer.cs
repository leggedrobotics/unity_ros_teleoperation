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
    public class GameBoardSettingsDrawer
    {
        static GUIStyle CopyFromPlayerOneButtonStyle = new GUIStyle(GUI.skin.label)
        {
            fixedWidth = 20,
            contentOffset = new Vector2(-4, -3),
            margin = new RectOffset(5, 5, 5, 5)
        };
        static readonly GUIContent copyButtonContent = new GUIContent(EditorGUIUtility.IconContent("TreeEditor.Duplicate")) { tooltip = "Import setting from Player 1" };

        /// <summary>
        /// Draw the Gameboard settings for the singleplayer TiltFiveManager
        /// </summary>
        /// <param name="gameBoardSettingsProperty"></param>
        public static void Draw(SerializedProperty gameBoardSettingsProperty)
        {
            var currentGameBoardProperty = gameBoardSettingsProperty.FindPropertyRelative("currentGameBoard");
            bool hasGameBoard = currentGameBoardProperty.objectReferenceValue;

            if (!hasGameBoard)
            {
                EditorGUILayout.HelpBox("Head Tracking requires an active Gameboard assigment.", MessageType.Warning);
            }

            // Draw the gameboard field
            EditorGUILayout.PropertyField(currentGameBoardProperty, new GUIContent("Gameboard"));

            var gameboardTypeOverrideProperty = gameBoardSettingsProperty.FindPropertyRelative("gameboardTypeOverride");
            gameboardTypeOverrideProperty.enumValueIndex =
                EditorGUILayout.Popup(new GUIContent("Gameboard Gizmo Override", "Forces the gameboard gizmo to reflect the selected gameboard configuration." +
                System.Environment.NewLine + System.Environment.NewLine +
                "If GameboardType_None is selected, the gizmo automatically reflects the gameboard configuration reported by the Tilt Five plugin."),
                gameboardTypeOverrideProperty.enumValueIndex, gameboardTypeOverrideProperty.enumDisplayNames);
        }

        /// <summary>
        /// Draw the Gameboard settings for Player One for the multiplayer TiltFiveManager
        /// </summary>
        /// <param name="playerOneGameboardSettingsProperty"></param>
        /// <param name="playerTwoGameboardSettingsProperty"></param>
        /// <param name="playerThreeGameboardSettingsProperty"></param>
        /// <param name="playerFourGameboardSettingsProperty"></param>
        public static void Draw(SerializedProperty playerOneGameboardSettingsProperty, SerializedProperty playerTwoGameboardSettingsProperty,
            SerializedProperty playerThreeGameboardSettingsProperty, SerializedProperty playerFourGameboardSettingsProperty)
        {
            var currentGameBoardProperty = playerOneGameboardSettingsProperty.FindPropertyRelative("currentGameBoard");
            bool hasGameBoard = currentGameBoardProperty.objectReferenceValue;

            // Determine which players need to have changes propagated to them
            var copyGameboardToPlayerTwo = playerTwoGameboardSettingsProperty.FindPropertyRelative("copyPlayerOneGameboard").boolValue;
            var copyGameboardToPlayerThree = playerThreeGameboardSettingsProperty.FindPropertyRelative("copyPlayerOneGameboard").boolValue;
            var copyGameboardToPlayerFour = playerFourGameboardSettingsProperty.FindPropertyRelative("copyPlayerOneGameboard").boolValue;
            var copyGameboardToAnyPlayer = copyGameboardToPlayerTwo || copyGameboardToPlayerThree || copyGameboardToPlayerFour;

            if (!hasGameBoard)
            {
                EditorGUILayout.HelpBox("Head Tracking requires an active Gameboard assigment.", MessageType.Warning);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                // Draw the gameboard field
                EditorGUILayout.PropertyField(currentGameBoardProperty, new GUIContent("Gameboard"));

                // Draw the info icon, and set its enabled status if any player is copying this setting from player 1
                using (new EditorGUI.DisabledGroupScope(!copyGameboardToAnyPlayer))
                {
                    SettingsDrawingHelper.DrawSettingSharedToggle(copyGameboardToAnyPlayer);
                }
            }

            // Propagate the gameboard to any players that need it
            currentGameBoardProperty.TryExportObjectReference(playerTwoGameboardSettingsProperty.FindPropertyRelative("currentGameBoard"), copyGameboardToPlayerTwo);
            currentGameBoardProperty.TryExportObjectReference(playerThreeGameboardSettingsProperty.FindPropertyRelative("currentGameBoard"), copyGameboardToPlayerThree);
            currentGameBoardProperty.TryExportObjectReference(playerFourGameboardSettingsProperty.FindPropertyRelative("currentGameBoard"), copyGameboardToPlayerFour);
        }

        /// <summary>
        /// Draw the Gameboard Settings for any player other than Player One for the multiplayer TiltFiveManager
        /// </summary>
        /// <param name="gameBoardSettingsProperty"></param>
        /// <param name="playerOneGameboardSettingsProperty"></param>
        public static void Draw(SerializedProperty gameBoardSettingsProperty, SerializedProperty playerOneGameboardSettingsProperty)
        {
            var currentGameBoardProperty = gameBoardSettingsProperty.FindPropertyRelative("currentGameBoard");
            bool hasGameBoard = currentGameBoardProperty.objectReferenceValue;

            var copyPlayerOneGameboardProperty = gameBoardSettingsProperty.FindPropertyRelative("copyPlayerOneGameboard");

            using(new EditorGUI.DisabledGroupScope(copyPlayerOneGameboardProperty.boolValue))
            {
                if (!hasGameBoard)
                {
                    EditorGUILayout.HelpBox("Head Tracking requires an active Gameboard assigment.", MessageType.Warning);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledGroupScope(copyPlayerOneGameboardProperty.boolValue))
                {
                    // Draw the gameboard field
                    EditorGUILayout.PropertyField(currentGameBoardProperty, new GUIContent("Gameboard"));
                }

                // Draw the lock toggle button indicating whether we want to copy this setting from Player One
                copyPlayerOneGameboardProperty.boolValue = SettingsDrawingHelper.DrawSettingLockToggle(copyPlayerOneGameboardProperty.boolValue);

                // Copy near clip plane from Player 1 if necessary
                playerOneGameboardSettingsProperty.FindPropertyRelative("currentGameBoard").TryExportObjectReference(currentGameBoardProperty, copyPlayerOneGameboardProperty.boolValue);
            }
        }
    }
}