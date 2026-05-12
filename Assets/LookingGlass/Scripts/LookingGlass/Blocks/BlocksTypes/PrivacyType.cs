using System;

namespace LookingGlass.Blocks {
    /// <summary>
    /// <para>Defines what type of access an already-published hologram has.</para>
    /// </summary>
    [Serializable]
    public enum PrivacyType {
        /// <summary>
        /// This hologram is publicly listed on your profile, and on the main Blocks page.
        /// </summary>
        PUBLIC,

        /// <summary>
        /// This hologram is hidden from other users on your profile, and is not shown on the main Blocks page.
        /// </summary>
        UNLISTED,

        /// <summary>
        /// This hologram is private, so only you can view it. The URL will only work if you're logged in, and not for anyone else.
        /// </summary>
        ONLY_ME
    }
}