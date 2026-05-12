using UnityEngine;

namespace LookingGlass {
    /// <summary>
    /// Using this attribute causes a <see cref="CameraClearFlags"/> field to look exactly like Unity's camera clear flags appears in the inspector.
    /// </summary>
    public class UnityImitatingClearFlags : PropertyAttribute {
        public UnityImitatingClearFlags() { }
    }
}
