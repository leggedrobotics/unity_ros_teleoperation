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
    public class GraphicsSettingsDrawer : MonoBehaviour
    {
        // Match Glasses Framerate toggle
        private const string MATCH_GLASSES_FRAMERATE_TOGGLE_LABEL = "Optimize Framerate";
        private const string MATCH_GLASSES_FRAMERATE_TOGGLE_TOOLTIP =
            "Limits the application's framerate to the glasses-preferred 60 frames per second when a pair of glasses is connected.\r\n\r\n" +
            "This ensures that a stable stream of frames reaches the glasses and helps to prevent animations from stuttering.\r\n\r\n" +
            "This also disables VSync, so frames don't need to halt and wait for the player's monitor to refresh before being sent to the glasses.";

        public static void Draw(SerializedProperty graphicsSettingsProperty)
        {
            var restrictFrameratePropety = graphicsSettingsProperty.FindPropertyRelative("matchGlassesFramerate");
            var matchGlassesFramerateLabel = new GUIContent(MATCH_GLASSES_FRAMERATE_TOGGLE_LABEL, MATCH_GLASSES_FRAMERATE_TOGGLE_TOOLTIP);

            restrictFrameratePropety.boolValue = EditorGUILayout.Toggle(
                matchGlassesFramerateLabel,
                restrictFrameratePropety.boolValue);
        }
    }
}