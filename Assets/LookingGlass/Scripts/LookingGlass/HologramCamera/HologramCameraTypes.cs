using System;
using UnityEngine;
using UnityEngine.Events;

namespace LookingGlass {
    /// <summary>
    /// An event that gets fired when a single view is rendered.
    /// </summary>
    [Serializable]
    public class ViewRenderEvent : UnityEvent<HologramCamera, int> { };

    public partial class HologramCamera {
        [Serializable]
        public enum DisplayTarget {
            Display1 = 0,
            Display2,
            Display3,
            Display4,
            Display5,
            Display6,
            Display7,
            Display8,
        }
    }
}