Shader "Unlit/StereoStream"
{
    Properties
    {
        _LeftTex ("Texture", 2D) = "white" {}
        _RightTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _LeftTex;
            sampler2D _RightTex;
            // head centre and right axis, set by StereoStreamer; XR runs multi-pass,
            // where unity_StereoEyeIndex is always 0
            float4 _HeadPos;
            float4 _HeadRight;

            
            v2f vert (appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.uv = v.uv;

                o.vertex = UnityObjectToClipPos(v.vertex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float3 eyePos = mul(transpose((float3x3)UNITY_MATRIX_V), -UNITY_MATRIX_V._m03_m13_m23);
                bool right = dot(eyePos - _HeadPos.xyz, _HeadRight.xyz) > 0;
                fixed4 col = right ? tex2D(_RightTex, i.uv) : tex2D(_LeftTex, i.uv);

                return col;
            }
            ENDCG
        }
    }
}
