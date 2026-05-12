//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

Shader "LookingGlass/Copy RGB-A" {
    Properties {
        _ColorTex ("Color Texture", 2D) = "white" {}
        _AlphaTex ("Alpha Texture", 2D) = "white" {}
    }

    SubShader {
        Pass {
            Blend One Zero
            ZWrite Off
            ZTest Always
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _ColorTex;
            float4 _ColorTex_ST;

            sampler2D _AlphaTex;
            
            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _ColorTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target {
                return fixed4(tex2D(_ColorTex, i.uv).xyz, tex2D(_AlphaTex, i.uv).a);
            }
            ENDCG
        }
    }
}
