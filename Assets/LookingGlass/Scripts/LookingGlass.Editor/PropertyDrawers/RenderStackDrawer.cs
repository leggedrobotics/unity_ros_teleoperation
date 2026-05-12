using UnityEngine;
using UnityEditor;

namespace LookingGlass.Editor {
    [CustomPropertyDrawer(typeof(RenderStack))]
    public class RenderStackDrawer : PropertyDrawer {
        private const bool AutoEnableNewRenderSteps = true;
        private GUIContent quiltMixLabel;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            Rect currentRect = position;
            currentRect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.BeginProperty(position, label, property);
            property.isExpanded = EditorGUI.Foldout(currentRect, property.isExpanded, label, true);
            if (property.isExpanded) {
                EditorGUI.indentLevel++;

                //Quilt Mix:
                RenderTexture quiltMix = property.GetValue<RenderStack>().QuiltMix;
                currentRect.y = currentRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                currentRect.height = EditorGUIUtility.singleLineHeight;

                if (quiltMixLabel == null)
                    quiltMixLabel = new GUIContent("Quilt Mix", "The final render result that combines " +
                        "all of the render steps in the stack, in the order shown in the inspector.");
                using (new EditorGUI.DisabledScope(true))
                    EditorGUI.ObjectField(currentRect, quiltMixLabel, quiltMix, typeof(RenderTexture), true);

                //Filter Mode:
                SerializedProperty filterMode = property.FindPropertyRelative(nameof(RenderStack.filterMode));
                currentRect.y = currentRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                currentRect.height = EditorGUI.GetPropertyHeight(filterMode);
                EditorGUI.PropertyField(currentRect, filterMode);

                //Anti-Aliasing Strength:
                if (filterMode.enumValueFlag == (int) QuiltFilterMode.PointVirtualPixelAA) {
                    SerializedProperty antiAliasingStrength = property.FindPropertyRelative(nameof(RenderStack.antiAliasingStrength));
                    currentRect.y = currentRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                    currentRect.height = EditorGUI.GetPropertyHeight(antiAliasingStrength);

                    EditorGUI.PropertyField(currentRect, antiAliasingStrength);
                }

                //Bypass AspectAdjustment:
#if LKG_ASPECT_ADJUSTMENT
                SerializedProperty bypassAspectAdjustment = property.FindPropertyRelative(nameof(RenderStack.bypassAspectAdjustment));
                currentRect.y = currentRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                currentRect.height = EditorGUI.GetPropertyHeight(bypassAspectAdjustment);
                EditorGUI.PropertyField(currentRect, bypassAspectAdjustment);
#endif

                //Steps:
                SerializedProperty steps = property.FindPropertyRelative(nameof(RenderStack.steps));
                int prevCount = steps.arraySize;
                currentRect.y = currentRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                currentRect.height = EditorGUI.GetPropertyHeight(steps);

                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(currentRect, steps, new GUIContent(steps.displayName, steps.tooltip), true);
                if (EditorGUI.EndChangeCheck()) {
                    if (AutoEnableNewRenderSteps) {
                        for (int i = prevCount; i < steps.arraySize; i++)
                            steps.GetArrayElementAtIndex(i).FindPropertyRelative("isEnabled").boolValue = true;
                    }
                }
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            SerializedProperty filterMode = property.FindPropertyRelative(nameof(RenderStack.filterMode));
            SerializedProperty antiAliasingStrength = property.FindPropertyRelative(nameof(RenderStack.antiAliasingStrength));
#if LKG_ASPECT_ADJUSTMENT
            SerializedProperty bypassAspectAdjustment = property.FindPropertyRelative(nameof(RenderStack.bypassAspectAdjustment));
#endif
            SerializedProperty steps = property.FindPropertyRelative(nameof(RenderStack.steps));

            return
                EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing +
                ((property.isExpanded) ? (
                    EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing +
                    EditorGUI.GetPropertyHeight(filterMode) + EditorGUIUtility.standardVerticalSpacing +
                    ((filterMode.enumValueFlag == (int) QuiltFilterMode.PointVirtualPixelAA) ?
                        EditorGUI.GetPropertyHeight(antiAliasingStrength) + EditorGUIUtility.standardVerticalSpacing : 0) +
#if LKG_ASPECT_ADJUSTMENT
                    EditorGUI.GetPropertyHeight(bypassAspectAdjustment) + EditorGUIUtility.standardVerticalSpacing +
#endif
                    EditorGUI.GetPropertyHeight(steps) + EditorGUIUtility.standardVerticalSpacing
                ) : 0);
        }
    }
}
