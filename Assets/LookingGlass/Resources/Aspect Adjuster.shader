Shader "LookingGlass/Aspect Adjuster" {
    Properties {
        _MainTex ("Texture", 2D) = "black" {}

        //NOTE: This should be greater than 0:
        aspect ("Aspect", Float) = 1

        //sourceUVRect.xy = UV start coordinate
        //sourceUVRect.zw = UV width and height
        sourceUVRect ("Source UV Rect", Vector) = (0, 0, 1, 1)

        //targetUVRect.xy = UV start coordinate
        //targetUVRect.zw = UV width and height
        targetUVRect ("Target UV Rect", Vector) = (0, 0, 1, 1)
    }
    SubShader {
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct VertexInput {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VertexOutput {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            uniform sampler2D _MainTex;
            uniform float aspect;
            uniform float4 sourceUVRect;
            uniform float4 targetUVRect;

            VertexOutput vert(VertexInput v) {
                VertexOutput o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            //This is the pure 0-1 function:
            
            //floor(|x|) results in a segment at y = 0 from x = -1 to x = 2,
            //  Since we don't need the rest of the graph, we just care about the first step centered on x = 0,
            //  We can double the rate of change to make it:
            //  floor(|2x|)
            //  This makes the function evaluate = 0 from x = -0.5 to +0.5.
            //  Then if we horizontally shift it to the right by 0.5,
            //  We conveniently have a function that evaluates to 0 between x = 0 and x = 1.
            //  Since the rest of it is > 0, if we flip the signs, this allows us to clip everything outside of the 0 to 1 range.
            // f(x) = -floor(|2(x - 0.5)|)
            //              x = 0
            //              |
            //  ________    ________    ________ y = 0
            //          ____|       ____
            //      ____    |           ____
            //  ____        |               ____
            inline float rangeFunction(float x) {
                return -floor(abs(2 * (x - 0.5)));
            }

            //This uses typical Pre-Calculus function (f(x)) transformations for horizontal stretches and shifts:
            //       f(1        )
            //g(x) =  (- (x - S)) results in a horizontal STRETCH by D, and a horizontal shift by S units.
            //        (D        )

            //Translation to HLSL variables below:
            //D = totalDelta
            //S = start
            inline float rangeFunction(float x, float start, float end) {
                float totalDelta = end - start;
                return rangeFunction((1 / totalDelta) * (x - start));
            }

            fixed4 frag(VertexOutput i) : SV_Target {
                //NOTE: i.uv represents the final full-screen 0-1 UV space that we clip based on (below).
                //  Cause we only wanna render into 1 tile of the quilt, and clip everything else off.
                //transformedUV represents the UVs across the tile we're sampling FROM!

                //So, let's start off with a perfect 1:1 mapping with linear interpolation:
                //  As our i.uv goes from the beginning to the end of our targetUVRect (.xy to .zw),
                //  our transformedUV has a starting value here of sourceUVRect .xy to .zw.
                float2 transformedUV = lerp(sourceUVRect.xy, sourceUVRect.xy + sourceUVRect.zw, (i.uv - targetUVRect.xy) / targetUVRect.zw);

                //THEN we can squish/squash it:
                float2 sourceCenterUV  = sourceUVRect.xy + sourceUVRect.zw / 2;
                if (aspect > 1) {
                    transformedUV.x -= sourceCenterUV.x;
                    transformedUV.x /= aspect;
                    transformedUV.x += sourceCenterUV.x;
                } else if (aspect < 1) {
                    transformedUV.y -= sourceCenterUV.y;
                    transformedUV.y *= aspect;
                    transformedUV.y += sourceCenterUV.y;
                }

                float horizontalRangeValue = rangeFunction(i.uv.x, targetUVRect.x, targetUVRect.x + targetUVRect.z);
                clip(horizontalRangeValue);

                float verticalRangeValue = rangeFunction(i.uv.y, targetUVRect.y, targetUVRect.y + targetUVRect.w);
                clip(verticalRangeValue);

                return tex2D(_MainTex, transformedUV);
            }
            ENDHLSL
        }
    }
}
