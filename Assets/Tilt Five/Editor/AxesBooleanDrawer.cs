/*
 * Copyright (C) 2020-2023 Tilt Five, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using UnityEngine;
using UnityEditor;

namespace TiltFive {

	[CustomPropertyDrawer(typeof(TiltFive.AxesBoolean))]
	public class AxesBooleanDrawer : PropertyDrawer
	{
		// Draw the property inside the given rect
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// Using BeginProperty / EndProperty on the parent property means that
			// prefab override logic works on the entire property.
			EditorGUI.BeginProperty(position, label, property);

			// Draw label
			position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

			// Don't make child fields be indented
			var indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			Rect rect = position;
			// Calculate rects
			float a = 18;
			float b = 26;
			rect.width = a;
			EditorGUI.PropertyField(rect, property.FindPropertyRelative("x"), GUIContent.none);
			rect.x += rect.width;
			rect.width = b;
			EditorGUI.LabelField (rect, "X");
			rect.x += rect.width;
			rect.width = a;
			EditorGUI.PropertyField(rect, property.FindPropertyRelative("y"), GUIContent.none);
			rect.x += rect.width;
			rect.width = b;
			EditorGUI.LabelField (rect, "Y");
			rect.x += rect.width;
			rect.width = a;
			EditorGUI.PropertyField(rect, property.FindPropertyRelative("z"), GUIContent.none);
			rect.x += rect.width;
			rect.width = b;
			EditorGUI.LabelField (rect, "Z");

			// Set indent back to what it was
			EditorGUI.indentLevel = indent;

			EditorGUI.EndProperty();
		}
	}
}