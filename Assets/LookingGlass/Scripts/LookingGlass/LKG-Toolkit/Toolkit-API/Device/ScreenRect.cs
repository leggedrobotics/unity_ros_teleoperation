using System;

namespace LookingGlass.Toolkit {
    /// <summary>
    /// Defines a rectangular area on a display or window.
    /// </summary>
    /// <remarks>
    /// This implementation assumes that coordinates are (0, 0) on the top-left, and increase in the x-axis going right, and increase in the y-axis going down.
    /// </remarks>
    [Serializable]
    public struct ScreenRect {
        public int left;
        public int top;
        public int right;
        public int bottom;

        public int Width => right - left;
        public int Height => bottom - top;

        public override string ToString() => nameof(ScreenRect) + " (position = (" + left + ", " + top + "), size = " + Width + "x" + Height + "))";
    }
}
