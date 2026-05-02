Shader "Unlit/Splat"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma exclude_renderers d3d11 gles
            #pragma vertex vert
            #pragma fragment frag


            #include "UnityCG.cginc"
            #include "Assets/Components/Lidar/Materials/Shaders/PointHelper.cginc"


            struct v2f
            {
                float4 pos: SV_POSITION;
                float4 color: COLOR0;
            };

            struct splatdata
            {
                float3 position;
                int color;
                float3 scale;
                float4 rotation;
                float3 normal;
                float3 fc_dc;
                float opacity;
            };

            StructuredBuffer<float3> _Positions;
            StructuredBuffer<splatdata> _PointData;

            uniform uint _BaseVertexIndex;
            uniform float _PointSize;
            uniform float4x4 _ObjectToWorld;
            uniform float4 _ColorMin;
            uniform float4 _ColorMax;

            v2f vert (uint vertexID: SV_VertexID, uint instanceID: SV_InstanceID)
            {
                v2f o;
                float3 pos = _PointData[instanceID].position;
                float2 uv = _Positions[_BaseVertexIndex + vertexID];


                float3 centerWorldPos = mul(_ObjectToWorld, float4(pos, 1.0f)).xyz;
                float4 centerClipPos = mul(UNITY_MATRIX_VP, float4(centerWorldPos, 1.0f));
                
                float4 rot = float4(0, 0, 0, 1);

                float3 scale = _PointData[instanceID].scale;
                scale.y = scale.z;
                scale.z = _PointData[instanceID].scale.y;

                // float2 delta = ApplyCovariance(_PointData[instanceID].scale, rot, pos, _PointSize, uv);
                float2 delta = ApplyCovariance(_PointData[instanceID].scale, _PointData[instanceID].rotation, pos, _PointSize, uv);

                float4 wpos = mul(_ObjectToWorld, float4(pos, 1.0f));
                o.pos = centerClipPos;
                o.pos.xy += delta * centerClipPos.w;

                if(_PointData[instanceID].opacity < 0.9)
                {
                    o.pos = asfloat(0x7fc00000);
                }

                o.color = UnpackRGBA(_PointData[instanceID].color);
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
