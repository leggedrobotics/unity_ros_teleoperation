using UnityEngine;

namespace LookingGlass {
    /// <summary>
    /// Prevents this field from being changed in Unity's inspector.<br />
    /// Instead of being editable, the field will be drawn with disabled editor GUI.
    /// </summary>
    public class ReadOnlyField : PropertyAttribute {
        public ReadOnlyField() { }

        public virtual bool IsReadOnly() => true;
    }
}
