using LookingGlass.Toolkit;
using UnityEngine;

namespace LookingGlass {
    public static class ScreenRectUtility {
        public static RectInt ToUnityRect(this ScreenRect screenRect) => new RectInt(screenRect.left, screenRect.top, screenRect.Width, screenRect.Height);
    }
}
