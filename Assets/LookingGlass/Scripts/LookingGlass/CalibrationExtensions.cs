using LookingGlass.Toolkit;

namespace LookingGlass {
    public static class CalibrationExtensions {
        public static bool IsDefaultSerialized(this Calibration value) {
            return
                string.IsNullOrWhiteSpace(value.rawJson) &&
                string.IsNullOrWhiteSpace(value.configVersion) &&
                string.IsNullOrWhiteSpace(value.serial) &&
                value.pitch == 0 &&
                value.slope == 0 &&
                value.center == 0 &&
                value.fringe == 0 &&
                value.viewCone == 0 &&
                value.invView == 0 &&
                value.verticalAngle == 0 &&
                value.dpi == 0 &&
                value.screenW == 0 &&
                value.screenH == 0 &&
                value.flipImageX == 0 &&
                value.flipImageY == 0 &&
                value.flipSubp == 0 &&
                value.cellPatternMode == 0 &&
                (value.subpixelCells == null || value.subpixelCells.Length == 0);
        }
    }
}
