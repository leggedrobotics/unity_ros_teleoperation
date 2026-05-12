using System;
using UnityEngine;
using UnityEditor;

namespace LookingGlass.Editor {
    [CustomPropertyDrawer(typeof(RenderStep))]
    public class RenderStepDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            try {
                SerializedProperty renderTypeProperty = property.FindPropertyRelative("renderType");
                RenderStep.Type renderType = (RenderStep.Type) renderTypeProperty.enumValueIndex;

                GUIContent renamedLabel = new GUIContent(label.text.Replace("Element", "Render Step"), label.tooltip);

                Rect firstLine = position;
                firstLine.height = EditorGUIUtility.singleLineHeight;

                SerializedProperty isEnabledProperty = property.FindPropertyRelative("isEnabled");
                GUIContent isEnabledLabel = new GUIContent(isEnabledProperty.displayName, isEnabledProperty.tooltip);

                bool isEnabled = isEnabledProperty.boolValue;

                //TODO: Figure out this math for any arbitrary indent level...
                //It *just* works for our cases for now..

                //More info:
                //So basically, when we draw this property drawer given the circumstances of the LookingGlass inspector,
                //The render step fields look great.
                //BUT, if we draw this property drawer at different indent levels, the alignment of things get messed up.
                //This is mostly because it was hard to draw a collapse/expand arrow AND a toggle checkbox
                //right next to each other on the same line.

                //I haven't figured out the math to make it work at any indent level yet, but this is a nice-to-have
                //Because we don't use the LookingGlass.RenderStep fields anywhere else other than in LookingGlass.RenderStack right now.
                Rect headerRect1 = firstLine;
                headerRect1.width = 16;

                Rect checkboxRect = firstLine;
                checkboxRect.xMin = headerRect1.xMax - 8;
                checkboxRect.width = 32;

                Rect headerRect2 = firstLine;
                headerRect2.xMin = checkboxRect.xMax + 12;
                headerRect2.width = Mathf.Min(EditorGUIUtility.labelWidth - 52 /*8 + 20 + 32*/, headerRect2.width);

                using (new EditorGUI.DisabledScope(!isEnabled)) {
                    EditorGUI.BeginProperty(Rect.MinMaxRect(headerRect1.xMin, headerRect1.yMin, headerRect2.xMax, headerRect2.yMax), renamedLabel, property);
                    property.isExpanded = EditorGUI.Foldout(headerRect1, property.isExpanded, GUIContent.none, true);

                    headerRect2.width = Mathf.Min(headerRect2.width, EditorGUIUtility.labelWidth);

                    DoWithZeroIndent(() => {
                        property.isExpanded = EditorGUI.Foldout(headerRect2, property.isExpanded, renamedLabel, true, GUI.skin.label);
                    });
                    EditorGUI.EndProperty();
                }

                EditorGUI.BeginChangeCheck();
                EditorGUI.BeginProperty(checkboxRect, GUIContent.none, isEnabledProperty);
                isEnabled = EditorGUI.ToggleLeft(checkboxRect, GUIContent.none, isEnabled);
                EditorGUI.EndProperty();
                if (EditorGUI.EndChangeCheck())
                    isEnabledProperty.boolValue = isEnabled;

                if (property.isExpanded) {
                    using (new EditorGUI.DisabledScope(!isEnabled)) {
                        EditorGUI.indentLevel++;

                        Rect renderTypeRect = position;
                        renderTypeRect.y += firstLine.height + EditorGUIUtility.standardVerticalSpacing;
                        renderTypeRect.height = EditorGUIUtility.singleLineHeight;

                        if (renderType != RenderStep.Type.CurrentHologramCamera)
                            renderTypeRect.width = Mathf.Max(90 + EditorGUIUtility.labelWidth, 0.5f * position.width);

                        GUIContent renderTypeLabel = new GUIContent(renderTypeProperty.displayName, renderTypeProperty.tooltip);

                        EditorGUI.BeginChangeCheck();
                        EditorGUI.BeginProperty(renderTypeRect, renderTypeLabel, renderTypeProperty);
                        renderType = (RenderStep.Type) EditorGUI.EnumPopup(renderTypeRect, renderTypeLabel, renderType);
                        EditorGUI.EndProperty();
                        if (EditorGUI.EndChangeCheck())
                            renderTypeProperty.enumValueIndex = (int) renderType;

                        switch (renderType) {
                            case RenderStep.Type.CurrentHologramCamera:
                                break;
                            case RenderStep.Type.Quilt:
                                Rect quiltRect = renderTypeRect;
                                quiltRect.xMax = position.xMax;
                                quiltRect.xMin = renderTypeRect.xMax;
                                int prev = EditorGUI.indentLevel;

                                SerializedProperty quiltProperty = property.FindPropertyRelative("quiltTexture");
                                DoWithZeroIndent(() => {
                                    EditorGUI.ObjectField(quiltRect, quiltProperty, GUIContent.none);
                                });

                                SerializedProperty quiltSettingsProperty = property.FindPropertyRelative("quiltSettings");
                                Rect quiltSettingsRect = new Rect(renderTypeRect.x, renderTypeRect.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUI.GetPropertyHeight(quiltSettingsProperty));
                                EditorGUI.PropertyField(quiltSettingsRect, quiltSettingsProperty, new GUIContent(quiltSettingsProperty.displayName, quiltSettingsProperty.tooltip), true);

                                SerializedProperty postProcessCameraProperty = property.FindPropertyRelative("postProcessCamera");
                                Rect postProcessCameraRect = new Rect(quiltSettingsRect.x, quiltSettingsRect.yMax + EditorGUIUtility.standardVerticalSpacing, quiltSettingsRect.width, EditorGUIUtility.singleLineHeight);
                                EditorGUI.PropertyField(postProcessCameraRect, postProcessCameraProperty, new GUIContent(postProcessCameraProperty.displayName, postProcessCameraProperty.tooltip));
                                break;
                            case RenderStep.Type.GenericTexture:
                                Rect textureRect = renderTypeRect;
                                textureRect.xMax = position.xMax;
                                textureRect.xMin = renderTypeRect.xMax;

                                SerializedProperty textureProperty = property.FindPropertyRelative("texture");
                                DoWithZeroIndent(() => {
                                    EditorGUI.ObjectField(textureRect, textureProperty, GUIContent.none);
                                });
                                break;
                        }
                        EditorGUI.indentLevel--;
                    }
                }
                property.serializedObject.ApplyModifiedProperties();
            } catch (ExitGUIException) { }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            float lineHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            int lineCount = 1;
            float extraHeight = 0;

            if (property.isExpanded) {
                RenderStep.Type renderType = (RenderStep.Type) property.FindPropertyRelative("renderType").enumValueIndex;

                switch (renderType) {
                    case RenderStep.Type.CurrentHologramCamera:
                        lineCount++;
                        break;
                    case RenderStep.Type.Quilt:
                        extraHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("quiltSettings")) + EditorGUIUtility.standardVerticalSpacing;
                        lineCount += 2;
                        break;
                    case RenderStep.Type.GenericTexture:
                        lineCount++;
                        break;
                }
            }

            return lineCount * lineHeight + extraHeight;
        }

        private void DoWithZeroIndent(Action action) {
            int prev = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            try {
                action();
            } finally {
                EditorGUI.indentLevel = prev;
            }
        }
    }
}
