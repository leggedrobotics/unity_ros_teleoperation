using System;

namespace LookingGlass {
    public static class HologramViewInterpolationExtensions {
        /// <summary>
        /// <para>
        /// Calculates a divisor that can be used in the form of <c>viewIndex % divisor</c>, which is used in some mathematical
        /// view interpolation calculates, and is useful in <c>for</c> loops to skip over loop iterations when <c>viewIndex % divisor != 0 && viewIndex != viewCount - 1</c>.
        /// </para>
        /// <para>See also: <seealso cref="IsInterpolatedView(HologramViewInterpolation, int, int)"/></para>
        /// </summary>
        internal static int GetViewIndexDivisor(this HologramViewInterpolation value, int viewCount) {
            switch (value) {
                case HologramViewInterpolation.None:
                default:
                                                            return 1;
                case HologramViewInterpolation.EveryOther:  return 2;
                case HologramViewInterpolation.Every4th:    return 4;
                case HologramViewInterpolation.Every8th:    return 8;
                case HologramViewInterpolation._4Views:     return viewCount / 3;
                case HologramViewInterpolation._2Views:     return viewCount;
            }
        }

        /// <summary>
        /// Determines whether or not the view at index <paramref name="viewIndex"/> should be an interpolated view or not,
        /// based on the type of view interpolation used, and the total number of views.
        /// </summary>
        public static bool IsInterpolatedView(this HologramViewInterpolation value, int viewIndex, int viewCount) {
            return viewIndex % GetViewIndexDivisor(value, viewCount) != 0 && viewIndex != viewCount - 1;
        }
    }
}