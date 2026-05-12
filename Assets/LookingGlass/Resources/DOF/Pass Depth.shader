//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

Shader "LookingGlass/DOF/Pass Depth"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
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
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex; 
			float4 _MainTex_ST;
			float4 _MainTex_TexelSize;
			float4 ProjParams;

			sampler2D QuiltDepth;

			float linearDepth(float depthSample) {
				return 
					ProjParams.y * depthSample /
					(depthSample * (ProjParams.y - ProjParams.z) + ProjParams.z);
			}

			v2f vert (appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target {
				// sample depth
				float4 color = tex2D(_MainTex, i.uv);
				#ifdef UNITY_REVERSED_Z
				float depth = 1.0 - SAMPLE_DEPTH_TEXTURE(QuiltDepth, i.uv);
				#else
				float depth = SAMPLE_DEPTH_TEXTURE(QuiltDepth, i.uv);
				#endif
				depth = linearDepth(depth);
				color.a = depth;
				return color;
			}
			ENDCG
		}
	}
}