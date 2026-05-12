using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

#if HAS_NEWTONSOFT_JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif

namespace LookingGlass.Toolkit {
    /// <summary>
    /// Contains data that is intrinsic to a specific LKG device. This data is used in rendering properly to the LKG display.
    /// </summary>
    [Serializable]
    public struct Calibration {
        //NOTE: Case insensitivity is started with (?i) and is removed with (?-i)
        internal static readonly Dictionary<Regex, LKGDeviceType> AutomaticSerialPatterns = new Dictionary<Regex, LKGDeviceType>() {
            { new Regex("(?i)(LKG-2K)"),                        LKGDeviceType._8_9inGen1 },
            { new Regex("(?i)(LKG-4K)"),                        LKGDeviceType._15_6inGen1 },
            //2 (reserved)
            { new Regex("(?i)(LKG-8K)"),                        LKGDeviceType._8KGen1 },
            { new Regex("(?i)(LKG-P)"),                         LKGDeviceType.PortraitGen2 },
            { new Regex("(?i)(LKG-A)"),                         LKGDeviceType._16inGen2 },
            { new Regex("(?i)(LKG-B)"),                         LKGDeviceType._32inGen2 },
            //7 (LKGDeviceType.ThirdParty)
            { new Regex("(?i)(LKG-D)"),                         LKGDeviceType._65inLandscapeGen2 },
            { new Regex("(?i)(LKG-Q)"),                         LKGDeviceType.Prototype },
            { new Regex("(?i)(LKG-E)"),                         LKGDeviceType.GoPortrait },
            //11 (reserved)
            { new Regex("(?i)(LKG-F)"),                         LKGDeviceType.Kiosk },
            { new Regex("(?i)(LKG-H)"),                         LKGDeviceType._16inPortraitGen3 },
            { new Regex("(?i)(LKG-J)"),                         LKGDeviceType._16inLandscapeGen3 },
            { new Regex("(?i)(LKG-K)"),                         LKGDeviceType._32inPortraitGen3 },
            { new Regex("(?i)(LKG-L)"),                         LKGDeviceType._32inLandscapeGen3 },
            { new Regex("(?i)(LKG-N)"),                         LKGDeviceType._27inPortraitGen3 },
            { new Regex("(?i)(LKG-R)"),                         LKGDeviceType._27inLandscapeGen3 },
        };

        public const int MaxSubpixelPatterns = 10;

        public static float ProcessPitch(float screenW, float pitch, float dpi, float slope) => pitch * screenW / dpi * MathF.Cos(MathF.Atan(1 / slope));
        public static float ProcessPitch(float screenW, in Calibration cal) => cal.pitch * screenW / cal.dpi * MathF.Cos(MathF.Atan(1 / cal.slope));
        public static float ProcessSlope(float screenW, float screenH, float slope, float flipImageX) => screenH / (screenW * slope) * (flipImageX >= 0.5f ? -1 : 1);
        public static float ProcessSlope(float screenW, float screenH, in Calibration cal) => screenH / (screenW * cal.slope) * (cal.flipImageX >= 0.5f ? -1 : 1);

        /// <summary>
        /// The JSON text contained within the LKG device's visual.json (Calibration) file.
        /// </summary>
        public string rawJson;

        public string configVersion;

        /// <summary>
        /// The unique serial identifier for the particular LKG device.
        /// </summary>
        public string serial;

        public float pitch;
        public float slope;
        public float center;
        public int fringe;

        public int viewCone;
        public int invView;
        public float verticalAngle;

        /// <summary>
        /// The LKG display's dots per inch (DPI).
        /// </summary>
        public float dpi;

        /// <summary>
        /// The native screen width of the LKG display, in pixels.
        /// </summary>
        public int screenW;

        /// <summary>
        /// The native screen height of the LKG display, in pixels.
        /// </summary>
        public int screenH;

