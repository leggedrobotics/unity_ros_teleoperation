using System;
using UnityEngine;

namespace LookingGlass {
    [Serializable]
    public class OptimizationProperties : PropertyGroup {
        public HologramViewInterpolation ViewInterpolation {
            get { return hologramCamera.viewInterpolation; }
            set { hologramCamera.viewInterpolation = value; }
        }

        public bool ReduceFlicker {
            get { return hologramCamera.reduceFlicker; }
            set { hologramCamera.reduceFlicker = value; }
        }

        public bool FillGaps {
            get { return hologramCamera.fillGaps; }
            set { hologramCamera.fillGaps = value; }
        }

        public bool BlendViews {
            get { return hologramCamera.blendViews; }
            set { hologramCamera.blendViews = value; }
        }
    }
}
