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
    public class GlassesSettingsDrawer
    {
        #region Private Fields

        static readonly GUIContent nearClipLabel = new GUIContent("Near", "The closest point relative to the camera that drawing will occur." +
            System.Environment.NewLine + System.Environment.NewLine +
            $"A minimum value of {GlassesSettings.MIN_NEAR_CLIP_DISTANCE_IN_METERS} is enforced to prevent user discomfort.");

        static readonly GUIContent farClipLabel = new GUIContent("Far", "The furthest point relative to the camera that drawing will occur.");
        static readonly GUIContent metersLabel = new GUIContent("m", "meters");

        static readonly GUIContent cameraTemplateLabel = new GUIContent("Camera Template", "The Camera driven by the glasses head tracking system.");
        static readonly GUIContent objectTemplateLabel = new GUIContent("Glasses Object Template", "The Game Object spawned by the glasses head tracking system when a new pair of glasses connects, attached to a given Glasses Object. An empty Game Object is created if nothing is specified.");
#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
        static readonly GUIContent playerTemplateLabel = new GUIContent("Player Template", "A Game Object spawned by the glasses head tracking system separate from the Glasses Object. Nothing is created if nothing is specified.");
#endif
        static readonly GUIContent cloneChildrenLabel = new GUIContent("Clone Children",
            "False by default - the Camera Template's children are not cloned when creating the eye cameras at runtime." +
            System.Environment.NewLine + System.Environment.NewLine +
            "Developers may wish to enable this to allow instances of the child GameObjects under the Camera Template to be cloned " +
            "to the eye cameras at runtime. This behavior is disabled by default since this behavior isn't always desirable. " +
            System.Environment.NewLine + System.Environment.NewLine +
            "For example, if one of the camera template's children has an AudioListener component, cloning it would cause Unity " +
            "to print warnings in the console about the presence of multiple AudioListener components in the scene." +
            System.Environment.NewLine + System.Environment.NewLine +
            "On the other hand, if the Camera Template's child doesn't have such a restriction, and it's desirable to have " +
            "its instances follow their respective parent eye cameras as they move through the scene, then it may make sense to enable this option.");

        #endregion

        #region Public Functions

        /// <summary>
        /// Draws the glasses settings for the singleplayer TiltFiveManager
        /// </summary>
        /// <param name="glassesSettingsProperty"></param>
        public static void DrawSingleplayer(SerializedProperty glassesSettingsProperty, SerializedProperty graphicsSettingsProperty)
        {
            DrawSingleplayerCameraTemplateField(glassesSettingsProperty);
            DrawSingleplayerObjectTemplateField(glassesSettingsProperty);
#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
            DrawSingleplayerPlayerTemplateField(glassesSettingsProperty);
#endif
            DrawSingleplayerCullingMaskField(glassesSettingsProperty);
            DrawSingleplayerGlassesFOVField(glassesSettingsProperty);
            DrawSingleplayerClippingPlanes(glassesSettingsProperty);
            DrawSingleplayerGlassesMirrorModeField(glassesSettingsProperty);

            DrawGlassesAvailabilityLabel(glassesSettingsProperty);
            EditorGUILayout.Space();

            DrawTrackingFailureModeField(glassesSettingsProperty);
            GraphicsSettingsDrawer.Draw(graphicsSettingsProperty);
        }

        /// <summary>
        /// Draws the glasses settings for player 1 of the multiplayer TiltFiveManager.
        /// </summary>
        /// <param name="playerOneGlassesSettingsProperty"></param>
        /// <param name="playerTwoGlassesSettingsProperty"></param>
        /// <param name="playerThreeGlassesSettingsProperty"></param>
        /// <param name="playerFourGlassesSettingsProperty"></param>
        public static void DrawPlayerOne(SerializedProperty playerOneGlassesSettingsProperty, SerializedProperty playerTwoGlassesSettingsProperty,
            SerializedProperty playerThreeGlassesSettingsProperty, SerializedProperty playerFourGlassesSettingsProperty)
        {
            DrawPlayerOneCameraTemplateField(playerOneGlassesSettingsProperty, playerTwoGlassesSettingsProperty, playerThreeGlassesSettingsProperty, playerFourGlassesSettingsProperty);
            DrawPlayerOneObjectTemplateField(playerOneGlassesSettingsProperty, playerTwoGlassesSettingsProperty, playerThreeGlassesSettingsProperty, playerFourGlassesSettingsProperty);
#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
            DrawPlayerOnePlayerTemplateField(playerOneGlassesSettingsProperty, playerTwoGlassesSettingsProperty, playerThreeGlassesSettingsProperty, playerFourGlassesSettingsProperty);
#endif
            DrawPlayerOneCullingMaskField(playerOneGlassesSettingsProperty, playerTwoGlassesSettingsProperty, playerThreeGlassesSettingsProperty, playerFourGlassesSettingsProperty);
            DrawPlayerOneGlassesFOVField(playerOneGlassesSettingsProperty, playerTwoGlassesSettingsProperty, playerThreeGlassesSettingsProperty, playerFourGlassesSettingsProperty);
            DrawPlayerOneClippingPlanes(playerOneGlassesSettingsProperty, playerTwoGlassesSettingsProperty, playerThreeGlassesSettingsProperty, playerFourGlassesSettingsProperty);

            DrawGlassesAvailabilityLabel(playerOneGlassesSettingsProperty);
        }

        /// <summary>
        /// Draws the glasses settings for players 2-4 for the multiplayer TiltFiveManager
        /// </summary>
        /// <param name="glassesSettingsProperty"></param>
        /// <param name="playerOneGlassesSettingsProperty"></param>
        public static void DrawRemainingPlayers(SerializedProperty glassesSettingsProperty, SerializedProperty playerOneGlassesSettingsProperty)
        {
            DrawRemainingPlayersCameraTemplateField(glassesSettingsProperty, playerOneGlassesSettingsProperty);
            DrawRemainingPlayersObjectTemplateField(glassesSettingsProperty, playerOneGlassesSettingsProperty);
#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
            DrawRemainingPlayersPlayerTemplateField(glassesSettingsProperty, playerOneGlassesSettingsProperty);
#endif
            DrawRemainingPlayersCullingMaskField(glassesSettingsProperty, playerOneGlassesSettingsProperty);
            DrawRemainingPlayersGlassesFOVField(glassesSettingsProperty, playerOneGlassesSettingsProperty);
            DrawRemainingPlayersClippingPlanes(glassesSettingsProperty, playerOneGlassesSettingsProperty);

            DrawGlassesAvailabilityLabel(glassesSettingsProperty);
        }

        #endregion


        #region Camera Template

        /// <summary>
        /// Draw the camera template field for the singleplayer TiltFiveManager
        /// </summary>
        /// <param name="glassesSettingsProperty"></param>
        private static void DrawSingleplayerCameraTemplateField(SerializedProperty glassesSettingsProperty)
        {
            var cameraTemplateProperty = glassesSettingsProperty.FindPropertyRelative("headPoseCamera");
            var cloneChildrenProperty = glassesSettingsProperty.FindPropertyRelative("cloneCameraTemplateChildren");

            // Warn the developer if they haven't assigned a camera yet
            DrawPreviewCameraWarning(cameraTemplateProperty);

            // Draw the camera template field
            EditorGUILayout.PropertyField(cameraTemplateProperty, cameraTemplateLabel, GUILayout.ExpandWidth(true));

            // Draw the "Clone Children" checkbox
            EditorGUILayout.PropertyField(cloneChildrenProperty, cloneChildrenLabel);
        }

        /// <summary>
        /// Draw the camera template field for Player One for the multiplayer TiltFiveManager
        /// </summary>
        /// <param name="playerOneGlassesSettingsProperty"></param>
        /// <param name="playerTwoGlassesSettingsProperty"></param>
        /// <param name="playerThreeGlassesSettingsProperty"></param>
        /// <param name="playerFourGlassesSettingsProperty"></param>
        private static void DrawPlayerOneCameraTemplateField(SerializedProperty playerOneGlassesSettingsProperty, SerializedProperty playerTwoGlassesSettingsProperty,
            SerializedProperty playerThreeGlassesSettingsProperty, SerializedProperty playerFourGlassesSettingsProperty)
        {
            var cameraTemplateProperty = playerOneGlassesSettingsProperty.FindPropertyRelative("headPoseCamera");
            var cloneChildrenProperty = playerOneGlassesSettingsProperty.FindPropertyRelative("cloneCameraTemplateChildren");

            // Determine which players need to have changes propagated to them
            var copyToPlayerTwo = playerTwoGlassesSettingsProperty.FindPropertyRelative("copyPlayerOneCameraTemplate").boolValue;
            var copyToPlayerThree = playerThreeGlassesSettingsProperty.FindPropertyRelative("copyPlayerOneCameraTemplate").boolValue;
            var copyToPlayerFour = playerFourGlassesSettingsProperty.FindPropertyRelative("copyPlayerOneCameraTemplate").boolValue;
            var copyToAnyPlayer = copyToPlayerTwo || copyToPlayerThree || copyToPlayerFour;

            var copyCloneChildrenFlagToPlayerTwo = playerTwoGlassesSettingsProperty.FindPropertyRelative("copyCloneCameraTemplateChildren").boolValue;
            var copyCloneChildrenFlagToPlayerThree = playerThreeGlassesSettingsProperty.FindPropertyRelative("copyCloneCameraTemplateChildren").boolValue;
            var copyCloneChildrenFlagToPlayerFour = playerFourGlassesSettingsProperty.FindPropertyRelative("copyCloneCameraTemplateChildren").boolValue;
            var copyCloneChildrenFlagToAnyPlayer = copyCloneChildrenFlagToPlayerTwo || copyCloneChildrenFlagToPlayerThree || copyCloneChildrenFlagToPlayerFour;

            using (new EditorGUILayout.HorizontalScope())   // Bundle up the camera template field and toggle button side by side.
            {
                // Draw the camera template field
                EditorGUILayout.PropertyField(cameraTemplateProperty, cameraTemplateLabel, GUILayout.ExpandWidth(true));

                // Draw the info icon, and set its enabled status if any player is copying this setting from player 1
                using (new EditorGUI.DisabledGroupScope(!copyToAnyPlayer))
                {
                    SettingsDrawingHelper.DrawSettingSharedToggle(copyToAnyPlayer);
                }
            }

            using (new EditorGUILayout.HorizontalScope())   // Bundle up the "Clone Children" checkbox and toggle button side by side
            {
                // Draw the "Clone Children" checkbox
                EditorGUILayout.PropertyField(cloneChildrenProperty, cloneChildrenLabel);

                // Draw the info icon, and set its enabled status if any player is copying this setting from player 1
                using (new EditorGUI.DisabledGroupScope(!copyCloneChildrenFlagToAnyPlayer))
                {
                    SettingsDrawingHelper.DrawSettingSharedToggle(copyCloneChildrenFlagToAnyPlayer);
                }
            }

            // Propagate the camera template to any players that need it
            cameraTemplateProperty.TryExportObjectReference(playerTwoGlassesSettingsProperty.FindPropertyRelative("headPoseCamera"), copyToPlayerTwo);
            cameraTemplateProperty.TryExportObjectReference(playerThreeGlassesSettingsProperty.FindPropertyRelative("headPoseCamera"), copyToPlayerThree);
            cameraTemplateProperty.TryExportObjectReference(playerFourGlassesSettingsProperty.FindPropertyRelative("headPoseCamera"), copyToPlayerFour);

            cloneChildrenProperty.TryExportBool(playerTwoGlassesSettingsProperty.FindPropertyRelative("cloneCameraTemplateChildren"), copyCloneChildrenFlagToPlayerTwo);
            cloneChildrenProperty.TryExportBool(playerThreeGlassesSettingsProperty.FindPropertyRelative("cloneCameraTemplateChildren"), copyCloneChildrenFlagToPlayerThree);
            cloneChildrenProperty.TryExportBool(playerFourGlassesSettingsProperty.FindPropertyRelative("cloneCameraTemplateChildren"), copyCloneChildrenFlagToPlayerFour);
        }

        /// <summary>
        /// Draw the camera template field for any player other than Player One for the multiplayer TiltFiveManager
        /// </summary>
        /// <param name="glassesSettingsProperty"></param>
        /// <param name="playerOneGlassesSettingsProperty"></param>
        private static void DrawRemainingPlayersCameraTemplateField(SerializedProperty glassesSettingsProperty, SerializedProperty playerOneGlassesSettingsProperty)
        {
            var cameraTemplateProperty = glassesSettingsProperty.FindPropertyRelative("headPoseCamera");

            using (new EditorGUILayout.HorizontalScope())   // Bundle up the camera template field and toggle button side by side.
            {
                var copyCameraTemplateProperty = glassesSettingsProperty.FindPropertyRelative("copyPlayerOneCameraTemplate");

                // Disable the head pose field if we're copying from Player 1
                using (new EditorGUI.DisabledGroupScope(copyCameraTemplateProperty.boolValue))
                {
                    // Draw the camera template field
                    EditorGUILayout.PropertyField(cameraTemplateProperty, cameraTemplateLabel, GUILayout.ExpandWidth(true));
                }

                // Draw the lock toggle button indicating whether we want to copy this setting from Player One
                copyCameraTemplateProperty.boolValue = SettingsDrawingHelper.DrawSettingLockToggle(copyCameraTemplateProperty.boolValue);

                // Copy camera template from Player 1 if necessary
                playerOneGlassesSettingsProperty.FindPropertyRelative("headPoseCamera").TryExportObjectReference(cameraTemplateProperty, copyCameraTemplateProperty.boolValue);
            }

            var cloneChildrenProperty = glassesSettingsProperty.FindPropertyRelative("cloneCameraTemplateChildren");

            using (new EditorGUILayout.HorizontalScope())   // Bundle up the "Clone Children" checkbox and toggle button side by side
            {
                var copyCloneChildrenProperty = glassesSettingsProperty.FindPropertyRelative("copyCloneCameraTemplateChildren");

                // Disable the "Clone Children" checkbox if we're copying from Player 1
                using (new EditorGUI.DisabledGroupScope(copyCloneChildrenProperty.boolValue))
                {
                    // Draw the "Clone Children" checkbox
                    EditorGUILayout.PropertyField(cloneChildrenProperty, cloneChildrenLabel);
                }

                // Draw the lock toggle button indicating whether we want to copy this setting from Player One
                copyCloneChildrenProperty.boolValue = SettingsDrawingHelper.DrawSettingLockToggle(copyCloneChildrenProperty.boolValue);

                // Copy "Clone Children" checkbox value from Player 1 if necessary
                playerOneGlassesSettingsProperty.FindPropertyRelative("cloneCameraTemplateChildren").TryExportBool(cloneChildrenProperty, copyCloneChildrenProperty.boolValue);
            }
        }

        #endregion

        #region Object Template

        /// <summary>
        /// Draw the object template field for the singleplayer TiltFiveManager
        /// </summary>
        /// <param name="glassesSettingsProperty"></param>
        private static void DrawSingleplayerObjectTemplateField(SerializedProperty glassesSettingsProperty)
        {
            var objectTemplateProperty = glassesSettingsProperty.FindPropertyRelative("objectTemplate");

            // Draw the object template field
            EditorGUILayout.PropertyField(objectTemplateProperty, objectTemplateLabel, GUILayout.ExpandWidth(true));

        }

        /// <summary>
        /// Draw the object template field for Player One for the multiplayer TiltFiveManager
        /// </summary>
        /// <param name="playerOneGlassesSettingsProperty"></param>
        /// <param name="playerTwoGlassesSettingsProperty"></param>
        /// <param name="playerThreeGlassesSettingsProperty"></param>
        /// <param name="playerFourGlassesSettingsProperty"></param>
        private static void DrawPlayerOneObjectTemplateField(SerializedProperty playerOneGlassesSettingsProperty, SerializedProperty playerTwoGlassesSettingsProperty,
            SerializedProperty playerThreeGlassesSettingsProperty, SerializedProperty playerFourGlassesSettingsProperty)
        {
            var objectTemplateProperty = playerOneGlassesSettingsProperty.FindPropertyRelative("objectTemplate");

            // Determine which players need to have changes propagated to them
            var copyToPlayerTwo = playerTwoGlassesSettingsProperty.FindPropertyRelative("copyPlayerOneObjectTemplate").boolValue;
            var copyToPlayerThree = playerThreeGlassesSettingsProperty.FindPropertyRelative("copyPlayerOneObjectTemplate").boolValue;
            var copyToPlayerFour = playerFourGlassesSettingsProperty.FindPropertyRelative("copyPlayerOneObjectTemplate").boolValue;
            var copyToAnyPlayer = copyToPlayerTwo || copyToPlayerThree || copyToPlayerFour;

            using (new EditorGUILayout.HorizontalScope())   // Bundle up the object template field and toggle button side by side.
            {
                // Draw the object template field
                EditorGUILayout.PropertyField(objectTemplateProperty, objectTemplateLabel, GUILayout.ExpandWidth(true));

                // Draw the info icon, and set its enabled status if any player is copying this setting from player 1
                using (new EditorGUI.DisabledGroupScope(!copyToAnyPlayer))
                {
                    SettingsDrawingHelper.DrawSettingSharedToggle(copyToAnyPlayer);
                }
            }


            // Propagate the object template to any players that need it
            objectTemplateProperty.TryExportObjectReference(playerTwoGlassesSettingsProperty.FindPropertyRelative("objectTemplate"), copyToPlayerTwo);
            objectTemplateProperty.TryExportObjectReference(playerThreeGlassesSettingsProperty.FindPropertyRelative("objectTemplate"), copyToPlayerThree);
            objectTemplateProperty.TryExportObjectReference(playerFourGlassesSettingsProperty.FindPropertyRelative("objectTemplate"), copyToPlayerFour);

           }

        /// <summary>
        /// Draw the object template field for any player other than Player One for the multiplayer TiltFiveManager
        /// </summary>
        /// <param name="glassesSettingsProperty"></param>
        /// <param name="playerOneGlassesSettingsProperty"></param>
        private static void DrawRemainingPlayersObjectTemplateField(SerializedProperty glassesSettingsProperty, SerializedProperty playerOneGlassesSettingsProperty)
        {
            var objectTemplateProperty = glassesSettingsProperty.FindPropertyRelative("objectTemplate");

            using (new EditorGUILayout.HorizontalScope())   // Bundle up the object template field and toggle button side by side.
            {
                var copyObjectTemplateProperty = glassesSettingsProperty.FindPropertyRelative("copyPlayerOneObjectTemplate");

                // Disable the object template field if we're copying from Player 1
                using (new EditorGUI.DisabledGroupScope(copyObjectTemplateProperty.boolValue))
                {
                    // Draw the object template field
                    EditorGUILayout.PropertyField(objectTemplateProperty, objectTemplateLabel, GUILayout.ExpandWidth(true));
                }

                // Draw the lock toggle button indicating whether we want to copy this setting from Player One
                copyObjectTemplateProperty.boolValue = SettingsDrawingHelper.DrawSettingLockToggle(copyObjectTemplateProperty.boolValue);

                // Copy object template from Player 1 if necessary
                playerOneGlassesSettingsProperty.FindPropertyRelative("objectTemplate").TryExportObjectReference(objectTemplateProperty, copyObjectTemplateProperty.boolValue);
            }
        }

        #endregion

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
        #region Input Template

        /// <summary>
        /// Draw the object template field for the singleplayer TiltFiveManager
        /// </summary>
        /// <param name="glassesSettingsProperty"></param>
        private static void DrawSingleplayerPlayerTemplateField(SerializedProperty glassesSettingsProperty)
        {
            var playerTemplateProperty = glassesSettingsProperty.FindPropertyRelative("playerTemplate");

            // Draw the object template field
            EditorGUILayout.PropertyField(playerTemplateProperty, playerTemplateLabel, GUILayout.ExpandWidth(true));

        }

        /// <summary>
        /// Draw the object template field for Player One for the multiplayer TiltFiveManager
        /// </summary>
        /// <param name="playerOneGlassesSettingsProperty"></param>
        /// <param name="playerTwoGlassesSettingsProperty"></param>
        /// <param name="playerThreeGlassesSettingsProperty"></param>
        /// <param name="playerFourGlassesSettingsProperty"></param>
        private static void DrawPlayerOnePlayerTemplateField(SerializedProperty playerOneGlassesSettingsProperty, SerializedProperty playerTwoGlassesSettingsProperty,
            SerializedProperty playerThreeGlassesSettingsProperty, SerializedProperty playerFourGlassesSettingsProperty)
        {
            var playerTemplateProperty = playerOneGlassesSettingsProperty.FindPropertyRelative("playerTemplate");

            // Determine which players need to have changes propagated to them
            var copyToPlayerTwo = playerTwoGlassesSettingsProperty.FindPropertyRelative("copyPlayerOnePlayerTemplate").boolValue;
            var copyToPlayerThree = playerThreeGlassesSettingsProperty.FindPropertyRelative("copyPlayerOnePlayerTemplate").boolValue;
            var copyToPlayerFour = playerFourGlassesSettingsProperty.FindPropertyRelative("copyPlayerOnePlayerTemplate").boolValue;
            var copyToAnyPlayer = copyToPlayerTwo || copyToPlayerThree || copyToPlayerFour;

            using (new EditorGUILayout.HorizontalScope())   // Bundle up the object template field and toggle button side by side.
            {
                // Draw the object template field
                EditorGUILayout.PropertyField(playerTemplateProperty, playerTemplateLabel, GUILayout.ExpandWidth(true));

                // Draw the info icon, and set its enabled status if any player is copying this setting from player 1
                using (new EditorGUI.DisabledGroupScope(!copyToAnyPlayer))
                {
                    SettingsDrawingHelper.DrawSettingSharedToggle(copyToAnyPlayer);
                }
            }


            // Propagate the object template to any players that need it
            playerTemplateProperty.TryExportObjectReference(playerTwoGlassesSettingsProperty.FindPropertyRelative("playerTemplate"), copyToPlayerTwo);
            playerTemplateProperty.TryExportObjectReference(playerThreeGlassesSettingsProperty.FindPropertyRelative("playerTemplate"), copyToPlayerThree);
            playerTemplateProperty.TryExportObjectReference(playerFourGlassesSettingsProperty.FindPropertyRelative("playerTemplate"), copyToPlayerFour);

           }

        /// <summary>
        /// Draw the object template field for any player other than Player One for the multiplayer TiltFiveManager
        /// </summary>
        /// <param name="glassesSettingsProperty"></param>
        /// <param name="playerOneGlassesSettingsProperty"></param>
        private static void DrawRemainingPlayersPlayerTemplateField(SerializedProperty glassesSettingsProperty, SerializedProperty playerOneGlassesSettingsProperty)
        {
            var playerTemplateProperty = glassesSettingsProperty.FindPropertyRelative("playerTemplate");

            using (new EditorGUILayout.HorizontalScope())   // Bundle up the object template field and toggle button side by side.
            {
                var copyPlayerTemplateProperty = glassesSettingsProperty.FindPropertyRelative("copyPlayerOnePlayerTemplate");

                // Disable the object template field if we're copying from Player 1
                using (new EditorGUI.DisabledGroupScope(copyPlayerTemplateProperty.boolValue))
                {
                    // Draw the object template field
                    EditorGUILayout.PropertyField(playerTemplateProperty, playerTemplateLabel, GUILayout.ExpandWidth(true));
                }

                // Draw the lock toggle button indicating whether we want to copy this setting from Player One
                copyPlayerTemplateProperty.boolValue = SettingsDrawingHelper.DrawSettingLockToggle(copyPlayerTemplateProperty.boolValue);

                // Copy object template from Player 1 if necessary
                playerOneGlassesSettingsProperty.FindPropertyRelative("playerTemplate").TryExportObjectReference(playerTemplateProperty, copyPlayerTemplateProperty.boolValue);
            }
        }
        
        #endregion Input Template

#endif // UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE

        #region Culling Mask

        /// <summary>
        /// Draw the culling mask field for the singleplayer TiltFiveManager
        /// </summary>
        /// <param name="glassesSettingsProperty"></param>
        private static void DrawSingleplayerCullingMaskField(SerializedProperty glassesSettingsProperty)
        {
            var cullingMaskProperty = glassesSettingsProperty.FindPropertyRelative("cullingMask");

            // Draw the culling mask field
            EditorGUILayout.PropertyField(cullingMaskProperty, new GUIContent("Culling Mask", "The culling mask(s) to be used by this player's eye cameras."), GUILayout.ExpandWidth(true));
        }

        /// <summary>
        /// Draw the culling mask field for Player One for the multiplayer TiltFiveManager
        /// </summary>
        /// <param name="playerOneGlassesSettingsProperty"></param>
        /// <param name="playerTwoGlassesSettingsProperty"></param>
        /// <param name="playerThreeGlassesSettingsProperty"></param>
        /// <param name="playerFourGlassesSettingsProperty"></param>
        private static void DrawPlayerOneCullingMaskField(SerializedProperty playerOneGlassesSettingsProperty, SerializedProperty playerTwoGlassesSettingsProperty,
            SerializedProperty playerThreeGlassesSettingsProperty, SerializedProperty playerFourGlassesSettingsProperty)
        {
            var cullingMaskProperty = playerOneGlassesSettingsProperty.FindPropertyRelative("cullingMask");

            // Determine which players need to have changes propagated to them
            var copyToPlayerTwo = playerTwoGlassesSettingsProperty.FindPropertyRelative("copyPlayerOneCullingMask").boolValue;
            var copyToPlayerThree = playerThreeGlassesSettingsProperty.FindPropertyRelative("copyPlayerOneCullingMask").boolValue;
            var copyToPlayerFour = playerFourGlassesSettingsProperty.FindPropertyRelative("copyPlayerOneCullingMask").boolValue;
            var copyToAnyPlayer = copyToPlayerTwo || copyToPlayerThree || copyToPlayerFour;

            using (new EditorGUILayout.HorizontalScope())   // Bundle up the culling mask field and toggle button side by side.
            {
                // Draw the culling mask field
                EditorGUILayout.PropertyField(cullingMaskProperty, new GUIContent("Culling Mask", "The culling mask(s) to be used by this player's eye cameras."), GUILayout.ExpandWidth(true));

                // Draw the info icon, and set its enabled status if any player is copying this setting from player 1
                using (new EditorGUI.DisabledGroupScope(!copyToAnyPlayer))
                {
                    SettingsDrawingHelper.DrawSettingSharedToggle(copyToAnyPlayer);
                }
            }

            // Propagate the culling mask to any players that need it
            cullingMaskProperty.TryExportInt(playerTwoGlassesSettingsProperty.FindPropertyRelative("cullingMask"), copyToPlayerTwo);
            cullingMaskProperty.TryExportInt(playerThreeGlassesSettingsProperty.FindPropertyRelative("cullingMask"), copyToPlayerThree);
            cullingMaskProperty.TryExportInt(playerFourGlassesSettingsProperty.FindPropertyRelative("cullingMask"), copyToPlayerFour);
        }

        /// <summary>
        /// Draw the culling mask field for any player other than Player One for the multiplayer TiltFiveManager
        /// </summary>
        /// <param name="glassesSettingsProperty"></param>
        /// <param name="playerOneGlassesSettingsProperty"></param>
        private static void DrawRemainingPlayersCullingMaskField(SerializedProperty glassesSettingsProperty, SerializedProperty playerOneGlassesSettingsProperty)
        {
            var cullingMaskProperty = glassesSettingsProperty.FindPropertyRelative("cullingMask");

            //using (new EditorGUI.IndentLevelScope())
            using (new EditorGUILayout.HorizontalScope())   // Bundle up the culling mask field and toggle button side by side.
            {
                var copyCullingMaskProperty = glassesSettingsProperty.FindPropertyRelative("copyPlayerOneCullingMask");

                // Disable the culling mask field if we're copying from Player 1
                using (new EditorGUI.DisabledGroupScope(copyCullingMaskProperty.boolValue))
                {
                    // Draw the culling mask field
                    EditorGUILayout.PropertyField(cullingMaskProperty, new GUIContent("Culling Mask", "The culling mask(s) to be used by this player's eye cameras."), GUILayout.ExpandWidth(true));
                }

                // Draw the lock toggle button indicating whether we want to copy this setting from Player One
                copyCullingMaskProperty.boolValue = SettingsDrawingHelper.DrawSettingLockToggle(copyCullingMaskProperty.boolValue);

                // Copy culling mask from Player 1 if necessary
                playerOneGlassesSettingsProperty.FindPropertyRelative("cullingMask").TryExportInt(cullingMaskProperty, copyCullingMaskProperty.boolValue);
            }
        }

        #endregion


        #region Field of View

        /// <summary>
        /// Draw the glasses FOV section for the singleplayer TiltFiveManager
        /// </summary>
        /// <param name="glassesSettingsProperty"></param>
        private static void DrawSingleplayerGlassesFOVField(SerializedProperty glassesSettingsProperty)
        {
            var overrideFOVProperty = glassesSettingsProperty.FindPropertyRelative("overrideFOV");
            var glassesFOVProperty = glassesSettingsProperty.FindPropertyRelative("customFOV");

            // Draw the override FOV toggle
            overrideFOVProperty.boolValue = EditorGUILayout.Toggle(new GUIContent("Override FOV"), overrideFOVProperty.boolValue);

            // Draw the FOV slider if necessary
            if (overrideFOVProperty.boolValue)
            {
                DrawOverrideFOVWarning();

                // Draw the FOV slider
                glassesFOVProperty.floatValue = EditorGUILayout.Slider(
                    new GUIContent("Field of View", "The field of view of the eye cameras. Higher values trade perceived sharpness for increased projection FOV."),
                    glassesFOVProperty.floatValue, GlassesSettings.MIN_FOV, GlassesSettings.MAX_FOV);
            }
        }

        /// <summary>
        /// Draw the glasses FOV section for Player One for the multiplayer TiltFiveManager
        /// </summary>
        /// <param name="playerOneGlassesSettingsProperty"></param>
        /// <param name="playerTwoGlassesSettingsProperty"></param>
        /// <param name="playerThreeGlassesSettingsProperty"></param>
        /// <param name="playerFourGlassesSettingsProperty"></param>
        private static void DrawPlayerOneGlassesFOVField(SerializedProperty playerOneGlassesSettingsProperty, SerializedProperty playerTwoGlassesSettingsProperty,
            SerializedProperty playerThreeGlassesSettingsProperty, SerializedProperty playerFourGlassesSettingsProperty)
        {
            var overrideFOVProperty = playerOneGlassesSettingsProperty.FindPropertyRelative("overrideFOV");

            // Determine which players need to have changes propagated to them
            var copyFOVToggleToPlayerTwo = playerTwoGlassesSettingsProperty.FindPropertyRelative("copyPlayerOneFOVToggle").boolValue;
            var copyFOVToggleToPlayerThree = playerThreeGlassesSettingsProperty.FindPropertyRelative("copyPlayerOneFOVToggle").boolValue;
            var copyFOVToggleToPlayerFour = playerFourGlassesSettingsProperty.FindPropertyRelative("copyPlayerOneFOVToggle").boolValue;
            var copyFOVToggleToAnyPlayer = copyFOVToggleToPlayerTwo || copyFOVToggleToPlayerThree || copyFOVToggleToPlayerFour;

            using (new EditorGUILayout.HorizontalScope())   // Bundle up the FOV toggle and toggle button side by side.
            {
                // Draw the override FOV toggle
                overrideFOVProperty.boolValue = EditorGUILayout.Toggle(new GUIContent("Override FOV"), overrideFOVProperty.boolValue);

                // Draw the info icon, and set its enabled status if any player is copying this setting from player 1
                using (new EditorGUI.DisabledGroupScope(!copyFOVToggleToAnyPlayer))
                {
                    SettingsDrawingHelper.DrawSettingSharedToggle(copyFOVToggleToAnyPlayer);
                }
            }

            // Propagate the FOV toggle state to any players that need it
            overrideFOVProperty.TryExportBool(playerTwoGlassesSettingsProperty.FindPropertyRelative("overrideFOV"), copyFOVToggleToPlayerTwo);
            overrideFOVProperty.TryExportBool(playerThreeGlassesSettingsProperty.FindPropertyRelative("overrideFOV"), copyFOVToggleToPlayerThree);
            overrideFOVProperty.TryExportBool(playerFourGlassesSettingsProperty.FindPropertyRelative("overrideFOV"), copyFOVToggleToPlayerFour);


            // We're done with the FOV Override toggle, so we can move on to the FOV slider
            var glassesFOVProperty = playerOneGlassesSettingsProperty.FindPropertyRelative("customFOV");

            // Determine which players need to have changes propagated to them
            var copyFOVToPlayerTwo = playerTwoGlassesSettingsProperty.FindPropertyRelative("copyPlayerOneFOV").boolValue;
            var copyFOVToPlayerThree = playerThreeGlassesSettingsProperty.FindPropertyRelative("copyPlayerOneFOV").boolValue;
            var copyFOVToPlayerFour = playerFourGlassesSettingsProperty.FindPropertyRelative("copyPlayerOneFOV").boolValue;
            var copyFOVToAnyPlayer = copyFOVToPlayerTwo || copyFOVToPlayerThree || copyFOVToPlayerFour;

            // Draw the FOV slider if necessary
            if (overrideFOVProperty.boolValue)
            {
                DrawOverrideFOVWarning();

                using (new EditorGUILayout.HorizontalScope())   // Bundle up the FOV slider and toggle button side by side.
                {
                    // Draw the FOV slider
                    glassesFOVProperty.floatValue = EditorGUILayout.Slider(
                        new GUIContent("Field of View", "The field of view of the eye cameras. Higher values trade perceived sharpness for increased projection FOV."),
                        glassesFOVProperty.floatValue, GlassesSettings.MIN_FOV, GlassesSettings.MAX_FOV);

                    // Draw the info icon, and set its enabled status if any player is copying this setting from player 1
                    using (new EditorGUI.DisabledGroupScope(!copyFOVToAnyPlayer))
                    {
                        SettingsDrawingHelper.DrawSettingSharedToggle(copyFOVToAnyPlayer);
                    }
                }
            }

            // Propagate the FOV to any players that need it
            glassesFOVProperty.TryExportFloat(playerTwoGlassesSettingsProperty.FindPropertyRelative("customFOV"), copyFOVToPlayerTwo);
            glassesFOVProperty.TryExportFloat(playerThreeGlassesSettingsProperty.FindPropertyRelative("customFOV"), copyFOVToPlayerThree);
            glassesFOVProperty.TryExportFloat(playerFourGlassesSettingsProperty.FindPropertyRelative("customFOV"), copyFOVToPlayerFour);
        }

        /// <summary>
        /// Draw the glasses FOV section for any player other than Player One for the multiplayer TiltFiveManager
        /// </summary>
        /// <param name="glassesSettingsProperty"></param>
        /// <param name="playerOneGlassesSettingsProperty"></param>
        private static void DrawRemainingPlayersGlassesFOVField(SerializedProperty glassesSettingsProperty, SerializedProperty playerOneGlassesSettingsProperty = null)
        {
            var overrideFOVProperty = glassesSettingsProperty.FindPropertyRelative("overrideFOV");
            var glassesFOVProperty = glassesSettingsProperty.FindPropertyRelative("customFOV");

            using (new EditorGUILayout.HorizontalScope())   // Bundle up the FOV toggle and toggle button side by side.
            {
                var copyPlayerOneFOVToggleProperty = glassesSettingsProperty.FindPropertyRelative("copyPlayerOneFOVToggle");

                // Disable the "Overide FOV" toggle if we're copying from Player 1
                using (new EditorGUI.DisabledGroupScope(copyPlayerOneFOVToggleProperty.boolValue))
                {
                    // Draw the override FOV toggle
                    overrideFOVProperty.boolValue = EditorGUILayout.Toggle(new GUIContent("Override FOV"), overrideFOVProperty.boolValue);
                }

                // Draw the lock toggle button indicating whether we want to copy this setting from Player One
                copyPlayerOneFOVToggleProperty.boolValue = SettingsDrawingHelper.DrawSettingLockToggle(copyPlayerOneFOVToggleProperty.boolValue);

                // Copy FOV toggle setting from Player 1 if necessary
                playerOneGlassesSettingsProperty.FindPropertyRelative("overrideFOV").TryExportBool(overrideFOVProperty, copyPlayerOneFOVToggleProperty.boolValue);
            }

            var copyPlayerOneFOVProperty = glassesSettingsProperty.FindPropertyRelative("copyPlayerOneFOV");

            // Draw the FOV slider if necessary
            if (overrideFOVProperty.boolValue)
            {
                using (new EditorGUI.DisabledGroupScope(copyPlayerOneFOVProperty.boolValue))
                {
                    DrawOverrideFOVWarning();
                }

                using (new EditorGUILayout.HorizontalScope())   // Bundle up the FOV slider and toggle button side by side.
                {
                    using (new EditorGUI.DisabledGroupScope(copyPlayerOneFOVProperty.boolValue))
                    {
                        // Draw the FOV slider
                        glassesFOVProperty.floatValue = EditorGUILayout.Slider(
                            new GUIContent("Field of View", "The field of view of the eye cameras. Higher values trade perceived sharpness for increased projection FOV."),
                            glassesFOVProperty.floatValue, GlassesSettings.MIN_FOV, GlassesSettings.MAX_FOV);
                    }

                    // Draw the lock toggle button indicating whether we want to copy this setting from Player One
                    copyPlayerOneFOVProperty.boolValue = SettingsDrawingHelper.DrawSettingLockToggle(copyPlayerOneFOVProperty.boolValue);
                }
            }

            // Copy FOV from Player 1 if necessary
            playerOneGlassesSettingsProperty.FindPropertyRelative("customFOV").TryExportFloat(glassesFOVProperty, copyPlayerOneFOVProperty.boolValue);
        }

        #endregion


        #region Clipping Planes

        private static void DrawSingleplayerClippingPlanes(SerializedProperty glassesSettingsProperty)
        {
            var nearClipPlaneProperty = glassesSettingsProperty.FindPropertyRelative("nearClipPlane");
            var farClipPlaneProperty = glassesSettingsProperty.FindPropertyRelative("farClipPlane");

            var oldLabelWidth = EditorGUIUtility.labelWidth;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(new GUIContent("Clipping Planes", "These distances are defined in physical space as meter values."));

                using (new EditorGUILayout.VerticalScope())     // Stack the two clipping plane fields vertically
                {
                    EditorGUIUtility.labelWidth = Mathf.Max(EditorStyles.label.CalcSize(nearClipLabel).x, EditorStyles.label.CalcSize(farClipLabel).x) + 5;
                    var floatFieldWidth = 72;

                    using (new EditorGUILayout.HorizontalScope())   // Place the clipping plane and its unit label side by side
                    {
                        // Draw the near plane field
                        nearClipPlaneProperty.floatValue = Mathf.Clamp(
                            EditorGUILayout.FloatField(nearClipLabel, nearClipPlaneProperty.floatValue, GUILayout.Width(floatFieldWidth)),
                            GlassesSettings.MIN_NEAR_CLIP_DISTANCE_IN_METERS, farClipPlaneProperty.floatValue);
                        DrawMetersLabel();
                    }
                    using (new EditorGUILayout.HorizontalScope())   // Place the clipping plane and its unit label side by side
                    {
                        // Draw the far plane field
                        farClipPlaneProperty.floatValue = Mathf.Max(
                            EditorGUILayout.FloatField(farClipLabel, farClipPlaneProperty.floatValue, GUILayout.Width(floatFieldWidth)), nearClipPlaneProperty.floatValue);
                        DrawMetersLabel();
                    }
                }
            }

            EditorGUIUtility.labelWidth = oldLabelWidth;
        }


        /// <summary>
        /// Draw the clipping planes for Player One for the multiplayer TiltFiveManager
        /// </summary>
        /// <param name="playerOneGlassesSettingsProperty"></param>
        /// <param name="playerTwoGlassesSettingsProperty"></param>
        /// <param name="playerThreeGlassesSettingsProperty"></param>
        /// <param name="playerFourGlassesSettingsProperty"></param>
        private static void DrawPlayerOneClippingPlanes(SerializedProperty playerOneGlassesSettingsProperty, SerializedProperty playerTwoGlassesSettingsProperty,
            SerializedProperty playerThreeGlassesSettingsProperty, SerializedProperty playerFourGlassesSettingsProperty)
        {
            var nearClipPlaneProperty = playerOneGlassesSettingsProperty.FindPropertyRelative("nearClipPlane");
            var farClipPlaneProperty = playerOneGlassesSettingsProperty.FindPropertyRelative("farClipPlane");

            // Determine which players need to have changes propagated to them
            var copyNearClipPlaneToPlayerTwo = playerTwoGlassesSettingsProperty.FindPropertyRelative("copyPlayerOneNearClipPlane").boolValue;
            var copyNearClipPlaneToPlayerThree = playerThreeGlassesSettingsProperty.FindPropertyRelative("copyPlayerOneNearClipPlane").boolValue;
            var copyNearClipPlaneToPlayerFour = playerFourGlassesSettingsProperty.FindPropertyRelative("copyPlayerOneNearClipPlane").boolValue;
            var copyNearClipPlaneToAnyPlayer = copyNearClipPlaneToPlayerTwo || copyNearClipPlaneToPlayerThree || copyNearClipPlaneToPlayerFour;

            // Determine which players need to have changes propagated to them
            var copyFarClipPlaneToPlayerTwo = playerTwoGlassesSettingsProperty.FindPropertyRelative("copyPlayerOneFarClipPlane").boolValue;
            var copyFarClipPlaneToPlayerThree = playerThreeGlassesSettingsProperty.FindPropertyRelative("copyPlayerOneFarClipPlane").boolValue;
            var copyFarClipPlaneToPlayerFour = playerFourGlassesSettingsProperty.FindPropertyRelative("copyPlayerOneFarClipPlane").boolValue;
            var copyFarClipPlaneToAnyPlayer = copyFarClipPlaneToPlayerTwo || copyFarClipPlaneToPlayerThree || copyFarClipPlaneToPlayerFour;

            var oldLabelWidth = EditorGUIUtility.labelWidth;

            using (new EditorGUILayout.HorizontalScope())
            {
                // Draw the clipping planes label
                EditorGUILayout.PrefixLabel(new GUIContent("Clipping Planes", "These distances are defined in physical space as meter values."));

                using (new EditorGUILayout.VerticalScope())     // Stack the two clipping plane fields vertically
                {
                    EditorGUIUtility.labelWidth = Mathf.Max(EditorStyles.label.CalcSize(nearClipLabel).x, EditorStyles.label.CalcSize(farClipLabel).x) + 5;
                    var floatFieldWidth = 72;

                    using (new EditorGUILayout.HorizontalScope())   // Place the clipping plane and its unit label side by side
                    {
                        // Draw the near clip plane field and its unit label
                        nearClipPlaneProperty.floatValue = Mathf.Clamp(
                            EditorGUILayout.FloatField(nearClipLabel, nearClipPlaneProperty.floatValue, GUILayout.Width(floatFieldWidth)),
                            GlassesSettings.MIN_NEAR_CLIP_DISTANCE_IN_METERS, farClipPlaneProperty.floatValue);
                        DrawMetersLabel();

                        // Draw the info icon, and set its enabled status if any player is copying this setting from player 1
                        using (new EditorGUI.DisabledGroupScope(!copyNearClipPlaneToAnyPlayer))
                        {
                            SettingsDrawingHelper.DrawSettingSharedToggle(copyNearClipPlaneToAnyPlayer);
                        }
                    }
                    using (new EditorGUILayout.HorizontalScope())   // Place the clipping plane and its unit label side by side
                    {
                        // Draw the far clip plane field and its unit label
                        farClipPlaneProperty.floatValue = Mathf.Max(
                            EditorGUILayout.FloatField(farClipLabel, farClipPlaneProperty.floatValue, GUILayout.Width(floatFieldWidth)), nearClipPlaneProperty.floatValue);
                        DrawMetersLabel();

                        // Draw the info icon, and set its enabled status if any player is copying this setting from player 1
                        using (new EditorGUI.DisabledGroupScope(!copyFarClipPlaneToAnyPlayer))
                        {
                            SettingsDrawingHelper.DrawSettingSharedToggle(copyFarClipPlaneToAnyPlayer);
                        }
                    }
                }
            }

            EditorGUIUtility.labelWidth = oldLabelWidth;

            // Propagate the near clip plane to any players that need it
            nearClipPlaneProperty.TryExportFloat(playerTwoGlassesSettingsProperty.FindPropertyRelative("nearClipPlane"), copyNearClipPlaneToPlayerTwo);
            nearClipPlaneProperty.TryExportFloat(playerThreeGlassesSettingsProperty.FindPropertyRelative("nearClipPlane"), copyNearClipPlaneToPlayerThree);
            nearClipPlaneProperty.TryExportFloat(playerFourGlassesSettingsProperty.FindPropertyRelative("nearClipPlane"), copyNearClipPlaneToPlayerFour);

            // Propagate the far clip plane to any players that need it
            farClipPlaneProperty.TryExportFloat(playerTwoGlassesSettingsProperty.FindPropertyRelative("farClipPlane"), copyFarClipPlaneToPlayerTwo);
            farClipPlaneProperty.TryExportFloat(playerThreeGlassesSettingsProperty.FindPropertyRelative("farClipPlane"), copyFarClipPlaneToPlayerThree);
            farClipPlaneProperty.TryExportFloat(playerFourGlassesSettingsProperty.FindPropertyRelative("farClipPlane"), copyFarClipPlaneToPlayerFour);
        }

        /// <summary>
        /// Draw the clipping planes for any player other than Player One for the multiplayer TiltFiveManager
        /// </summary>
        /// <param name="glassesSettingsProperty"></param>
        /// <param name="playerOneGlassesSettingsProperty"></param>
        private static void DrawRemainingPlayersClippingPlanes(SerializedProperty glassesSettingsProperty, SerializedProperty playerOneGlassesSettingsProperty)
        {
            var nearClipPlaneProperty = glassesSettingsProperty.FindPropertyRelative("nearClipPlane");
            var farClipPlaneProperty = glassesSettingsProperty.FindPropertyRelative("farClipPlane");

            var copyPlayerOneNearPlaneProperty = glassesSettingsProperty.FindPropertyRelative("copyPlayerOneNearClipPlane");
            var copyPlayerOneFarPlaneProperty = glassesSettingsProperty.FindPropertyRelative("copyPlayerOneFarClipPlane");

            var oldLabelWidth = EditorGUIUtility.labelWidth;

            // Place the clipping planes' vertical scope to the right of the clipping planes label.
            using (new EditorGUILayout.HorizontalScope())
            {
                // Disable the clipping planes label if both clipping planes are being copied
                using (new EditorGUI.DisabledGroupScope(copyPlayerOneNearPlaneProperty.boolValue && copyPlayerOneFarPlaneProperty.boolValue))
                {
                    // Draw the clipping planes label
                    EditorGUILayout.PrefixLabel(new GUIContent("Clipping Planes", "These distances are defined in physical space as meter values."));
                }

                using (new EditorGUILayout.VerticalScope())     // Stack the two clipping plane fields vertically
                {
                    EditorGUIUtility.labelWidth = Mathf.Max(EditorStyles.label.CalcSize(nearClipLabel).x, EditorStyles.label.CalcSize(farClipLabel).x) + 5;
                    var floatFieldWidth = 72;

                    using (new EditorGUILayout.HorizontalScope())   // Place the clipping plane and its unit label side by side
                    {
                        // Disable the near clip plane field if we're copying it from Player One
                        using (new EditorGUI.DisabledGroupScope(copyPlayerOneNearPlaneProperty.boolValue))
                        {
                            // Draw the near clip plane field and its unit label
                            nearClipPlaneProperty.floatValue = Mathf.Clamp(
                                EditorGUILayout.FloatField(nearClipLabel, nearClipPlaneProperty.floatValue, GUILayout.Width(floatFieldWidth)),
                                GlassesSettings.MIN_NEAR_CLIP_DISTANCE_IN_METERS, farClipPlaneProperty.floatValue);
                            DrawMetersLabel();
                        }

                        // Draw the lock toggle button indicating whether we want to copy this setting from Player One
                        copyPlayerOneNearPlaneProperty.boolValue = SettingsDrawingHelper.DrawSettingLockToggle(copyPlayerOneNearPlaneProperty.boolValue);

                        // Copy near clip plane from Player 1 if necessary
                        playerOneGlassesSettingsProperty.FindPropertyRelative("nearClipPlane").TryExportFloat(nearClipPlaneProperty, copyPlayerOneNearPlaneProperty.boolValue);

                    }
                    using (new EditorGUILayout.HorizontalScope())   // Place the clipping plane and its unit label side by side
                    {
                        // Disable the far clip plane field if we're copying it from Player One
                        using (new EditorGUI.DisabledGroupScope(copyPlayerOneFarPlaneProperty.boolValue))
                        {
                            // Draw the far clip plane field and its unit label
                            farClipPlaneProperty.floatValue = Mathf.Max(
                                EditorGUILayout.FloatField(farClipLabel, farClipPlaneProperty.floatValue, GUILayout.Width(floatFieldWidth)), nearClipPlaneProperty.floatValue);
                            DrawMetersLabel();
                        }

                        // Draw the lock toggle button indicating whether we want to copy this setting from Player One
                        copyPlayerOneFarPlaneProperty.boolValue = SettingsDrawingHelper.DrawSettingLockToggle(copyPlayerOneFarPlaneProperty.boolValue);

                        // Copy far clip plane from Player 1 if necessary
                        playerOneGlassesSettingsProperty.FindPropertyRelative("farClipPlane").TryExportFloat(farClipPlaneProperty, copyPlayerOneFarPlaneProperty.boolValue);
                    }
                }
            }

            EditorGUIUtility.labelWidth = oldLabelWidth;
        }


        #endregion


        #region Singleplayer Fields

        private static void DrawSingleplayerGlassesMirrorModeField(SerializedProperty glassesSettingsProperty)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var mirrorModeProperty = glassesSettingsProperty.FindPropertyRelative("glassesMirrorMode");
                var oldEnumValueIndex = mirrorModeProperty.enumValueIndex;
                mirrorModeProperty.enumValueIndex = EditorGUILayout.Popup("Mirror Mode", mirrorModeProperty.enumValueIndex, mirrorModeProperty.enumDisplayNames);
            }
        }

        private static void DrawTrackingFailureModeField(SerializedProperty glassesSettingsProperty)
        {
            var trackingFailureModeProperty = glassesSettingsProperty.FindPropertyRelative("FailureMode");

            trackingFailureModeProperty.enumValueIndex =
                EditorGUILayout.Popup("Tracking Failure Mode", trackingFailureModeProperty.enumValueIndex, trackingFailureModeProperty.enumDisplayNames);

            if((TrackableSettings.TrackingFailureMode)trackingFailureModeProperty.enumValueIndex == TrackableSettings.TrackingFailureMode.SnapToDefault)
            {
                DrawPreviewPoseField(glassesSettingsProperty);
            }
        }

        internal static void DrawPreviewPoseField(SerializedProperty glassesSettingsProperty)
        {
            var usePreviewPoseProperty = glassesSettingsProperty.FindPropertyRelative("usePreviewPose");
            var previewPoseProperty = glassesSettingsProperty.FindPropertyRelative("previewPose");

            usePreviewPoseProperty.boolValue = EditorGUILayout.Toggle(
                new GUIContent("Use Preview Pose",
                    "If enabled, the head pose camera pose will be set to match " +
                    "that of the Preview Pose GameObject if the glasses are no longer looking at the gameboard." +
                    System.Environment.NewLine +
                    "If disabled, it is up to the developer to set the head pose position until head tracking resumes." +
                    "It is also up to the developer to stop driving the head pose position once head tracking resumes."),
                usePreviewPoseProperty.boolValue);

            if (usePreviewPoseProperty.boolValue)
            {
                if(!previewPoseProperty.objectReferenceValue)
                {
                    EditorGUILayout.HelpBox("No Transform assigned to Preview Pose." +
                    System.Environment.NewLine + System.Environment.NewLine +
                    "If the \"Use Preview Pose\" flag is enabled, but no Transform is assigned, " +
                    "the head pose camera will not be updated at all if the glasses lose tracking.", MessageType.Warning);
                }
                EditorGUILayout.PropertyField(previewPoseProperty, new GUIContent("Preview Pose",
                    "A reference pose for the head pose camera to use while the user is looking away from the gameboard."));
            }
        }

        #endregion


        #region Readonly Fields

        private static void DrawGlassesAvailabilityLabel(SerializedProperty glassesSettingsProperty)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var friendlyNameProperty = glassesSettingsProperty.FindPropertyRelative("friendlyName");
            var friendlyName = friendlyNameProperty.stringValue;
            if(!Player.IsConnected(PlayerIndex.One) || string.IsNullOrWhiteSpace(friendlyName))
            {
                friendlyName = "N/A";
            }

            EditorGUILayout.LabelField($"Status: {(Player.IsConnected(PlayerIndex.One) ? "Ready" : "Unavailable")}");
            EditorGUILayout.LabelField($"Friendly Name: {friendlyName}");
        }

        #endregion


        #region Helper/Common Functions

        private static void DrawPreviewCameraWarning(SerializedProperty previewCameraProperty)
        {
            bool hasCamera = previewCameraProperty.objectReferenceValue;

            if (!hasCamera)
            {
                EditorGUILayout.HelpBox("Head Tracking requires an active Camera assignment. Changing the Camera assignment at runtime is not supported.", MessageType.Warning);
            }
        }

        private static void DrawOverrideFOVWarning()
        {
            EditorGUILayout.HelpBox("Overriding this value is not recommended - proceed with caution." +
                            System.Environment.NewLine + System.Environment.NewLine +
                            "Changing the FOV value affects the image projected by the glasses, " +
                            "either cropping/shrinking it while boosting sharpness, or expanding it while reducing sharpness. ",
                            MessageType.Warning);
        }

        private static void DrawMetersLabel()
        {
            var metersLabelWidth = EditorStyles.label.CalcSize(metersLabel).x;
            EditorGUILayout.LabelField(metersLabel, GUILayout.MaxWidth(metersLabelWidth), GUILayout.ExpandWidth(true));
        }

        #endregion
    }

    internal static class SettingsDrawingHelper
    {
        internal static GUIStyle lockButtonStyle = new GUIStyle(GUIStyle.none)
        {
            fixedHeight = 18,
            alignment = TextAnchor.MiddleLeft,
            contentOffset = new Vector2(1, 0)
        };
        internal static GUIStyle sharedButtonStyle = new GUIStyle(GUIStyle.none)
        {
            fixedHeight = 18,
            alignment = TextAnchor.MiddleLeft,
            contentOffset = new Vector2(0, 1)
        };
#if UNITY_2020_1_OR_NEWER
        internal static readonly GUIContent unlockedToggleContent = EditorGUIUtility.IconContent("d_Unlinked", "|Use setting from Player 1");
        internal static readonly GUIContent lockedToggleContent = EditorGUIUtility.IconContent("d_Linked", "|Using setting from Player 1");
#else
        internal static readonly GUIContent unlockedToggleContent = EditorGUIUtility.IconContent("LockIcon", "|Use setting from Player 1");
        internal static readonly GUIContent lockedToggleContent = EditorGUIUtility.IconContent("LockIcon-On", "|Using setting from Player 1");
#endif
        internal static readonly GUIContent sharedToggleContent = EditorGUIUtility.IconContent("d_UnityEditor.InspectorWindow", "|Setting is shared with one or more players");
        internal static readonly GUIContent unsharedToggleContent = new GUIContent(EditorGUIUtility.IconContent("d_UnityEditor.InspectorWindow")) { tooltip = "Setting is not being shared with any other players" };

        internal static bool DrawSettingLockToggle(bool enabled)
        {
            return GUILayout.Toggle(enabled,
                    enabled ? lockedToggleContent : unlockedToggleContent,
                    lockButtonStyle,
                    GUILayout.Width(20), GUILayout.Height(20));
        }

        internal static bool DrawSettingSharedToggle(bool shared)
        {
            return GUILayout.Toggle(true,
                    shared ? sharedToggleContent : unsharedToggleContent,
                    sharedButtonStyle,
                    GUILayout.Width(20), GUILayout.Height(20));
        }
    }

    /// <summary>
    /// These extension methods make moving values between serialized properties a little less tedious.
    /// </summary>
    internal static class SerializedObjectExtensions
    {
        internal static void TryExportInt(this SerializedProperty source, SerializedProperty destination, bool condition)
        {
            if (condition)
            {
                destination.intValue = source.intValue;
            }
        }

        internal static void TryExportFloat(this SerializedProperty source, SerializedProperty destination, bool condition)
        {
            if (condition)
            {
                destination.floatValue = source.floatValue;
            }
        }

        internal static void TryExportEnumValueIndex(this SerializedProperty source, SerializedProperty destination, bool condition)
        {
            if (condition)
            {
                destination.enumValueIndex = source.enumValueIndex;
            }
        }

        internal static void TryExportBool(this SerializedProperty source, SerializedProperty destination, bool condition)
        {
            if (condition)
            {
                destination.boolValue = source.boolValue;
            }
        }

        internal static void TryExportObjectReference(this SerializedProperty source, SerializedProperty destination, bool condition)
        {
            if (condition)
            {
                destination.objectReferenceValue = source.objectReferenceValue;
            }
        }
    }

}
