using UnityEditor;
using UnityEngine;

namespace LookingGlass.Editor {
    [CustomPropertyDrawer(typeof(QuiltPreset))]
    public class QuiltPresetDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            float linePlusSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            SerializedProperty deviceType = property.FindPropertyRelative(nameof(QuiltPreset.deviceType));
            SerializedProperty useCustom = property.FindPropertyRelative(nameof(QuiltPreset.useCustom));
            SerializedProperty customSettings = property.FindPropertyRelative(nameof(QuiltPreset.customSettings));

            Rect r = position;
            r.height = linePlusSpace;
            EditorGUI.PropertyField(r, property, label, false);
            r.y += linePlusSpace;

            if (property.isExpanded) {
                EditorGUI.indentLevel++;

                r = EditorGUI.IndentedRect(r);
                EditorGUI.BeginChangeCheck();
                using (new EditorGUI.DisabledScope(useCustom.boolValue))
                    EditorGUI.PropertyField(r, deviceType);
                if (EditorGUI.EndChangeCheck()) {
                    if (!useCustom.boolValue)
                        UseDefaultFromLKGDeviceType(property);
                }
                r.y += linePlusSpace;

                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(r, useCustom);
                if (EditorGUI.EndChangeCheck()) {
                    if (!useCustom.boolValue)
                        UseDefaultFromLKGDeviceType(property);
                }
                r.y += linePlusSpace;

                using (new EditorGUI.DisabledScope(!useCustom.boolValue)) {
                    r.height = position.yMax - r.yMin;
                    EditorGUI.PropertyField(r, customSettings, true);
                }
                EditorGUI.indentLevel--;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUI.GetPropertyHeight(property, label);
        }

        private void UseDefaultFromLKGDeviceType(SerializedProperty property) {
            property.serializedObject.ApplyModifiedProperties();
            QuiltPreset value = property.GetValue<QuiltPreset>();
            value.UseDefaultFrom(value.deviceType);
            property.SetValue(value);
            property.serializedObject.Update();
        }
    }
}
