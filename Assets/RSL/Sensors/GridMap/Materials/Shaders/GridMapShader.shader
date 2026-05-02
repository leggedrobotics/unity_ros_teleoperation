Shader "Unlit/ROS/GridMapShader"
{
    Properties
    {
        _GradientTex ("Gradient (RGB)", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha

        Cull Off
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Assets/Components/Lidar/Materials/Shaders/PointHelper.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                uint vertexID : SV_VertexID;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                uint vertexID : TEXCOORD0;
            };

            StructuredBuffer<float> _GridData;
            StructuredBuffer<float> _IntensityData;

            uniform float4x4 _ObjectToWorld;
            sampler2D _GradientTex;
            uniform uint _BaseVertexIndex;
            uniform float _IntensityMin;
            uniform float _IntensityMax;

            uniform float _Opacity;

            v2f vert(appdata v) {
                float3 worldPos = v.vertex.xyz;
                worldPos.y += _GridData[v.vertexID];
                v2f o;
                float4 objectPos = mul(_ObjectToWorld, float4(worldPos, 1));
                o.pos = UnityObjectToClipPos(objectPos);
                o.vertexID = v.vertexID;
                return o;
            } 

            fixed4 frag(v2f i) : SV_Target {
                float intensity = (_IntensityData[i.vertexID] - _IntensityMin) / (_IntensityMax - _IntensityMin);
                intensity = clamp(intensity, 0.0, 1.0);
                fixed4 color = tex2D(_GradientTex, float2(intensity, 0));
                color.a = _Opacity;
                return color;
            }
            ENDCG
        }
    }
}
