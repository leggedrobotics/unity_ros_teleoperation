using UnityEngine;
using UnityEditor;
using LookingGlass.Toolkit;

namespace LookingGlass.Editor {
    [CustomPropertyDrawer(typeof(QuiltSettings))]
    public class QuiltSettingsDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            string rootPath = property.propertyPath;

            SerializedProperty child = property.Copy();
            
            Rect current = position;
            current.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.BeginProperty(current, label, property);
            property.isExpanded = EditorGUI.Foldout(current, property.isExpanded, label);
            EditorGUI.EndProperty();

            if (property.isExpanded && child.NextVisible(true)) {
                current.y += current.height + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.indentLevel++;
                current = EditorGUI.IndentedRect(current);
                current.height = 0;
                do {
                    GUIContent childLabel = new(child.displayName, child.tooltip);
                    current.height = EditorGUI.GetPropertyHeight(child, childLabel, true);

                    EditorGUI.PropertyField(current, child, true);
                    Validate(property, child);

                    current.y += current.height + EditorGUIUtility.standardVerticalSpacing;
                } while (child.NextVisible(false) && child.propertyPath.Contains(rootPath));

                if (CheckForWarning(property)) {
                    current.height = GetWarningHeight();
                    EditorGUI.HelpBox(current, GetWarningMessage(), MessageType.Warning);
                    current.y += current.height + EditorGUIUtility.standardVerticalSpacing;
                }

                EditorGUI.indentLevel--;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            float height = 0;
            height += EditorGUI.GetPropertyHeight(property, label);

            if (CheckForWarning(property))
                height += GetWarningHeight() + EditorGUIUtility.standardVerticalSpacing;

            return height;
        }

        private void Validate(SerializedProperty parent, SerializedProperty property) {
            switch (property.name) {
                case nameof(QuiltSettings.renderAspect):
                    if (property.floatValue < 0.01f)
                        property.floatValue = 0.01f;
                    break;
                case nameof(QuiltSettings.quiltWidth):
                case nameof(QuiltSettings.quiltHeight):
                    int quiltSize = property.intValue;
                    if (quiltSize < QuiltSettings.MinSize)
                        property.intValue = QuiltSettings.MinSize;
                    else if (quiltSize > QuiltSettings.MaxSize)
                        property.intValue = QuiltSettings.MaxSize;
                    break;
                case nameof(QuiltSettings.columns):
                case nameof(QuiltSettings.rows):
                    int rowOrColumns = property.intValue;
                    if (rowOrColumns < QuiltSettings.MinRowColumnCount)
                        property.intValue = QuiltSettings.MinRowColumnCount;
                    if (rowOrColumns > QuiltSettings.MaxRowColumnCount)
                        property.intValue = QuiltSettings.MaxRowColumnCount;
                    break;
                case nameof(QuiltSettings.tileCount):
                    int tileCount = property.intValue;
                    int max = parent.FindPropertyRelative(nameof(QuiltSettings.columns)).intValue * parent.FindPropertyRelative(nameof(QuiltSettings.rows)).intValue;
                    if (tileCount < 1)
                        property.intValue = 1;
                    else if (tileCount > max)
                        property.intValue = max;
                    break;
            }
        }

        private float GetWarningHeight() => 2 * EditorGUIUtility.singleLineHeight;
        private string GetWarningMessage() => "The given quilt setting's tile count differs from the columns x rows.";
        private bool CheckForWarning(SerializedProperty property) {
            int tileCount = property.FindPropertyRelative(nameof(QuiltSettings.tileCount)).intValue;
            int maxTiles = property.FindPropertyRelative(nameof(QuiltSettings.columns)).intValue * property.FindPropertyRelative(nameof(QuiltSettings.rows)).intValue;

            return tileCount != maxTiles;
        }
    }
}
