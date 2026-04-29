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
    public class ScaleSettingsDrawer
    {
        static GUIStyle CopyFromPlayerOneButtonStyle = new GUIStyle(GUI.skin.label)
        {
            fixedWidth = 20,
            contentOffset = new Vector2(-4, -3),
            margin = new RectOffset(5, 5, 5, 5)
        };
        static readonly GUIContent copyButtonContent = new GUIContent(EditorGUIUtility.IconContent("TreeEditor.Duplicate")) { tooltip = "Import setting from Player 1" };

        /// <summary>
        /// Draw the Scale settings for the singleplayer TiltFiveManager
        /// </summary>
        /// <param name="scaleSettingsProperty"></param>
        public static void Draw(SerializedProperty scaleSettingsProperty)
        {
            var scaleRatioProperty = scaleSettingsProperty.FindPropertyRelative("contentScaleRatio");
            var physicalUnitsProperty = scaleSettingsProperty.FindPropertyRelative("contentScaleUnit");
            var legacyInvertGameboardScaleProperty = scaleSettingsProperty.FindPropertyRelative("legacyInvertGameboardScale");

            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField(new GUIContent("Content Scale",
                "Content Scale is a scalar applied to the camera translation to achieve " +
                "the effect of scaling content. Setting this may also require you to adjust " +
                "the camera's near and far clip planes."));

                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUIUtility.labelWidth = 145;
                    EditorGUILayout.PropertyField(
                        scaleRatioProperty,
                        new GUIContent("1 world space unit is: "));

                    physicalUnitsProperty.enumValueIndex = EditorGUILayout.Popup(
                        new GUIContent(" "),
                        physicalUnitsProperty.enumValueIndex,
                        physicalUnitsProperty.enumDisplayNames);
                    EditorGUIUtility.labelWidth = 0;
                }

                EditorGUILayout.LabelField(new GUIContent("Legacy Gameboard Scale Inversion",
                "Prior versions of the Tilt Five Unity SDK incorrectly inverted the scale of the " +
                "Gameboard object, causing the virtual gameboard to get smaller, and virtual " +
                "geometry viewed through the glasses to get larger, when the scale transform " +
                "value was increased. Enabling this checkbox will cause the old behavior to be " +
                "used. This is NOT recommended except when attempting to make content developed " +
                "with an older SDK function."));

                using (new EditorGUI.IndentLevelScope())
                {
                    legacyInvertGameboardScaleProperty.boolValue = EditorGUILayout.Toggle(new GUIContent(
                                "Invert Gameboard Scale"), legacyInvertGameboardScaleProperty.boolValue);
                }
            }
        }


        /// <summary>
        /// Draw the Scale settings for Player One for the multiplayer TiltFiveManager
        /// </summary>
        /// <param name="playerOneScaleSettingsProperty"></param>
        /// <param name="playerTwoScaleSettingsProperty"></param>
        /// <param name="playerThreeScaleSettingsProperty"></param>
        /// <param name="playerFourScaleSettingsProperty"></param>
        public static void Draw(SerializedProperty playerOneScaleSettingsProperty, SerializedProperty playerTwoScaleSettingsProperty,
            SerializedProperty playerThreeScaleSettingsProperty, SerializedProperty playerFourScaleSettingsProperty)
        {
            var scaleRatioProperty = playerOneScaleSettingsProperty.FindPropertyRelative("contentScaleRatio");
            var physicalUnitsProperty = playerOneScaleSettingsProperty.FindPropertyRelative("contentScaleUnit");
            var legacyInvertGameboardScaleProperty = playerOneScaleSettingsProperty.FindPropertyRelative("legacyInvertGameboardScale");

            // Determine which players need to have changes propagated to them
            var copyScaleRatioToPlayerTwo = playerTwoScaleSettingsProperty.FindPropertyRelative("copyPlayerOneScaleRatio").boolValue;
            var copyScaleRatioToPlayerThree = playerThreeScaleSettingsProperty.FindPropertyRelative("copyPlayerOneScaleRatio").boolValue;
            var copyScaleRatioToPlayerFour = playerFourScaleSettingsProperty.FindPropertyRelative("copyPlayerOneScaleRatio").boolValue;
            var copyScaleRatioToAnyPlayer = copyScaleRatioToPlayerTwo || copyScaleRatioToPlayerThree || copyScaleRatioToPlayerFour;

            var copyScaleUnitsToPlayerTwo = playerTwoScaleSettingsProperty.FindPropertyRelative("copyPlayerOneScaleUnit").boolValue;
            var copyScaleUnitsToPlayerThree = playerThreeScaleSettingsProperty.FindPropertyRelative("copyPlayerOneScaleUnit").boolValue;
            var copyScaleUnitsToPlayerFour = playerFourScaleSettingsProperty.FindPropertyRelative("copyPlayerOneScaleUnit").boolValue;
            var copyScaleUnitsToAnyPlayer = copyScaleUnitsToPlayerTwo || copyScaleUnitsToPlayerThree || copyScaleUnitsToPlayerFour;

            using (new EditorGUILayout.VerticalScope())     // Stack the content scale and unit fields on top of each other
            {
                EditorGUILayout.LabelField(new GUIContent("Content Scale",
                "Content Scale is a scalar applied to the camera translation to achieve " +
                "the effect of scaling content. Setting this may also require you to adjust " +
                "the camera's near and far clip planes."));

                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUIUtility.labelWidth = 145;
                    using (new EditorGUILayout.HorizontalScope())   // Bundle up the content scale ratio and toggle button side by side.
                    {
                        EditorGUILayout.PropertyField(
                            scaleRatioProperty,
                            new GUIContent("1 world space unit is: "));

                        // Draw the info icon, and set its enabled status if any player is copying this setting from player 1
                        using (new EditorGUI.DisabledGroupScope(!copyScaleRatioToAnyPlayer))
                        {
                            SettingsDrawingHelper.DrawSettingSharedToggle(copyScaleRatioToAnyPlayer);
                        }
                    }

                    using (new EditorGUILayout.HorizontalScope())   // Bundle up the content scale unit dropdown and toggle button side by side.
                    {
                        physicalUnitsProperty.enumValueIndex = EditorGUILayout.Popup(
                            new GUIContent(" "),
                            physicalUnitsProperty.enumValueIndex,
                            physicalUnitsProperty.enumDisplayNames);

                        // Draw the info icon, and set its enabled status if any player is copying this setting from player 1
                        using (new EditorGUI.DisabledGroupScope(!copyScaleUnitsToAnyPlayer))
                        {
                            SettingsDrawingHelper.DrawSettingSharedToggle(copyScaleUnitsToAnyPlayer);
                        }
                    }
                    EditorGUIUtility.labelWidth = 0;
                }

                EditorGUILayout.LabelField(new GUIContent("Legacy Gameboard Scale Inversion",
                "Prior versions of the Tilt Five Unity SDK incorrectly inverted the scale of the " +
                "Gameboard object, causing the virtual gameboard to get smaller, and virtual " +
                "geometry viewed through the glasses to get larger, when the scale transform " +
                "value was increased. Enabling this checkbox will cause the old behavior to be " +
                "used. This is NOT recommended except when attempting to make content developed " +
                "with an older SDK function. The setting for Player 1 applies to all players."));

                using (new EditorGUI.IndentLevelScope())
                {
                    legacyInvertGameboardScaleProperty.boolValue = EditorGUILayout.Toggle(new GUIContent(
                                "Invert Gameboard Scale"), legacyInvertGameboardScaleProperty.boolValue);
                }
            }

            // Propagate the content scale ratio to any players that need it
            scaleRatioProperty.TryExportFloat(playerTwoScaleSettingsProperty.FindPropertyRelative("contentScaleRatio"), copyScaleRatioToPlayerTwo);
            scaleRatioProperty.TryExportFloat(playerThreeScaleSettingsProperty.FindPropertyRelative("contentScaleRatio"), copyScaleRatioToPlayerThree);
            scaleRatioProperty.TryExportFloat(playerFourScaleSettingsProperty.FindPropertyRelative("contentScaleRatio"), copyScaleRatioToPlayerFour);

            // Propagate the content scale units to any players that need it
            physicalUnitsProperty.TryExportEnumValueIndex(playerTwoScaleSettingsProperty.FindPropertyRelative("contentScaleUnit"), copyScaleUnitsToPlayerTwo);
            physicalUnitsProperty.TryExportEnumValueIndex(playerThreeScaleSettingsProperty.FindPropertyRelative("contentScaleUnit"), copyScaleUnitsToPlayerThree);
            physicalUnitsProperty.TryExportEnumValueIndex(playerFourScaleSettingsProperty.FindPropertyRelative("contentScaleUnit"), copyScaleUnitsToPlayerFour);

            // Propagate the legacy gameboard scale inversion to any players that need it
            legacyInvertGameboardScaleProperty.TryExportBool(playerTwoScaleSettingsProperty.FindPropertyRelative("legacyInvertGameboardScale"), true);
            legacyInvertGameboardScaleProperty.TryExportBool(playerThreeScaleSettingsProperty.FindPropertyRelative("legacyInvertGameboardScale"), true);
            legacyInvertGameboardScaleProperty.TryExportBool(playerFourScaleSettingsProperty.FindPropertyRelative("legacyInvertGameboardScale"), true);
        }

        /// <summary>
        /// Draw the Scale settings for players 2-4 for the multiplayer TiltFiveManager
        /// </summary>
        /// <param name="scaleSettingsProperty"></param>
        /// <param name="playerOneScaleSettingsProperty"></param>
        public static void Draw(SerializedProperty scaleSettingsProperty, SerializedProperty playerOneScaleSettingsProperty)
        {
            var scaleRatioProperty = scaleSettingsProperty.FindPropertyRelative("contentScaleRatio");
            var physicalUnitsProperty = scaleSettingsProperty.FindPropertyRelative("contentScaleUnit");
            var legacyInvertGameboardScaleProperty = scaleSettingsProperty.FindPropertyRelative("legacyInvertGameboardScale");

            var copyPlayerOneScaleRatioProperty = scaleSettingsProperty.FindPropertyRelative("copyPlayerOneScaleRatio");
            var copyPlayerOneScaleUnitProperty = scaleSettingsProperty.FindPropertyRelative("copyPlayerOneScaleUnit");

            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    // Disable the content scale label if both fields are being copied
                    using (new EditorGUI.DisabledGroupScope(copyPlayerOneScaleRatioProperty.boolValue && copyPlayerOneScaleUnitProperty.boolValue))
                    {
                        EditorGUILayout.LabelField(new GUIContent("Content Scale",
                        "Content Scale is a scalar applied to the camera translation to achieve " +
                        "the effect of scaling content. Setting this may also require you to adjust " +
                        "the camera's near and far clip planes."));
                    }

                }

                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUIUtility.labelWidth = 145;

                    using (new EditorGUILayout.HorizontalScope())   // Bundle up the content scale unit dropdown and toggle button side by side.
                    {
                        // Disable the content scale ratio field if we're copying it from Player One
                        using (new EditorGUI.DisabledGroupScope(copyPlayerOneScaleRatioProperty.boolValue))
                        {
                            // Draw the content scale ratio field
                            EditorGUILayout.PropertyField(
                                scaleRatioProperty,
                                new GUIContent("1 world space unit is: "));
                        }

                        // Draw the lock toggle button indicating whether we want to copy this setting from Player One
                        copyPlayerOneScaleRatioProperty.boolValue = SettingsDrawingHelper.DrawSettingLockToggle(copyPlayerOneScaleRatioProperty.boolValue);

                        // Copy near clip plane from Player 1 if necessary
                        playerOneScaleSettingsProperty.FindPropertyRelative("contentScaleRatio").TryExportFloat(scaleRatioProperty, copyPlayerOneScaleRatioProperty.boolValue);
                    }


                    using (new EditorGUILayout.HorizontalScope())   // Bundle up the content scale unit dropdown and toggle button side by side.
                    {
                        // Disable the content scale unit field if we're copying it from Player One
                        using (new EditorGUI.DisabledGroupScope(copyPlayerOneScaleUnitProperty.boolValue))
                        {
                            // Draw the content scale unit popup
                            physicalUnitsProperty.enumValueIndex = EditorGUILayout.Popup(
                                new GUIContent(" "),
                                physicalUnitsProperty.enumValueIndex,
                                physicalUnitsProperty.enumDisplayNames);
                        }

                        // Draw the lock toggle button indicating whether we want to copy this setting from Player One
                        copyPlayerOneScaleUnitProperty.boolValue = SettingsDrawingHelper.DrawSettingLockToggle(copyPlayerOneScaleUnitProperty.boolValue);

                        // Copy near clip plane from Player 1 if necessary
                        playerOneScaleSettingsProperty.FindPropertyRelative("contentScaleUnit").TryExportEnumValueIndex(physicalUnitsProperty, copyPlayerOneScaleUnitProperty.boolValue);
                    }


                    EditorGUIUtility.labelWidth = 0;
                }
            }
        }
    }
}
