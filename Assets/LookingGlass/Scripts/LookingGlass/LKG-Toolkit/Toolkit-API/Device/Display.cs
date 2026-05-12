using System;

#if HAS_NEWTONSOFT_JSON
using Newtonsoft.Json.Linq;
#endif

namespace LookingGlass.Toolkit {
    /// <summary>
    /// This represents a connected display, which may or may not be a LKG display or a regular 2D monitor.
    /// </summary>
    [Serializable]
    public class Display {
        public int id;

        /// <summary>
        /// <para>
        /// The LKG display-specific calibration values, required for accurate holographic rendering to the LKG display.<br />
        /// This will only be set properly if this display is a LKG display.
        /// </para>
        /// See also: <seealso cref="IsLKG"/>
        /// </summary>
        public Calibration calibration;

        /// <summary>
        /// <para>
        /// The default, recommended quilt settings for this LKG display.<br />
        /// This will only be set properly if this display is a LKG display.
        /// </para>
        /// See also: <seealso cref="IsLKG"/>
        /// </summary>
        public QuiltSettings defaultQuilt;

        public DisplayInfo hardwareInfo;

        public bool IsLKG {
            get {
                return hardwareInfo.hwid != null && hardwareInfo.hwid.Contains("LKG")
                    && calibration.SeemsGood();
            }
        }

        public Display() {
            id = -1;
            calibration = new Calibration();
            defaultQuilt = new QuiltSettings();
            hardwareInfo = new DisplayInfo();
        }

        public Display(Display source) {
            id = source.id;
            calibration = source.calibration;
            defaultQuilt = source.defaultQuilt;
            hardwareInfo = source.hardwareInfo;
        }

        private Display(int id) {
            this.id = id;
        }

        private Display(int id, Calibration calibration, QuiltSettings defautQuilt, DisplayInfo hardwareInfo) {
            this.id = id;
            this.calibration = calibration;
            this.defaultQuilt = defautQuilt;
            this.hardwareInfo = hardwareInfo;
        }

#if HAS_NEWTONSOFT_JSON
        public static Display Parse(int id, JObject obj) {
            Display display = new(id);

            //"{ }" or ""
            if (obj.TryGet("calibration", "value", out JObject jCalibration))
                display.calibration = Calibration.Parse(jCalibration);
            if (obj.TryGet("defaultQuilt", "value", out JObject jDefaultQuilt))
                display.defaultQuilt = QuiltSettings.Parse(jDefaultQuilt);
            display.hardwareInfo = DisplayInfo.Parse(obj);

            return display;
        }
#endif

        public string GetInfoString() =>
            "Display Type: " + hardwareInfo.hardwareVersion + "\n" +
            "Display Hardware ID: " + hardwareInfo.hwid + "\n" +
            "Display Coords: [" + hardwareInfo.windowCoordX + ", " + hardwareInfo.windowCoordY + "]\n" +
            "Calibration Version: " + calibration.configVersion + "\n";

        public bool IsSameDevice(Display other) => other.hardwareInfo.hwid == hardwareInfo.hwid;
        public override int GetHashCode() => hardwareInfo.hwid?.GetHashCode() ?? 0;
        public override bool Equals(object obj) {
            if (obj == null)
                return false;
            if (obj is Display other) {
                return id == other.id &&
                    calibration.Equals(other.calibration) &&
                    defaultQuilt.Equals(other.defaultQuilt) &&
                    hardwareInfo.Equals(other.hardwareInfo);
            }
            return false;
        }
    }
}
