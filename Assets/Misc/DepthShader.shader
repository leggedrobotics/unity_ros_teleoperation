// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/DepthShader"
{
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
        _RenderTex ("Render Texture", 2D) = "white" {}
		_DepthLevel ("Depth Level", Range(1, 3)) = 3
        _DepthClip ("Depth Clip", Range(0, 1)) = .5
	}
	SubShader
	{
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			
			uniform sampler2D _MainTex;
            uniform sampler2D _RenderTex;
			uniform sampler2D _CameraDepthTexture;
			uniform fixed _DepthLevel;
			uniform half4 _MainTex_TexelSize;
            uniform fixed _DepthClip;

			struct input
			{
				float4 pos : POSITION;
				half2 uv : TEXCOORD0;
			};

			struct output
			{
				float4 pos : SV_POSITION;
				half2 uv : TEXCOORD0;
			};


			output vert(input i)
			{
				output o;
				o.pos = UnityObjectToClipPos(i.pos);
				o.uv = MultiplyUV(UNITY_MATRIX_TEXTURE0, i.uv);

				// why do we need this? cause sometimes the image I get is flipped. see: http://docs.unity3d.com/Manual/SL-PlatformDifferences.html
				#if UNITY_UV_STARTS_AT_TOP
				if (_MainTex_TexelSize.y < 0)
						o.uv.y = 1 - o.uv.y;
				#endif

				return o;
			}
			
			fixed4 frag(output o) : COLOR
			{
				float depth = UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, o.uv));
				depth = pow(Linear01Depth(depth), _DepthLevel);

                fixed2 uv = fixed2(o.uv.x, 1-o.uv.y);

                fixed4 color = tex2D(_RenderTex, uv);
                color.a = depth>_DepthClip?1:0;
				return color;
			}
			
			ENDCG
		}
	} 
}