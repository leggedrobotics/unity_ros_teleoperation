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
    public class WandSettingsDrawer
    {
        public static void Draw(SerializedProperty wandSettingsProperty, ControllerIndex controllerIndex)
        {
            var wandDisplayName = "";

            switch(controllerIndex)
            {
                case ControllerIndex.Right:
                    wandDisplayName = "Right";
                    break;
                case ControllerIndex.Left:
                    wandDisplayName = "Left";
                    break;
                default:
                    Debug.Log($"Seeing a controller index of {controllerIndex}");
                    break;
            }

            var gripPointObject = wandSettingsProperty.FindPropertyRelative("GripPoint");
            var fingertipsPointObject = wandSettingsProperty.FindPropertyRelative("FingertipPoint");
            var aimPointObject = wandSettingsProperty.FindPropertyRelative("AimPoint");

            Rect wandPointsRect = EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField($"{wandDisplayName} Wand");
            ++EditorGUI.indentLevel;

            bool wandAvailable = gripPointObject.objectReferenceValue || fingertipsPointObject.objectReferenceValue || aimPointObject.objectReferenceValue;
            if (!wandAvailable)
            {
                EditorGUILayout.HelpBox($"Tracking for the {wandDisplayName} Wand requires an active GameObject assignment.", MessageType.Warning);
            }

            Rect wandGripRect = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(gripPointObject, new GUIContent("Grip Point"));
            EditorGUILayout.EndHorizontal();
            EditorGUI.LabelField(wandGripRect, new GUIContent("",
                "The GameObject driven by the wand's grip position, located at the center of the wand handle."));

            Rect wandFingertipsRect = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(fingertipsPointObject, new GUIContent("Fingertips Point"));
            EditorGUILayout.EndHorizontal();
            EditorGUI.LabelField(wandFingertipsRect, new GUIContent("",
                "The GameObject driven by the wand's fingertips position, located between the trigger and joystick."));

            Rect wandAimRect = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(aimPointObject, new GUIContent("Aim Point"));
            EditorGUILayout.EndHorizontal();
            EditorGUI.LabelField(wandAimRect, new GUIContent("",
                "The GameObject driven by the wand's aim position, located at the tip of the wand."));

            --EditorGUI.indentLevel;
            EditorGUILayout.EndVertical();    // End wandPointsRect

            DrawWandAvailableLabel(controllerIndex);
        }

        private static void DrawWandAvailableLabel(ControllerIndex controllerIndex)
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            ++EditorGUI.indentLevel;
            EditorGUILayout.LabelField($"Status: {(Input.GetWandAvailability(controllerIndex) ? "Ready" : "Unavailable")}");
            --EditorGUI.indentLevel;
        }
    }
}