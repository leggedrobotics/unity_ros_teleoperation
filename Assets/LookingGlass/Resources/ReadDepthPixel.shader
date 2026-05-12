//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

Shader "LookingGlass/ReadDepthPixel" {

	Properties {
		_MainTex ("Texture", 2D) = "white" {}
	}

    SubShader {
        Pass {
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

			sampler2D _MainTex;
			float4 _MainTex_ST;
            float4 samplePoint;

            // todo: remove uv and pos cause not used
            v2f vert (appdata v) {
                v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target {
                // a simple passthru but using a "sample point" instead of uvs
                fixed4 c = tex2D(_MainTex, samplePoint.xy);
                return c;
            }
            ENDCG
        }
    }
}