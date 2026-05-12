using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LookingGlass.Editor {
    [CustomPropertyDrawer(typeof(OutputFolder))]
    internal class OutputFolderDrawer : TargetedPropertyDrawer<OutputFolder> {
        private SerializedProperty rootProperty;
        private SerializedProperty valueProperty;
        private SerializedProperty forceAssetFolder;

        protected override void Initialize(SerializedProperty property) {
            base.Initialize(property);

            if (rootProperty == null)
                rootProperty = property.FindPropertyRelative(nameof(OutputFolder.root));
            if (valueProperty == null)
                valueProperty = property.FindPropertyRelative(nameof(OutputFolder.value));
            if (forceAssetFolder == null)
                forceAssetFolder = property.FindPropertyRelative(nameof(OutputFolder.forceAssetFolder));
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            Initialize(property);

            label.tooltip = "The path to the folder where the Recorder saves the output files";
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Keyboard), label);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            const float rootWidth = 110.0f;
            const float buttonWidth = 30.0f;

            float leafWidth = Target.ForceAssetsFolder ? position.width - rootWidth : position.width - rootWidth - buttonWidth - 10;
            Rect rootRect = new Rect(position.x, position.y, rootWidth, position.height);
            Rect valueRect = new Rect(position.x + rootWidth + 5, position.y, leafWidth, position.height);
            Rect buttonRect = new Rect(position.x + rootWidth + leafWidth + 10, position.y, buttonWidth, position.height);

            OutputFolder.DirectoryRootType pathType = (OutputFolder.DirectoryRootType) rootProperty.intValue;
            if (Target.ForceAssetsFolder) {
                GUI.Label(rootRect, pathType + " " + Path.DirectorySeparatorChar);
            } else {
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(rootRect, rootProperty, GUIContent.none);
                if (EditorGUI.EndChangeCheck()) {
                    if (rootProperty.intValue == (int) OutputFolder.DirectoryRootType.Absolute)
                        valueProperty.stringValue = PathUtil.SanitizeFilePath(Path.GetFullPath(valueProperty.stringValue));
                    else
                        valueProperty.stringValue = OutputFolder.FromPath(valueProperty.stringValue).Value;
                }
            }

            EditorGUI.PropertyField(valueRect, valueProperty); // Show the absolute path
            valueProperty.stringValue = (EditorGUI.TextField(valueRect, valueProperty.stringValue)); // Show the leaf

            string fullPath = OutputFolder.GetFullPath((OutputFolder.DirectoryRootType) rootProperty.intValue, valueProperty.stringValue);
            if (!Target.ForceAssetsFolder) {
                if (GUI.Button(buttonRect, new GUIContent("...", "Select the output location through your file browser"))) {
                    string newPath = EditorUtility.OpenFolderPanel("Select output location", fullPath, "");
                    if (!string.IsNullOrEmpty(newPath)) {
                        OutputFolder newValue = OutputFolder.FromPath(newPath);
                        rootProperty.intValue = (int) newValue.Root;
                        valueProperty.stringValue = newValue.Value;
                    }
                }
            }

            EditorGUI.indentLevel = indent;
            try {
                EditorGUI.EndProperty();
            } catch (InvalidOperationException) {
                //UNITY BUG
            }
        }
    }
}
