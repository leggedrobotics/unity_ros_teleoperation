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
    public class LogSettingsDrawer
    {
        private static GUIContent[] logLevelOptions = {
                    new GUIContent("VERBOSE"),
                    new GUIContent("DEBUG"),
                    new GUIContent("INFO"),
                    new GUIContent("WARN"),
                    new GUIContent("ERROR"),
                    new GUIContent("DISABLED"),};
        public static void Draw(SerializedProperty logSettingsProperty)
        {
            var logLevelProperty = logSettingsProperty.FindPropertyRelative("level");
            var logTagProperty = logSettingsProperty.FindPropertyRelative("TAG");

            ++EditorGUI.indentLevel;
            Rect logTagRect = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(logTagProperty, new GUIContent("TAG"));
            EditorGUILayout.EndHorizontal();
            EditorGUI.LabelField(logTagRect, new GUIContent("",
                "The logging TAG prefixed to each log message."));

            Rect logLevelRect = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Level");
            GUILayout.Space(-20);
            logLevelProperty.intValue = EditorGUILayout.Popup(logLevelProperty.intValue, logLevelOptions);
            EditorGUILayout.EndHorizontal();
            EditorGUI.LabelField(logLevelRect, new GUIContent("",
                "The logging level."));
            --EditorGUI.indentLevel;
        }
    }
}