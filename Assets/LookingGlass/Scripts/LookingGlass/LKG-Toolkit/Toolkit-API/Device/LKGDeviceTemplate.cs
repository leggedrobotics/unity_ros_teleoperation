using System;

namespace LookingGlass.Toolkit {
    [Serializable]
    public class LKGDeviceTemplate {
        public Calibration calibration;
        public QuiltSettings defaultQuilt;

        public LKGDeviceTemplate() { }

        public LKGDeviceTemplate(LKGDeviceTemplate source) {
            calibration = source.calibration;
            defaultQuilt = source.defaultQuilt;
        }

        public LKGDeviceTemplate(Calibration calibration, QuiltSettings defaultQuilt) {
            this.calibration = calibration;
            this.defaultQuilt = defaultQuilt;
        }
    }
}
