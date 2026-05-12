#if UNITY_EDITOR
using System;

namespace LookingGlass {
    [Serializable]
    public struct GameViewSizeInfo {
        public static GameViewSizeInfo Invalid => new GameViewSizeInfo();

        public int width;
        public int height;
        public string label;
        public string displayText;

        public bool IsValid => label != null && displayText != null;
    }
}
#endif
