using System;
using LookingGlass.Toolkit;

namespace LookingGlass {
    [Serializable]
    public struct OverrideLKGDisplay {
        /// <summary>
        /// The file system path of the calibration JSON file.
        /// </summary>
        public string calibration;

        public QuiltSettings defaultQuilt;

        public DisplayInfo hardwareInfo;
    }
}
