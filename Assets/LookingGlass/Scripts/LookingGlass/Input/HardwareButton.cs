using System;

namespace LookingGlass {
    /// <summary>
    /// Represents the hardware buttons present on all generations of Looking Glass displays.
    /// </summary>
    [Serializable]
    public enum HardwareButton {
        /// <summary>
        /// Square button on Looking Glass classic devices.
        /// </summary>
        Square = 1,

        /// <summary>
        /// Left button on Looking Glass classic devices.
        /// </summary>
        Left = 2,

        /// <summary>
        /// Right button on Looking Glass classic devices.
        /// </summary>
        Right = 3,

        /// <summary>
        /// Circle button on Looking Glass classic devices.
        /// </summary>
        Circle = 4,

        /// <summary>
        /// Forward button on Portrait and Gen2 devices.
        /// </summary>
        Forward = 5,

        /// <summary>
        /// Back button on Portrait and Gen2 devices.
        /// </summary>
        Back = 6,

        /// <summary>
        /// Play / Pause / Loop button on Portrait and Gen2 devices.
        /// </summary>
        PlayPause = 7
    }
}
