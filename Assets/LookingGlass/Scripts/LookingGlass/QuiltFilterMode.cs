using System;
using UnityEngine;

namespace LookingGlass {
    /// <summary>
    /// Represents what kind of texture filtering is used for the final quilt mix that will be sampled in the lenticular shader.
    /// </summary>
    [Serializable]
    public enum QuiltFilterMode {
        /// <summary>
        /// Represents <see cref="FilterMode.Point"/> with no modifications.
        /// </summary>
        Point = 0,

        /// <summary>
        /// Represents <see cref="FilterMode.Bilinear"/> with no modifications.
        /// </summary>
        Bilinear = 1,

        /// <summary>
        /// Represents <see cref="FilterMode.Point"/> sampling with custom lenticular-based anti-aliasing performed in the lenticular shader.
        /// </summary>
        [InspectorName("Point & Virtual Pixel AA")]
        PointVirtualPixelAA = 2,
    }

    public static class QuiltFilterModeExtensions {
        /// <summary>
        /// Gets the corresponding Unity <see cref="FilterMode"/> associated with the <see cref="QuiltFilterMode"/> value.
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static FilterMode GetUnityFilterMode(this QuiltFilterMode mode) {
            switch (mode) {
                case QuiltFilterMode.Bilinear:  return FilterMode.Bilinear;
                default:
                    return FilterMode.Point;
            }
        }

        /// <summary>
        /// Does the current <see cref="QuiltFilterMode"/> use lenticular-based anti-aliasing?
        /// </summary>
        public static bool UsesLenticularAA(this QuiltFilterMode mode) => mode == QuiltFilterMode.PointVirtualPixelAA;
    }
}