        /// <summary>
        /// Determines whether or not to flip the screen horizontally. A value of 1 causes the screen to flip horizontally. The default value is 0.
        /// </summary>
        public float flipImageX;

        /// <summary>
        /// Determines whether or not to flip the screen vertically. A value of 1 causes the screen to flip vertically. The default value is 0.
        /// </summary>
        public float flipImageY;

        public float flipSubp;

        /// <summary>
        /// An integer that determines what type of cell pattern is used when lenticularizing for this display.
        /// </summary>
        /// <remarks>
        /// Also known as the cellPatternType for the lenticular shader uniform.<br />
        /// Valid values are currently 0 (default), 1, 2, 3, or 4.
        /// A value of 0 is assumed for LKG displays that were made before the 3rd generation.
        /// </remarks>
        public int cellPatternMode;

        /// <summary>
        /// Defines the arrangement of the RGB subpixels on the display, measured in pixels on the display.
        /// </summary>
        /// <remarks>For example, a value of 0.3333 is about one-third of a full pixel over.</remarks>
        public SubpixelCell[] subpixelCells;

        /// <summary>
        /// The display's native aspect ratio, calculated using <see cref="screenW"/> / <see cref="screenH"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Because quilts may not always be able to achieve this exact aspect ratio for each of the quilt tiles, <see cref="QuiltSettings"/>
        /// store this value at the time of capturing the quilt into the <see cref="QuiltSettings.renderAspect"/> variable,
        /// so that the quilt may be re-rendered later with the correct squishing or stretching to match the display's aspect ratio again when rendering the quilt.
        /// </para>
        /// <para>
        /// This value is always greater than zero. If either the <see cref="screenW"/> or <see cref="screenH"/> are 0, this returns 1.
        /// </para>
        /// </remarks>
        public float ScreenAspect => (screenW == 0 || screenH == 0) ? 1 : (float) screenW / screenH;
        public float ProcessedPitch => ProcessPitch(screenW, this);
        public float ProcessedSlope => ProcessSlope(screenW, screenH, this);
        public bool IsSameDevice(in Calibration other) => other.serial == serial;

        public static Calibration CreateDefault() {
            ILKGDeviceTemplateSystem system = ServiceLocator.Instance.GetSystem<ILKGDeviceTemplateSystem>();
            if (system == null)
                return default;
            LKGDeviceTemplate settings = system.GetDefaultTemplate();
            if (settings == null)
                return default;
            return settings.calibration;
        }

        public LKGDeviceType GetDeviceType() {
            if (string.IsNullOrEmpty(serial))
                return LKGDeviceTypeExtensions.GetDefault();

            foreach (KeyValuePair<Regex, LKGDeviceType> pair in AutomaticSerialPatterns)
                if (pair.Key.IsMatch(serial))
                    return pair.Value;

            ILogger logger = ServiceLocator.Instance.GetSystem<ILogger>();
            if (logger != null)
                logger.LogError("Unrecognized type of LKG device by serial field! (serial = \"" + serial + "\")");
            return LKGDeviceTypeExtensions.GetDefault();
        }

        public bool SeemsGood() {
            if (screenW != 0 && screenH != 0)
                return true;
            return false;
        }

