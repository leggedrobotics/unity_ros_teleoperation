using System;

namespace LookingGlass {
    /// <summary>
    /// Describes the type of view interpolation to use, if any, for performance optimization purposes.
    /// </summary>
    [Serializable]
    public enum HologramViewInterpolation {
        None,
        EveryOther,
        Every4th,
        Every8th,
        _4Views,
        _2Views
    }
}
