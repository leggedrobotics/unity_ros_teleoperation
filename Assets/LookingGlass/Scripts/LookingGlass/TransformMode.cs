using System;

namespace LookingGlass {
    /// <summary>
    /// Defines how the <see cref="LookingGlass.HologramCamera"/>'s transform controls the shape of the viewing volume.
    /// </summary>
    [Serializable]
    public enum TransformMode {
        /// <summary>
        /// The transform position defines the focal plane.
        /// </summary>
        Volume = 0,

        /// <summary>
        /// The transform position defines the position of the camera.
        /// </summary>
        Camera = 1
    }
}
