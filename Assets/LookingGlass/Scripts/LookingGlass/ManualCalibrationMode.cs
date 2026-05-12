using System;

namespace LookingGlass {
    [Serializable]
    public enum ManualCalibrationMode {
        None = 0,
        UseCalibrationTextAsset = 1,
        UseManualSettings = 2
    }
}
