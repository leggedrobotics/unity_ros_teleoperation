using System;

namespace LookingGlass.Blocks {
    /// <summary>
    /// <para>Defines whether a hologram is made from a quilt or an RGB-D texture or video.</para>
    /// </summary>
    [Serializable]
    public enum HologramType {
        QUILT,
        RGBD
    }
}