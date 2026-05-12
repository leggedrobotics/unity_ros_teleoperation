//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

namespace LookingGlass.Demos {
    public class DemoBlurUnblur : MonoBehaviour {
#if UNITY_POST_PROCESSING_STACK_V2
        public PostProcessVolume postProcessingVolume;
        public UnityEngine.UI.Text label;
        public bool enablePostProcessing;

        void Update() {
            if (enablePostProcessing) {
                label.text = "Post Processing";
                postProcessingVolume.enabled = true;
            } else {
                label.text = "No Post Processing";
                postProcessingVolume.enabled = false;
            }
        }
#endif
    }
}
