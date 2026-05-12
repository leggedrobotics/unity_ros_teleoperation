using LookingGlass.Toolkit;
using LookingGlass.Toolkit.Bridge;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LookingGlass {
    /// <summary>
    /// Provides user-configurable settings to customize behavior of the LKG Unity Plugin, via an optional JSON file at <c>lkg-settings.json</c> (See: <see cref="LKGSettingsSystem.FileName"/>).
    /// </summary>
    public struct LKGSettings {
        public static LKGSettings Default => new() {
            loggingFlags = BridgeLoggingFlags.All,
            queryingLoggingFlags = BridgeLoggingFlags.None,
            enableHologramDebugging = false,
#if ENABLE_INPUT_SYSTEM
            hologramDebuggingKeys = new Key[] {
                Key.LeftCtrl,
                Key.LeftAlt,
                Key.LeftShift
            }
#endif
        };

        /// <summary>
        /// Determines how much is logged with LKG Bridge by default, when using the <see cref="LKGDisplaySystem"/> (and internally, <see cref="BridgeConnectionHTTP"/>).
        /// </summary>
        public BridgeLoggingFlags loggingFlags;

        /// <summary>
        /// <para>Every couple of seconds, LKG Bridge is queried for the most up-to-date LKG displays connected to your system.</para>
        /// <para>This overrides the amount of logging specifically for these API calls (over <see cref="loggingFlags"/>), since these calls can easily spam your Unity console/log file.</para>
        /// </summary>
        public BridgeLoggingFlags queryingLoggingFlags;

        /// <summary>
        /// Determines whether or not hologram debugging snapshots can be taken.
        /// </summary>
        public bool enableHologramDebugging;

#if ENABLE_INPUT_SYSTEM
        /// <summary>
        /// The keys to press that will take a hologram debugging snapshot, if hologram debugging is enabled (<see cref="enableHologramDebugging"/> must be set to <c>true</c>).
        /// </summary>
        public Key[] hologramDebuggingKeys;
#endif

        /// <summary>
        /// When set to <c>true</c>, this forces non-Preview GameView windows (The user's GameViews) in the Unity editor to NOT have their resolution automatically match the Looking Glass.
        /// </summary>
        /// <remarks>
        /// This may be useful for example if you are developing with a high-resolution Looking Glass, but do not wish to slow down your Unity editor to render high-resolution GameViews.
        /// </remarks>
        public bool skipUserGameViews;

        /// <summary>
        /// <para>If this is set to a <c>non-null</c> value, this will completely override all the LKG displays that are considered to be connected to this machine.</para>
        /// <para>In other words, this will replace what LKG Bridge sends during the ordinary flow of calibration loading in the LKG Unity Plugin.</para>
        /// <para>This <em>only</em> applies to the Unity Plugin, and does NOT affect the OS, Looking Glass Bridge, or any other Looking Glass programs.</para>
        /// </summary>
        /// <remarks>
        /// Use this for very advanced scenarios when you have a strange LKG display setup such as a display that is not reporting calibration correctly, but you know exactly the data that it should be.<br />
        /// (use LKG Bridge/Toolkit logging to help you fill out all of the fields, and use native OS display arrangement/virtual screen coordinates).
        /// </remarks>
        public OverrideLKGDisplay[] overrideLKGDisplays;
    }
}
