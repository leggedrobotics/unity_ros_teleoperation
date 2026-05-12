using System;

namespace LookingGlass {
    [Serializable]
    public enum QuiltRecordingPreset {
        Custom = -2,
        Automatic = -1,
        LookingGlassGo = 0,
        LookingGlassPortrait = 1,
        LookingGlass16Landscape,
        LookingGlass16Portrait,
        LookingGlass27Landscape,
        LookingGlass27Portrait,
        LookingGlass32Landscape,
        LookingGlass32Portrait,
        LookingGlass65,
        LookingGlass16Gen2,
        LookingGlass32Gen2,
    }
}
