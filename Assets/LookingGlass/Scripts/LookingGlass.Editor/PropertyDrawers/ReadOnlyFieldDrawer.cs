using UnityEngine;
using UnityEditor;

namespace LookingGlass.Editor {
    [CustomPropertyDrawer(typeof(ReadOnlyField), true)]
    public class ReadOnlyFieldDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            ReadOnlyField attr = (ReadOnlyField) attribute;
            bool readOnly = attr.IsReadOnly();

            using (new EditorGUI.DisabledScope(readOnly)) {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
