using System;
using LookingGlass.Toolkit;

namespace LookingGlass {
    /// <summary>
    /// Defines <see cref="LookingGlass.Toolkit.QuiltSettings"/> that may either be the default quilt settings for a given type of Looking Glass display, or custom quilt settings.
    /// </summary>
    /// <remarks>
    /// See also:
    /// <list type="bullet">
    /// <item><see cref="LookingGlass.Toolkit.QuiltSettings"/></item>
    /// </list>
    /// </remarks>
    [Serializable]
    public struct QuiltPreset {
        public LKGDeviceType deviceType;
        public bool useCustom;
        public QuiltSettings customSettings;

        /// <summary>
        /// The calculated <see cref="LookingGlass.Toolkit.QuiltSettings"/>, based on either the default template data
        /// for the given <see cref="deviceType"/>, or just the <see cref="customSettings"/> when <see cref="useCustom"/> is set to <c>true</c>.
        /// </summary>
        public QuiltSettings QuiltSettings {
            get {
                if (useCustom)
                    return customSettings;
                return QuiltSettings.GetDefaultFor(deviceType);
            }
        }

        /// <summary>
        /// Creates a quilt preset with default LKG device quilt settings.
        /// </summary>
        /// <param name="deviceType">The type of display to use the quilt settings of.</param>
        public QuiltPreset(LKGDeviceType deviceType) {
            this.deviceType = deviceType;
            useCustom = false;
            customSettings = QuiltSettings.GetDefaultFor(deviceType);
        }
        
        /// <summary>
        /// Creates custom quilt settings.
        /// </summary>
        /// <param name="customValues"></param>
        public QuiltPreset(QuiltSettings customValues) {
            deviceType = LKGDeviceType.ThirdParty;
            useCustom = true;
            this.customSettings = customValues;
        }

        public void UseCustom(QuiltSettings customValues) {
            deviceType = LKGDeviceType.ThirdParty;
            useCustom = true;
            this.customSettings = customValues;
        }

        public void UseDefaultFrom(LKGDeviceType deviceType) {
            this.deviceType = deviceType;
            useCustom = false;
            customSettings = QuiltSettings.GetDefaultFor(deviceType);
        }
    }
}
