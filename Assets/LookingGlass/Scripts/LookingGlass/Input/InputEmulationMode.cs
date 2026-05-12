using System;

namespace LookingGlass {
    /// <summary>
    /// Indicates when hardware input will be emulated.
    /// </summary>
    [Serializable]
    public enum InputEmulationMode {
        /// <summary>
        /// Hardware input will never be emulated.
        /// </summary>
        Never,

        /// <summary>
        /// Hardware input will only be emulated while in the Unity editor.
        /// </summary>
        EditorOnly,

        /// <summary>
        /// Hardware input will always be emulated.
        /// </summary>
        Always
    }
}
