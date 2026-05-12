using System;
using System.Collections.Generic;
using UnityEngine;
using LookingGlass;

#if UNITY_EDITOR
using UnityEditor;
#endif

//TODO: Possibly use a custom namespace for this custom code?
namespace UnityEngine.Rendering.PostProcessing {
    /// <summary>
    /// Contains extensions to <see cref="HologramCamera"/> to support post-processing, by implementing callbacks during onto OnEnable and OnDisable.
    /// </summary>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    internal static class HologramCameraPostProcessSetup {
#if UNITY_EDITOR
        static HologramCameraPostProcessSetup() {
            RegisterCallbacks();
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterCallbacks() {
            HologramCamera.UsePostProcessing = DetermineIfShouldUsePostProcessing;
        }

        private static bool DetermineIfShouldUsePostProcessing(HologramCamera hologramCamera) {
            if (!hologramCamera.TryGetComponent(out PostProcessLayer layer) || !layer.enabled)
                return false;

#if !UNITY_POST_PROCESSING_STACK_V2
            Debug.LogWarning("WARNING: Looking Glass' fork of Unity post-processing is in the project, but UNITY_POST_PROCESSING_STACK_V2 is not defined in the project settings / script compilation symbols, so no post-processing will take effect.");
            return false;
#endif

            if (!RenderPipelineUtil.IsBuiltIn) {
                Debug.LogWarning("WARNING: Ignoring " + nameof(PostProcessLayer) + " on the " + nameof(HologramCamera) + " because only Unity BiRP (Built-in Render Pipeline) is supported by it!");
                return false;
            }

            return true;
        }
    }
}
