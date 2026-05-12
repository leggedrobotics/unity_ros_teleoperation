using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LookingGlass.Editor.EditorPropertyGroups {
    /// <summary>
    /// Represents a named collection of zero or more named properties that can be identified in an object's inspector and serialized representation.
    /// </summary>
    [Serializable]
    public class EditorPropertyGroup {
        private string groupName;
        private string tooltip;
        private GUIContent label;

        private HashSet<string> propertyNames = new HashSet<string>();

        //These fields determine the ordering of this group in the inspector:
        private bool orderBefore = false;
        private string orderReferenceProperty = null;

        private List<string> orderedPropertyNames = new List<string>();
        private bool isExpanded = false;

        public EditorPropertyGroup(string groupName, IEnumerable<string> propertyNames) : this(groupName, null, propertyNames) { }
        public EditorPropertyGroup(string groupName, string tooltip, IEnumerable<string> propertyNames) {
            if (groupName == null)
                throw new ArgumentNullException(nameof(groupName));
            this.groupName = groupName;
            this.tooltip = tooltip;
            label = new GUIContent(groupName, tooltip);

            SetProperties(propertyNames);
        }

        public EditorPropertyGroup(string groupName, IEnumerable<string> propertyNames, bool orderBefore, string orderReferenceProperty = null)
            : this(groupName, null, propertyNames, orderBefore, orderReferenceProperty) { }
        public EditorPropertyGroup(string groupName, string tooltip, IEnumerable<string> propertyNames, bool orderBefore, string orderReferenceProperty = null)
            : this(groupName, tooltip, propertyNames) {
            OrderBefore = orderBefore;
            OrderReferenceProperty = orderReferenceProperty;
        }

        public string GroupName => groupName;
        public string Tooltip => tooltip;
        public GUIContent Label => label;

        public int PropertyCount => propertyNames.Count;
        //public bool IsFullyLinked => propertyNames.Count == orderedProperties.Count;
        public bool HasProperty(string propertyName) => propertyNames.Contains(propertyName);

        public bool IsFirst => OrderBefore && !HasOrderReferenceProperty;
        public bool IsLast => !OrderBefore && !HasOrderReferenceProperty;

        /// <summary>
        /// Should this property group appear before the <see cref="OrderReferenceProperty"/> (or all fields, when the reference property is <c>null</c>)?
        /// </summary>
        public bool OrderBefore {
            get { return orderBefore; }
            set { orderBefore = value; }
        }

        /// <summary>
        /// The name of the property that comes
        /// before (<c><see cref="OrderBefore"/> = false</c>) or
        /// after (<c><see cref="OrderBefore"/> = true</c>) this group in the inspector.
        /// </summary>
        public string OrderReferenceProperty {
            get { return orderReferenceProperty; }
            set { orderReferenceProperty = value; }
        }

        public bool HasOrderReferenceProperty => !string.IsNullOrWhiteSpace(orderReferenceProperty);

        public bool IsExpanded {
            get { return isExpanded; }
            set { isExpanded = value; }
        }

        public IEnumerable<string> GetPropertiesInOrder() {
            foreach (string propertyName in orderedPropertyNames)
                yield return propertyName;
        }

        public void SetProperties(IEnumerable<string> propertyNames) {
            this.propertyNames.Clear();
            orderedPropertyNames.Clear();
            if (propertyNames == null)
                return;

            foreach (string name in propertyNames)
                this.propertyNames.Add(name);
        }

        public bool IsReferenceLinked(string propertyName) => orderedPropertyNames.FindIndex(p => p == propertyName) >= 0;
        public bool LinkPropertyOrder(string propertyName) {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));

            if (!HasProperty(propertyName))
                throw new ArgumentException(nameof(propertyName), "Unable to link property order:\n" +
                    "The property \"" + propertyName + "\" didn't exist as part of the " + nameof(EditorPropertyGroup) + " of " + groupName + "!");
            orderedPropertyNames.Add(propertyName);
            return true;
        }
    }
}
