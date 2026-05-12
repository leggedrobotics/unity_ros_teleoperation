//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

Shader "LookingGlass/DOF/Box Blur"
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
			#pragma multi_compile _ _HORIZONTAL_ONLY

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
			float blurPassNum;
			float blurSize;
			float4 dofParams;
				// x: 1.0 / (dofparams.x - dofparams.y),
				// y: dofparams.y,
				// z: dofparams.z,
				// w: 1.0 / (dofparams.w - dofparams.z)
			float focalLength;
				// capture.GetAdjustedDistance());
			float4 tile;
			float4 viewPortion;
			float4 ProjParams;

			float4 getBounds(float2 uv) {
				float2 nuv = floor(uv / viewPortion.xy * tile.xy) / tile.xy * viewPortion.xy;
				float2 txl = _MainTex_TexelSize.xy * 2.0;
				return float4(
					nuv.x + txl.x, 
					nuv.y + txl.y,
					nuv.x + viewPortion.x / tile.x - txl.x,
					nuv.y + viewPortion.y / tile.y - txl.y
				);
			}

			float2 checkBounds(float2 uv, float4 bounds) {
				return min(max(uv, bounds.xy), bounds.zw);
			}

			float depthDist(float depthSample) {
				return
					ProjParams.y + depthSample * 
					(ProjParams.z - ProjParams.y);
			}

			v2f vert (appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target {
				float4 bounds = getBounds(i.uv);

				// sample the color and save the depth to alpha
				// note: the number of samples seems to be trivial in profiler
				// 3 4 5
				// 1 0 2
				// 6 7 8
				// or
				// 3 1 0 2 4 if horizontal only 
				float2 txl = _MainTex_TexelSize.xy * blurSize;
				float4 color  = tex2D(_MainTex, i.uv);
				float4 color1 = tex2D(_MainTex, checkBounds(i.uv + txl * float2( 1,  0), bounds));
				float4 color2 = tex2D(_MainTex, checkBounds(i.uv + txl * float2(-1,  0), bounds));
				#if !defined(_HORIZONTAL_ONLY)
				float4 color3 = tex2D(_MainTex, checkBounds(i.uv + txl * float2(-1,  1), bounds));
				float4 color4 = tex2D(_MainTex, checkBounds(i.uv + txl * float2( 0,  1), bounds));
				float4 color5 = tex2D(_MainTex, checkBounds(i.uv + txl * float2( 1,  1), bounds));
				float4 color6 = tex2D(_MainTex, checkBounds(i.uv + txl * float2(-1, -1), bounds));
				float4 color7 = tex2D(_MainTex, checkBounds(i.uv + txl * float2( 0, -1), bounds));
				float4 color8 = tex2D(_MainTex, checkBounds(i.uv + txl * float2( 1, -1), bounds));
				#else
				float4 color3 = tex2D(_MainTex, checkBounds(i.uv + txl * float2(-2,  0), bounds));
				float4 color4 = tex2D(_MainTex, checkBounds(i.uv + txl * float2( 2,  0), bounds));
				#endif


				// do the blur
				color.xyz += color1.xyz;
				color.xyz += color2.xyz;
				color.xyz += color3.xyz;
				color.xyz += color4.xyz;
				#if !defined(_HORIZONTAL_ONLY)
				color.xyz += color5.xyz;
				color.xyz += color6.xyz;
				color.xyz += color7.xyz;
				color.xyz += color8.xyz;
				color.xyz *= 0.111111111111;
				#else
				color.xyz *= 0.2;
				#endif

				// take the nearest coc
				color.a = min(color.a, color1.a);
				color.a = min(color.a, color2.a);
				color.a = min(color.a, color3.a);
				color.a = min(color.a, color4.a);
				#if !defined(_HORIZONTAL_ONLY)
				color.a = min(color.a, color5.a);
				color.a = min(color.a, color6.a);
				color.a = min(color.a, color7.a);
				color.a = min(color.a, color8.a);
				#endif

				// second sampling to eliminate ghosting.
				// only using the depth here.
				txl = _MainTex_TexelSize.xy * (blurSize * 2.0);
				color1 = tex2D(_MainTex, checkBounds(i.uv + txl * float2(-1,  0), bounds));
				color2 = tex2D(_MainTex, checkBounds(i.uv + txl * float2( 1,  0), bounds));
				#if !defined(_HORIZONTAL_ONLY)
				color3 = tex2D(_MainTex, checkBounds(i.uv + txl * float2(-1,  1), bounds));
				color4 = tex2D(_MainTex, checkBounds(i.uv + txl * float2( 0,  1), bounds));
				color5 = tex2D(_MainTex, checkBounds(i.uv + txl * float2( 1,  1), bounds));
				color6 = tex2D(_MainTex, checkBounds(i.uv + txl * float2(-1, -1), bounds));
				color7 = tex2D(_MainTex, checkBounds(i.uv + txl * float2( 0, -1), bounds));
				color8 = tex2D(_MainTex, checkBounds(i.uv + txl * float2( 1, -1), bounds));
				#else
				color3 = tex2D(_MainTex, checkBounds(i.uv + txl * float2(-2,  0), bounds));
				color4 = tex2D(_MainTex, checkBounds(i.uv + txl * float2( 2,  0), bounds));
				float4 color5 = tex2D(_MainTex, checkBounds(i.uv + txl * float2( 0,  1), bounds));
				float4 color6 = tex2D(_MainTex, checkBounds(i.uv + txl * float2( 0, -1), bounds));
				float4 color7 = tex2D(_MainTex, checkBounds(i.uv + txl * float2( 1,  1), bounds));
				float4 color8 = tex2D(_MainTex, checkBounds(i.uv + txl * float2( 1, -1), bounds));
				float4 color9 = tex2D(_MainTex, checkBounds(i.uv + txl * float2(-1,  1), bounds));
				float4 color10= tex2D(_MainTex, checkBounds(i.uv + txl * float2(-1, -1), bounds));
				#endif

				// take the nearest coc again, but with a bigger area
				color.a = min(color.a, color1.a);
				color.a = min(color.a, color2.a);
				color.a = min(color.a, color3.a);
				color.a = min(color.a, color4.a);
				color.a = min(color.a, color5.a);
				color.a = min(color.a, color6.a);
				color.a = min(color.a, color7.a);
				color.a = min(color.a, color8.a);
				#if defined(_HORIZONTAL_ONLY)
				color.a = min(color.a, color9.a);
				color.a = min(color.a, color10.a);
				#endif
 
				// this is the same as the coc = coc < 0 ? line in Final Pass
				// but for positive values only
				float coc = (depthDist(color.a) - focalLength);
				float cocForeground = (coc - dofParams.y) * dofParams.x;
				float cocBackground = (coc - dofParams.z) * dofParams.w;

				// 3 passes, should be 0-3 range
				cocForeground *= 3.0;
				cocBackground *= 3.0;

				// the foreground blur should blend with the bg blur
				coc = saturate(cocForeground - blurPassNum) + saturate(cocBackground - blurPassNum);
				coc = saturate(coc);
				color.a = coc;
				return color;
			}
			ENDCG
		}
	}
}