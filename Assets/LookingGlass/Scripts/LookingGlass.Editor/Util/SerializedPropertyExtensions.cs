//Thanks to helpful resource at https://github.com/lordofduct/spacepuppy-unity-framework/blob/master/SpacepuppyBaseEditor/EditorHelper.cs

using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;

namespace LookingGlass.Editor {
    public static class SerializedPropertyExtensions {
        public static T GetValue<T>(this SerializedProperty property) {
            object value = property.GetValue();

            if (value == null)
                return default;
            if (!(value is T specificValue))
                throw new InvalidCastException("Expected a type of " + typeof(T).Name + ", but received " + value.GetType().Name + " instead!");
            return specificValue;
        }
        public static object GetValue(this SerializedProperty property) {
            string path = property.propertyPath.Replace(".Array.data[", ".[");
            string[] fields = path.Split('.');

            //This variable is stepped down the field hierarchy until we arrive
            //at the result we want.
            //Ex:
            //  propertyPath = "stats.combatLevel.level"
            //  currentObject = CharacterStatsLink
            //      = CharacterStats
            //      = CombatLevel
            //      = int
            object currentObject = property.serializedObject.targetObject;

            foreach (string fieldName in fields) {
                bool hasArrayIndex = fieldName[fieldName.Length - 1] == ']';
                if (hasArrayIndex) {
                    int indexA = fieldName.IndexOf('[');
                    Assert.IsTrue(indexA >= 0);
                    //Skip past the '[' to the first character of the index (number)
                    int index = int.Parse(fieldName.Substring(indexA + 1, fieldName.Length - 2 - indexA)); //-2 to clip both the '[' and ']'

                    string arrayName = fieldName.Substring(0, indexA);
                    Assert.IsFalse(arrayName.Contains("["));
                    Assert.IsFalse(arrayName.Contains("]"));

                    IList array = (IList) currentObject;
                    currentObject = array[index];
                } else {
                    FieldInfo fieldInfo = currentObject.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);
                    currentObject = fieldInfo.GetValue(currentObject);
                }
            }
            return currentObject;
        }

        private static void SetValue(ref object currentObject, string[] fieldPath, int fieldIndex, object finalValue) {
            //Ex:
            //  propertyPath = "typeSpecificErrorMessages.keyValues.Array.data[2].key"
            //      --> "typeSpecificErrorMessages.keyValues.[2].key"
            //      fieldPath.Length = 3
            //          > ServerAccessViolationSystem
            //          > SerializableDictionary<SerializableType, string>
            //          > KeyValuePair
            //          > SerializableType

            string fieldName = fieldPath[fieldIndex];
            bool hasArrayIndex = fieldName[fieldName.Length - 1] == ']';
            FieldInfo fieldInfo;
            object nextObject;
            if (fieldIndex < fieldPath.Length - 1) {
                if (hasArrayIndex) {
                    int indexA = fieldName.IndexOf('[');
                    Assert.IsTrue(indexA >= 0);
                    //Skip past the '[' to the first character of the index (number)
                    int index = int.Parse(fieldName.Substring(indexA + 1, fieldName.Length - 2 - indexA)); //-2 to clip both the '[' and ']'

                    string arrayName = fieldName.Substring(0, indexA);
                    Assert.IsFalse(arrayName.Contains("["));
                    Assert.IsFalse(arrayName.Contains("]"));

                    IList array = (IList) currentObject;
                    nextObject = array[index];

                    SetValue(ref nextObject, fieldPath, fieldIndex + 1, finalValue);
                    array[index] = nextObject; //Back-set the values over themselves IN CASE we're dealing with value-types
                } else {
                    fieldInfo = currentObject.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);
                    nextObject = fieldInfo.GetValue(currentObject);

                    SetValue(ref nextObject, fieldPath, fieldIndex + 1, finalValue);
                    fieldInfo.SetValue(currentObject, nextObject); //Back-set the values over themselves IN CASE we're dealing with value-types
                }

            } else {
                if (hasArrayIndex) {
                    int indexA = fieldName.IndexOf('[');
                    Assert.IsTrue(indexA >= 0);
                    //Skip past the '[' to the first character of the index (number)
                    int index = int.Parse(fieldName.Substring(indexA + 1, fieldName.Length - 2 - indexA)); //-2 to clip both the '[' and ']'

                    string arrayName = fieldName.Substring(0, indexA);
                    Assert.IsFalse(arrayName.Contains("["));
                    Assert.IsFalse(arrayName.Contains("]"));

                    IList array = (IList) currentObject;
                    array[index] = finalValue;
                } else {
                    fieldInfo = currentObject.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);
                    fieldInfo.SetValue(currentObject, finalValue);
                }
            }

            //We just changed some of the serialized data. Just in case the data type reads/writes data differently during serialization,
            //Let's give them the callback that the SERIALIZED data was changed -- and they can regenerate their non-serialized data (which happens during OnAfterDeserialize())
            if (currentObject is ISerializationCallbackReceiver receiver)
                receiver.OnAfterDeserialize();
        }

        public static void SetValue(this SerializedProperty property, object value) {
            string path = property.propertyPath.Replace(".Array.data[", ".["); //Use a period to count this indexing [x] operation as it's own field!! 
            string[] fields = path.Split('.');

            object firstObject = property.serializedObject.targetObject;
            SetValue(ref firstObject, fields, 0, value);
        }
    }
}
