Shader "Hidden/PostProcessing/DeferredFog"
{
    HLSLINCLUDE

        #pragma multi_compile __ FOG_LINEAR FOG_EXP FOG_EXP2
        #include "../StdLib.hlsl"
        #include "Fog.hlsl"

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);

        #pragma multi_compile _ USE_FAKE_DEPTH
        #if USE_FAKE_DEPTH
            TEXTURE2D_SAMPLER2D(_FAKEDepthTexture, sampler_FAKEDepthTexture);
        #else
            TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);
            #define _FAKEDepthTexture _CameraDepthTexture
            #define sampler_FAKEDepthTexture sampler_CameraDepthTexture
        #endif

        #define SKYBOX_THREASHOLD_VALUE 0.9999

        float4 Frag(VaryingsDefault i) : SV_Target
        {
            half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoordStereo);

            float depth = SAMPLE_DEPTH_TEXTURE(_FAKEDepthTexture, sampler_FAKEDepthTexture, i.texcoordStereo);
            depth = Linear01Depth(depth);
            float dist = ComputeFogDistance(depth);
            half fog = 1.0 - ComputeFog(dist);

            return lerp(color, _FogColor, fog);
        }

        float4 FragExcludeSkybox(VaryingsDefault i) : SV_Target
        {
            half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoordStereo);

            float depth = SAMPLE_DEPTH_TEXTURE(_FAKEDepthTexture, sampler_FAKEDepthTexture, i.texcoordStereo);
            depth = Linear01Depth(depth);
            float skybox = depth < SKYBOX_THREASHOLD_VALUE;
            float dist = ComputeFogDistance(depth);
            half fog = 1.0 - ComputeFog(dist);

            return lerp(color, _FogColor, fog * skybox);
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment Frag

            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragExcludeSkybox

            ENDHLSL
        }
    }
}
