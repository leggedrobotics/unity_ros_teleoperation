//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

Shader "LookingGlass/DepthOnly" {

	Properties {
		_MainTex ("Texture", 2D) = "white" {}
	}

    SubShader {
        Tags { "RenderType"="Opaque" }
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

            struct v2f {
				float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
                half3 worldNormal : TEXCOORD1;
            };

			sampler2D _MainTex;
			float4 _MainTex_ST;

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldNormal = mul((float3x3)UNITY_MATRIX_V, o.worldNormal);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target {
                // get linear depth
                float d = LinearEyeDepth(i.pos.z); // depth in unity units
                float near = _ProjectionParams.y; 
                float far = _ProjectionParams.z;
                float ld = (d - near) / (far - near); // normalize it
                // encode depth and normals
                return EncodeDepthNormal(ld, i.worldNormal);
            }
            ENDCG
        }
    }
}