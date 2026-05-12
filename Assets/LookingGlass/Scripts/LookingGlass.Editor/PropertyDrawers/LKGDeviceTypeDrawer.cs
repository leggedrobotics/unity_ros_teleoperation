using System;
using System.Linq;
using LookingGlass.Toolkit;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace LookingGlass.Editor {
    [CustomPropertyDrawer(typeof(LKGDeviceType))]
    public class LKGDeviceTypeDrawer : PropertyDrawer {
        private static Dictionary<int, LKGDeviceType> indexToEnumLookup = new();
        private static Dictionary<LKGDeviceType, int> enumToIndexLookup = new();
        private static LKGDeviceType[] shortenedList;
        private static GUIContent[] displayNames;
        private static GUIContent[] DisplayValues {
            get {
                if (displayNames == null) {
                    LKGDeviceType[] sortedValues = ((LKGDeviceType[]) Enum.GetValues(typeof(LKGDeviceType)))
                        .OrderBy(v => (int) v)
                        .ToArray();

                    for (int i = 0; i < sortedValues.Length; i++) {
                        indexToEnumLookup.Add(i, sortedValues[i]);
                        enumToIndexLookup.Add(sortedValues[i], i);
                    }

                    List<LKGDeviceType> list = new();
                    List<GUIContent> labels = new();
                    foreach (LKGDeviceType deviceType in sortedValues) {
                        if (QuiltSettings.GetDefaultFor(deviceType).IsDefaultOrBlank)
                            continue;
                        list.Add(deviceType);
                        labels.Add(new GUIContent(deviceType.GetNiceName()));
                    }
                    shortenedList = list.ToArray();
                    displayNames = labels.ToArray();
                }
                return displayNames;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            GUIContent[] labels = DisplayValues;

            int shortenedListIndex = 0;
            LKGDeviceType current = indexToEnumLookup[property.enumValueIndex];
            for (int i = 0; i < shortenedList.Length; i++) {
                if (shortenedList[i] == current) {
                    shortenedListIndex = i;
                    break;
                }
            }
            shortenedListIndex = EditorGUI.Popup(position, label, shortenedListIndex, labels);
            if (EditorGUI.EndChangeCheck()) {
                property.enumValueIndex = enumToIndexLookup[shortenedList[shortenedListIndex]];
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUI.GetPropertyHeight(property, label);
        }
    }
}
