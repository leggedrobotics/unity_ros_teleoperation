using System;

namespace LookingGlass {
    /// <summary>
    /// Describes which render pipeline is currently being used for a Unity project.
    /// </summary>
    [Serializable]
    public enum RenderPipelineType {
        /// <summary>
        /// The render pipeline asset in-use is not supported by the LookingGlass Unity Plugin.
        /// </summary>
        Unsupported = -1,

        /// <summary>
        /// There is no render pipeline asset, so Unity's built-in render pipeline is being used.
        /// </summary>
        BuiltIn = 0,

        /// <summary>
        /// Unity's HDRP (High-Definition Render Pipeline) is being used.
        /// </summary>
        HDRP = 1,

        /// <summary>
        /// Unity's URP (Universal Render Pipeline) is being used.
        /// </summary>
        URP = 2
    }
}
