Shader "Unlit/RGBDMesh"
{
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


            struct v2f
            {
                float4 pos: SV_POSITION;
                float4 color: COLOR0;
            };

            struct lidardata
            {
                float3 position;
                float3 rgb;
            };

            StructuredBuffer<float3> _Positions;
            StructuredBuffer<lidardata> _LidarData;

            uniform uint _BaseVertexIndex;
            uniform float _PointSize;
            uniform float4x4 _ObjectToWorld;
            uniform float4 _ColorMin;
            uniform float4 _ColorMax;

            v2f vert (uint vertexID: SV_VertexID, uint instanceID: SV_InstanceID)
            {
                v2f o;
                float3 pos = _LidarData[instanceID].position;
                float2 uv = _Positions[_BaseVertexIndex + vertexID] * _PointSize;
                uv /= float2(_ScreenParams.x/_ScreenParams.y, 1);
                float4 wpos = mul(_ObjectToWorld, float4(pos, 1.0f));
                o.pos = mul(UNITY_MATRIX_VP, wpos) + float4(uv,0,0);
                o.color = float4(_LidarData[instanceID].rgb, 1.0f);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}
