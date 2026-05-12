using System;
using System.Collections.Generic;

namespace LookingGlass.Toolkit {
    /// <summary>
    /// <para>Represents a type of Looking Glass display.</para>
    /// <para>You can <see cref="ILKGDeviceTemplateSystem.GetTemplate(LKGDeviceType)">retrieve device-specific settings</see> based on this device type.</para>
    /// </summary>
    /// <remarks>
    /// <para>This enum must be kept in-sync/order with LKG Bridge's LKGHWVersionNamesLong and LKGHWVersionNames, defined in Constants.h.</para>
    /// <para>It's also assumed to contain contiguous values of zero and positive integers only, for convenience with arrays.</para>
    /// </remarks>
    [Serializable]
    public enum LKGDeviceType {
        /// <summary>
        /// <em>(legacy)</em> An 8.9" Looking Glass (Gen1), originally called the Standard LookingGlass.
        /// </summary>
        _8_9inGen1 = 0,

        /// <summary>
        /// <em>(legacy)</em> A 15.6" Looking Glass (Gen1), originally called the Large LookingGlass.
        /// </summary>
        _15_6inGen1 = 1,

        /// <summary>
        /// <em>(legacy)</em> The first 8K-resolution Looking Glass (Gen1), originally called the 8K LookingGlass.
        /// </summary>
        _8KGen1 = 3,

        /// <summary>
        /// A Looking Glass Portrait.
        /// </summary>
        PortraitGen2,

        /// <summary>
        /// A 4K-resolution, 16" Looking Glass (Gen2).
        /// </summary>
        _16inGen2,

        /// <summary>
        /// An 8K-resolution, 32" Looking Glass (Gen2).
        /// </summary>
        _32inGen2,

        /// <summary>
        /// A third-party, non-Looking Glass device.
        /// </summary>
        ThirdParty,

        /// <summary>
        /// The first large-screen 65" Looking Glass (Gen2, in landscape form).
        /// </summary>
        _65inLandscapeGen2,

        /// <summary>
        /// ? ? ?
        /// </summary>
        Prototype,

        /// <summary>
        /// The Looking Glass Go (Gen3, in portrait form).
        /// </summary>
        GoPortrait = 10,

        /// <summary>
        /// The Looking Glass Kiosk.
        /// </summary>
        Kiosk = 12,

        /// <summary>
        /// The 16" Looking Glass Spatial Display (Gen3, in portrait form).
        /// </summary>
        _16inPortraitGen3,

        /// <summary>
        /// The 16" Looking Glass Spatial Display (Gen3, in landscape form).
        /// </summary>
        _16inLandscapeGen3,

        /// <summary>
        /// The 32" Looking Glass Spatial Display (Gen3, in portrait form).
        /// </summary>
        _32inPortraitGen3,

        /// <summary>
        /// The 32" Looking Glass Spatial Display (Gen3, in landscape form).
        /// </summary>
        _32inLandscapeGen3,

        /// <summary>
        /// The 27" Looking Glass Spatial Display (Gen3, in portrait form).
        /// </summary>
        _27inPortraitGen3,

        /// <summary>
        /// The 27" Looking Glass Spatial Display (Gen3, in landscape form).
        /// </summary>
        _27inLandscapeGen3,
    }

    public static class LKGDeviceTypeExtensions {
        public static LKGDeviceType GetDefault() => LKGDeviceType.ThirdParty;

        //NOTE: There are indices that are in LKG Bridge, but unused here:
        private static readonly Dictionary<LKGDeviceType, string> NiceNames = new() {
            { LKGDeviceType._8_9inGen1,                     "Looking Glass 8.9\" (Legacy)" },
            { LKGDeviceType._15_6inGen1,                    "Looking Glass 15.6\" (Legacy)" },
            //2 (reserved)
            { LKGDeviceType._8KGen1,                        "Looking Glass 8K (Legacy)" },
            { LKGDeviceType.PortraitGen2,                   "Looking Glass Portrait" },
            { LKGDeviceType._16inGen2,                      "Looking Glass 16\"" },
            { LKGDeviceType._32inGen2,                      "Looking Glass 32\"" },
            { LKGDeviceType.ThirdParty,                     "Third-Party Non-Looking Glass" },
            { LKGDeviceType._65inLandscapeGen2,             "Looking Glass 65\" (Landscape)" },
            { LKGDeviceType.Prototype,                      "Looking Glass Prototype" },
            { LKGDeviceType.GoPortrait,                     "Looking Glass Go (Portrait)" },
            //11 (reserved)
            { LKGDeviceType.Kiosk,                          "Looking Glass Kiosk" },
            { LKGDeviceType._16inPortraitGen3,              "Looking Glass 16\" Spatial Display (Portrait)" },
            { LKGDeviceType._16inLandscapeGen3,             "Looking Glass 16\" Spatial Display (Landscape)" },
            { LKGDeviceType._32inPortraitGen3,              "Looking Glass 32\" Spatial Display (Portrait)" },
            { LKGDeviceType._32inLandscapeGen3,             "Looking Glass 32\" Spatial Display (Landscape)" },
            { LKGDeviceType._27inPortraitGen3,              "Looking Glass 27\" Spatial Display (Portrait)" },
            { LKGDeviceType._27inLandscapeGen3,             "Looking Glass 27\" Spatial Display (Landscape)" },
        };

        /// <summary>
        /// Gets a user-friendly name for the Looking Glass device, based on its type.
        /// </summary>
        /// <remarks>Note that this name is NOT necessarily unique, as several <see cref="LKGDeviceType"/>s may have the same nice name (such as both orientations of the Looking Glass Go).
        /// </remarks>
        /// <param name="type">The type of Looking Glass (LKG) display.</param>
        public static string GetNiceName(this LKGDeviceType type) => NiceNames[type];
    }
}