        public Calibration CopyWithCustomResolution(int renderWidth, int renderHeight) {
            Calibration copy = this;
            copy.screenW = renderWidth;
            copy.screenH = renderHeight;
            return copy;
        }

#if HAS_NEWTONSOFT_JSON
        /// <summary>
        /// Saves this calibration data into a JSON object that matches in structure to a visual.json calibration file.
        /// </summary>
        public JObject ToJSON() {
            JObject j = new();
            //NOTE: No inner "value" for the following fields:
            //  - configVersion
            //  - serial
            //  - subpixelCells

            j.Add("configVersion", configVersion);
            j.Add("serial", serial);

            j.Add("pitch", "value", pitch);
            j.Add("slope", "value", slope);
            j.Add("center", "value", center);
            j.Add("fringe", "value", fringe);

            j.Add("viewCone", "value", viewCone);
            j.Add("invView", "value", invView);
            j.Add("verticalAngle", "value", verticalAngle);
            j.Add("DPI", "value", dpi);
            j.Add("screenW", "value", screenW);
            j.Add("screenH", "value", screenH);

            j.Add("flipImageX", "value", flipImageX);
            j.Add("flipImageY", "value", flipImageY);
            j.Add("flipSubp", "value", flipSubp);

            if (cellPatternMode != 0)
                j.Add("CellPatternMode", "value", cellPatternMode);
            if (subpixelCells != null && subpixelCells.Length > 0)
                j.Add("subpixelCells", JArray.FromObject(subpixelCells));
            return j;
        }

        /// <summary>
        /// <para>Saves this calibration data into JSON the same way as <see cref="ToJSON"/>, but formats the output as a string with pretty-printed spacing with an indent size of 4.</para>
        /// <para>See also: <seealso cref="JObjectExtensions.FormatPrettyJSON(JObject, char, int)"/></para>
        /// </summary>
        public string ToJSONString() {
            JObject j = ToJSON();
            return j.FormatPrettyJSON(' ', 4);
        }

        public static Calibration Parse(string json) {
            JObject j = JObject.Parse(json);
            return Parse(j);
        }

        public static Calibration Parse(JObject obj) {
            Calibration cal = new();
            cal.rawJson = obj.ToString(Formatting.Indented);

            obj.TryGet<string>("configVersion", out cal.configVersion);
            obj.TryGet<string>("serial", out cal.serial);
            obj.TryGet<float>("pitch", "value", out cal.pitch);
            obj.TryGet<float>("slope", "value", out cal.slope);
            obj.TryGet<float>("center", "value", out cal.center);
            obj.TryGet<int>("fringe", "value", out cal.fringe);

            obj.TryGet<int>("viewCone", "value", out cal.viewCone);
            obj.TryGet<int>("invView", "value", out cal.invView);
            obj.TryGet<float>("verticalAngle", "value", out cal.verticalAngle);
            obj.TryGet<float>("DPI", "value", out cal.dpi);
            obj.TryGet<int>("screenW", "value", out cal.screenW);
            obj.TryGet<int>("screenH", "value", out cal.screenH);

            obj.TryGet<float>("flipImageX", "value", out cal.flipImageX);
            obj.TryGet<float>("flipImageY", "value", out cal.flipImageY);
            obj.TryGet<float>("flipSubp", "value", out cal.flipSubp);

            if (!obj.TryGet<int>("CellPatternMode", "value", out cal.cellPatternMode))
                obj.TryGet<int>("cellPatternMode", "value", out cal.cellPatternMode); //NOTE: This supports lowercase, in case we ever want to fix the typo! :)

            if (obj.TryGet("subpixelCells", out JArray jSubpixelCells)) {
                cal.subpixelCells = new SubpixelCell[jSubpixelCells.Count];
                for (int i = 0; i < cal.subpixelCells.Length; i++) {
                    jSubpixelCells[i].TryGet<float>("ROffsetX", out cal.subpixelCells[i].ROffsetX);
                    jSubpixelCells[i].TryGet<float>("ROffsetY", out cal.subpixelCells[i].ROffsetY);
                    jSubpixelCells[i].TryGet<float>("GOffsetX", out cal.subpixelCells[i].GOffsetX);
                    jSubpixelCells[i].TryGet<float>("GOffsetY", out cal.subpixelCells[i].GOffsetY);
                    jSubpixelCells[i].TryGet<float>("BOffsetX", out cal.subpixelCells[i].BOffsetX);
                    jSubpixelCells[i].TryGet<float>("BOffsetY", out cal.subpixelCells[i].BOffsetY);
                }
            }

            return cal;
        }
#endif
    }
}
