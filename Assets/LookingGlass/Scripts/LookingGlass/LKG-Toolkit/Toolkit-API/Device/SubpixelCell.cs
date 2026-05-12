using System;

namespace LookingGlass.Toolkit {
    /// <summary>
    /// <para>
    /// Describes the layout of subpixel cells on a LKG display.
    /// </para>
    /// </summary>
    /// <remarks>
    /// NOTE: The offsets may be described in different spaces (such as pixel coordinates on the display's, or in viewport coordinates
    /// that are normalized by the <see cref="Calibration.screenW">screen width</see> width and <see cref="Calibration.screenH">screen height</see>.
    /// </remarks>
    [Serializable]
    public struct SubpixelCell {
        /// <summary>
        /// The size of one <see cref="SubpixelCell"/> struct, in bytes.
        /// </summary>
        public static int Stride => 3 * sizeof(float) * 2;

        public float ROffsetX;
        public float ROffsetY;
        public float GOffsetX;
        public float GOffsetY;
        public float BOffsetX;
        public float BOffsetY;

        public void Normalize(float screenWidth, float screenHeight) {
            ROffsetX /= screenWidth;
            ROffsetY /= screenHeight;

            GOffsetX /= screenWidth;
            GOffsetY /= screenHeight;

            BOffsetX /= screenWidth;
            BOffsetY /= screenHeight;
        }
    }
}
