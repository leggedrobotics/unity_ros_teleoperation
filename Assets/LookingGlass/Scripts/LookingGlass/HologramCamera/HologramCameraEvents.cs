using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace LookingGlass {
    /// <summary>
    /// Contains the events that a <see cref="HologramCamera"/> component will fire off.
    /// </summary>
    [Serializable]
    public class HologramCameraEvents : PropertyGroup {
        public ViewRenderEvent OnViewRendered {
            get { return hologramCamera.onViewRendered; }
            internal set { hologramCamera.onViewRendered = value; } //NOTE: Setter available for serialization layout updates
        }
    }
}
