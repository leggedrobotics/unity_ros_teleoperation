using UnityEngine;
using UnityEngine.Rendering;

namespace LookingGlass {
    //Huge thanks to these Unity forums threads!
    //https://forum.unity.com/threads/hdrp-lwrp-detection-from-editor-script.540642/
    //https://forum.unity.com/threads/how-to-know-if-i-have-the-lwrp-hdrp-api-at-compile-time.546513/

    //TODO: It would be better to use C# preprocessor defines so we can conditionally compile certain code, like:
    //#if UNITY_HDRP
    //#elif UNITY_URP
    //#else
    //etc.
    public static class RenderPipelineUtil {
        public static bool IsBuiltIn => GetRenderPipelineType() == RenderPipelineType.BuiltIn;
        public static bool IsHDRP => GetRenderPipelineType() == RenderPipelineType.HDRP;
        public static bool IsURP => GetRenderPipelineType() == RenderPipelineType.URP;
        public static RenderPipelineType GetRenderPipelineType() {
#if UNITY_2019_3_OR_NEWER
            RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;

            if (pipeline == null)
                return RenderPipelineType.BuiltIn;

            string pipelineTypeFullName = pipeline.GetType().FullName;
            if (pipelineTypeFullName.Contains("HighDefinition"))
                return RenderPipelineType.HDRP;

            if (pipelineTypeFullName.Contains("Universal"))
                return RenderPipelineType.URP;

            Debug.LogWarning("Unsupported render pipeline for LookingGlass! (" + pipelineTypeFullName + ")");
            return RenderPipelineType.Unsupported;
#else
            return RenderPipelineType.BuiltIn;
#endif
        }
    }
}
