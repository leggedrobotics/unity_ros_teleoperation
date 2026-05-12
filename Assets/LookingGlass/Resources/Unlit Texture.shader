Shader "LookingGlass/Tests/Unlit Texture" {
    Properties {
        _MainTex ("Texture", 2D) = "black" {}
    }
    SubShader {
        Cull Off
        ZWrite On
        ZTest Always

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct VertexInput {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VertexOutput {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;

            VertexOutput vert(VertexInput v) {
                VertexOutput o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(VertexOutput i) : SV_Target {
                fixed4 color = tex2D(_MainTex, i.uv);
                return color;
            }
            ENDCG
        }
    }
}
