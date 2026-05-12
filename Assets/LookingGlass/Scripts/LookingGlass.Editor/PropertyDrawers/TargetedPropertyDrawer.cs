using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace LookingGlass.Editor {
    public class TargetedPropertyDrawer<T> : PropertyDrawer where T : class {
        private T target;

        protected T Target => target;

        protected virtual void Initialize(SerializedProperty prop) {
            if (target == null) {
                string[] pathTokens = prop.propertyPath.Split('.');

                object target = prop.serializedObject.targetObject;
                foreach (var pathNode in pathTokens)
                    target = GetSerializedField(target, pathNode).GetValue(target);

                this.target = target as T;
            }
        }

        public static FieldInfo GetSerializedField(object target, string pathNode) {
            return target.GetType().GetField(pathNode, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        }
    }
}
