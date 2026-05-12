using UnityEngine;
using UnityEditor;

namespace LookingGlass.Editor {
    [CustomPropertyDrawer(typeof(SemanticVersion), true)]
    public class SemanticVersionDrawer : PropertyDrawer {
        private static float HelpBoxHeight => 2.5f * EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            SemanticVersion version = property.GetValue<SemanticVersion>();
            using (new EditorGUI.DisabledScope(version.IsReadOnly)) {
                SerializedProperty valueProperty = property.FindPropertyRelative(nameof(SemanticVersion.value));

                Rect valueRect = position;
                valueRect.height = EditorGUI.GetPropertyHeight(valueProperty);

                if (!version.IsValid) {
                    Rect warningRect = position;
                    warningRect.height = HelpBoxHeight;
                    valueRect.y += warningRect.height + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.HelpBox(warningRect, "Invalid version syntax!\nThe value should be in the form: \"[v]major.minor.patch[-label].\"", MessageType.Warning);
                }

                EditorGUI.PropertyField(valueRect, valueProperty, label);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            SemanticVersion version = property.GetValue<SemanticVersion>();
            SerializedProperty valueProperty = property.FindPropertyRelative(nameof(SemanticVersion.value));

            if (!version.IsValid)
                return EditorGUI.GetPropertyHeight(valueProperty) + EditorGUIUtility.standardVerticalSpacing
                    + HelpBoxHeight + EditorGUIUtility.standardVerticalSpacing;
            return EditorGUI.GetPropertyHeight(valueProperty) + EditorGUIUtility.standardVerticalSpacing;
        }
    }
}