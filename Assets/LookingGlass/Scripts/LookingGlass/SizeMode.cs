using System;

namespace LookingGlass {
    /// <summary>
    /// Determines how the LookingGlass camera size is affected.
    /// </summary>
    [Serializable]
    public enum SizeMode {
        /// <summary>
        /// The camera size is set manually, and does not affect nor get affected by transform scaling.
        /// </summary>
        Manual = 0,

        /// <summary>
        /// The camera size sets the <see cref="HologramCamera"/> transform's local scale.
        /// </summary>
        SizeSetsScale = 1,

        /// <summary>
        /// The local scale of the <see cref="HologramCamera"/>'s transform sets the camera size.<br />
        /// If the scaling is non-uniform, the largest scaling axis will be used.
        /// </summary>
        ScaleSetsSize = 2
    }
}
