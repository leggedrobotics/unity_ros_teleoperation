using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;

namespace LookingGlass.Editor.EditorPropertyGroups {
    /// <summary>
    /// Represents a callback that can draw custom GUI for a serialized property field.
    /// </summary>
    /// <param name="property">The property to draw in the inspector.</param>
    /// <returns><c>true</c> when custom gui was drawn, <c>false</c> when nothing was drawn and the default editor GUI should be used as a fallback instead.</returns>
    public delegate bool SerializedPropertyGUIInterception(SerializedProperty property);

    /// <summary>
    /// Contains helper methods for drawing otherwise-default Unity editor GUI, with support for one level of <see cref="EditorPropertyGroup"/>s.
    /// </summary>
    public static class SectionGUILayout {
        public static void DrawDefaultInspectorWithSections(SerializedObject obj, EditorPropertyGroup[] groups = null, SerializedPropertyGUIInterception interception = null) {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (groups == null) {
                SerializedProperty property = obj.GetIterator();
                property.NextVisible(true);
                while (property.NextVisible(false)) {
                    bool skip = false;
                    if (interception != null) {
                        try {
                            skip = interception(property);
                        } catch (Exception e) {
                            Debug.LogException(e);
                        }
                    }
                    if (!skip)
                        EditorGUILayout.PropertyField(property, true);
                }
            } else {
                //NOTE: Not a List<SerializedProperty>, because I think Unity re-uses the class instances (so the list would get corrupted easily with the same modified instance everywhere!)
                //And we'd probably get worse performance for deep copying a lot of SerializedProperty objects
                HashSet<string> grouplessPropertyNames = new HashSet<string>();

                List<int> insertGroupIndices = null;
                int groupIndex = 0;
                insertGroupIndices = new List<int>(groups.Length);

                groupIndex = BubbleFirstGroups(groups);
                for (int i = 0; i < groupIndex; i++)
                    insertGroupIndices.Add(0);

                SerializedProperty property = obj.GetIterator();
                property.NextVisible(true);
                while (property.NextVisible(false)) {
                    string propertyName = property.name;

                    for (int i = groupIndex; i < groups.Length; i++) {
                        if (groups[i].OrderReferenceProperty == propertyName &&
                            groups[i].OrderBefore) {
                            insertGroupIndices.Add(grouplessPropertyNames.Count);
                            if (i > groupIndex)
                                Swap(groups, i, groupIndex);
                            groupIndex++;
                        }
                    }

                    bool isPropertyInGroup = false;
                    foreach (EditorPropertyGroup group in groups) {
                        if (group.HasProperty(propertyName)) {
                            isPropertyInGroup = true;
                            if (!group.IsReferenceLinked(propertyName))
                                group.LinkPropertyOrder(propertyName);
                        }
                    }
                    if (!isPropertyInGroup)
                        grouplessPropertyNames.Add(propertyName);

                    for (int i = groupIndex; i < groups.Length; i++) {
                        if (groups[i].OrderReferenceProperty == propertyName &&
                            !groups[i].OrderBefore) {
                            insertGroupIndices.Add(grouplessPropertyNames.Count);
                            if (i > groupIndex)
                                Swap(groups, i, groupIndex);
                            groupIndex++;
                        }
                    }
                }
                for (int i = groupIndex; i < groups.Length; i++)
                    insertGroupIndices.Add(grouplessPropertyNames.Count);

                groupIndex = 0;

                int propertyIndex = 0;
                void CheckToInsertGroups(ref int index) {
                    while (index < insertGroupIndices.Count && insertGroupIndices[index] <= propertyIndex)
                        DrawSection(obj, groups[index++], interception);
                }
                CheckToInsertGroups(ref groupIndex);

                property = obj.GetIterator();
                property.NextVisible(true);
                while (property.NextVisible(false)) {
                    if (!grouplessPropertyNames.Contains(property.name))
                        continue;

                    bool skip = false;
                    if (interception != null) {
                        try {
                            skip = interception(property);
                        } catch (Exception e) {
                            Debug.LogException(e);
                        }
                    }
                    if (!skip)
                        EditorGUILayout.PropertyField(property, true);

                    propertyIndex++;
                    CheckToInsertGroups(ref groupIndex);
                }
            }
        }

        /// <summary>
        /// Moves up all of the groups whose <c><see cref="EditorPropertyGroup.IsFirst"/> = true</c> to the front of the array.
        /// </summary>
        /// <returns>
        /// The total count of groups whose <c><see cref="EditorPropertyGroup.IsFirst"/> = true</c> that are at the front of the array.
        /// </returns>
        private static int BubbleFirstGroups(EditorPropertyGroup[] groups) {
            Assert.IsNotNull(groups);

            int countOfFirstGroups = 0;
            for (int i = 0; i < groups.Length && groups[i].IsFirst; i++)
                countOfFirstGroups++;

            int j = countOfFirstGroups;
            for (int i = countOfFirstGroups + 1; i < groups.Length; i++) {
                if (groups[i].IsFirst)
                    Swap(groups, i, j++);
            }
            return j;
        }

        private static void Swap<T>(T[] array, int indexA, int indexB) {
            Assert.IsNotNull(array);
            T temp = array[indexA];
            array[indexA] = array[indexB];
            array[indexB] = temp;
        }

        private static void DrawSection(SerializedObject obj, EditorPropertyGroup group, SerializedPropertyGUIInterception interception = null) {
            Assert.IsNotNull(obj);
            Assert.IsNotNull(group);

            group.IsExpanded = EditorGUILayout.Foldout(group.IsExpanded, group.Label, true);
            if (group.IsExpanded) {
                try {
                    EditorGUI.indentLevel++;

                    foreach (string propertyName in group.GetPropertiesInOrder()) {
                        SerializedProperty property = obj.FindProperty(propertyName);

                        bool skip = false;
                        if (interception != null) {
                            try {
                                skip = interception(property);
                            } catch (Exception e) {
                                Debug.LogException(e);
                            }
                        }
                        if (!skip)
                            EditorGUILayout.PropertyField(property, true);
                    }
                } finally {
                    EditorGUI.indentLevel--;
                }
            }
        }
    }
}
