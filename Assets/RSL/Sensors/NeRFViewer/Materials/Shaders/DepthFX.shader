// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/DepthFX"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _DepthTex ("Depth Texture", 2D) = "white" {}
        _DepthClip ("Depth Clip", Range(0, 1)) = 0.5
        _DepthLevel ("Depth Level", Range(0, 1)) = 0.5

    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            sampler2D _DepthTex;
            float _DepthClip;
            uniform sampler2D _CameraDepthTexture;
            uniform fixed _DepthLevel;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                // transform vertex position based depth
                //o.vertex = UnityObjectToClipPos(v.vertex);
                //o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = MultiplyUV(UNITY_MATRIX_TEXTURE0, v.uv);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // apply a parallax effect based on depth and the camera pose
                float depth = UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, i.uv));
                depth = pow(Linear01Depth(depth), _DepthLevel);


                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                col.a = depth > _DepthClip ? 1 : 0;

                return depth;
            }
            ENDCG
        }
    }
}
