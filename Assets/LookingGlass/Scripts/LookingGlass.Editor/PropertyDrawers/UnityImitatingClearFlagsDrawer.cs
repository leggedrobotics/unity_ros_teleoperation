using System;
using UnityEngine;
using UnityEditor;

namespace LookingGlass.Editor {
    [CustomPropertyDrawer(typeof(UnityImitatingClearFlags), true)]
    public class UnityImitatingClearFlagsDrawer : PropertyDrawer {
        private struct ClearFlagsNamingInfo {
            public GUIContent[] displayContents;
            public CameraClearFlags[] correspondingClearFlags;
        }

        //NOTE: I couldn't find anywhere where Unity has these custom strings in the UnityEditor C# reference.. so I guess we'll
        //have to hard-code these values in for now, as our own custom logic to imitate exactly how Unity shows a Camera component's clear flags in the inspector.
        private static readonly Lazy<ClearFlagsNamingInfo> Info = new Lazy<ClearFlagsNamingInfo>(() => new ClearFlagsNamingInfo() {
            displayContents = new GUIContent[] {
                //NOTE: Tooltips copied from UnityEngine comments
                new GUIContent("Skybox", "Clear with the skybox"),
                new GUIContent("Solid Color", "Clear with a background color."),
                new GUIContent("Depth only", "Clear only the depth buffer."),
                new GUIContent("Nothing", "Don't clear anything.")
            },
            correspondingClearFlags = new CameraClearFlags[] {
                CameraClearFlags.Skybox,
                CameraClearFlags.SolidColor,
                CameraClearFlags.Depth,
                CameraClearFlags.Nothing
            }
        });

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            ClearFlagsNamingInfo info = Info.Value;
            int index = Array.IndexOf(info.correspondingClearFlags, (CameraClearFlags) property.intValue);
            if (index < 0) {
                Debug.LogError("Failed to find " + property.intValue + " camera clear flags value!");
            } else {
                index = EditorGUI.Popup(position, label, index, info.displayContents);
                property.intValue = (int) info.correspondingClearFlags[index];
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
