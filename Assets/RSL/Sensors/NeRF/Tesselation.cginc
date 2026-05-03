#if defined(SHADER_API_D3D11) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_VULKAN) || defined(SHADER_API_METAL) || defined(SHADER_API_PSSL)
#define UNITY_CAN_COMPILE_TESSELLATION 1
#   define UNITY_domain                 domain
#   define UNITY_partitioning           partitioning
#   define UNITY_outputtopology         outputtopology
#   define UNITY_patchconstantfunc      patchconstantfunc
#   define UNITY_outputcontrolpoints    outputcontrolpoints
#endif
 
 
 
// The structure definition defines which variables it contains.
// This example uses the Attributes structure as an input structure in
// the vertex shader.
 
// vertex to fragment struct
struct Varyings
{
	float4 color : COLOR;
	float3 normal : NORMAL;
	float4 vertex : SV_POSITION;
	float2 uv : TEXCOORD0;
	float4 noise : TEXCOORD1;
};
 
 
// tessellation data
struct TessellationFactors
{
	float edge[3] : SV_TessFactor;
	float inside : SV_InsideTessFactor;
};
 
// Extra vertex struct
struct ControlPoint
{
	float4 vertex : INTERNALTESSPOS;
	float2 uv : TEXCOORD0;
	float4 color : COLOR;
	float3 normal : NORMAL;
};
 
// the original vertex struct
struct Attributes
{
	float4 vertex : POSITION;
	float3 normal : NORMAL;
	float2 uv : TEXCOORD0;
	float4 color : COLOR;
 
};
 
// tessellation variables, add these to your shader properties
float _Tess;
float _MaxTessDistance;
 
// info so the GPU knows what to do (triangles) and how to set it up , clockwise, fractional division
// hull takes the original vertices and outputs more
[UNITY_domain("tri")]
[UNITY_outputcontrolpoints(3)]
[UNITY_outputtopology("triangle_cw")]
[UNITY_partitioning("fractional_odd")]
//[UNITY_partitioning("fractional_even")]
//[UNITY_partitioning("pow2")]
//[UNITY_partitioning("integer")]
[UNITY_patchconstantfunc("patchConstantFunction")]
ControlPoint hull(InputPatch<ControlPoint, 3> patch, uint id : SV_OutputControlPointID)
{
	return patch[id];
}
 
TessellationFactors UnityCalcTriEdgeTessFactors (float3 triVertexFactors)
{
    TessellationFactors tess;
    tess.edge[0] = 0.5 * (triVertexFactors.y + triVertexFactors.z);
    tess.edge[1] = 0.5 * (triVertexFactors.x + triVertexFactors.z);
    tess.edge[2] = 0.5 * (triVertexFactors.x + triVertexFactors.y);
    tess.inside = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0f;
    return tess;
}
 
// fade tessellation at a distance
float CalcDistanceTessFactor(float4 vertex, float minDist, float maxDist, float tess)
{
				float3 worldPosition = mul(unity_ObjectToWorld, vertex).xyz;
				float dist = distance(worldPosition, _WorldSpaceCameraPos);
				float f = clamp(1.0 - (dist - minDist) / (maxDist - minDist), 0.01, 1.0);
 
				return f * tess;
}
 
TessellationFactors DistanceBasedTess(float4 v0, float4 v1, float4 v2, float minDist, float maxDist, float tess)
{
				float3 f;
				f.x = CalcDistanceTessFactor(v0, minDist, maxDist, tess);
				f.y = CalcDistanceTessFactor(v1, minDist, maxDist, tess);
				f.z = CalcDistanceTessFactor(v2, minDist, maxDist, tess);
 
				return UnityCalcTriEdgeTessFactors(f);
}
 
 
 
float UnityCalcEdgeTessFactor (float3 wpos0, float3 wpos1, float edgeLen)
{
    // distance to edge center
    float dist = distance (0.5 * (wpos0+wpos1), _WorldSpaceCameraPos);
    // length of the edge
    float len = distance(wpos0, wpos1);
    // edgeLen is approximate desired size in pixels
    float f = max(len * _ScreenParams.y / (edgeLen * dist), 1.0);
    return f;
}
 
 
float UnityDistanceFromPlane (float3 pos, float4 plane)
{
    float d = dot (float4(pos,1.0f), plane);
    return d;
}
 
 
// Returns true if triangle with given 3 world positions is outside of camera's view frustum.
// cullEps is distance outside of frustum that is still considered to be inside (i.e. max displacement)
bool UnityWorldViewFrustumCull (float3 wpos0, float3 wpos1, float3 wpos2, float cullEps)
{
    float4 planeTest;
 
    // left
    planeTest.x = (( UnityDistanceFromPlane(wpos0, unity_CameraWorldClipPlanes[0]) > -cullEps) ? 1.0f : 0.0f ) +
                  (( UnityDistanceFromPlane(wpos1, unity_CameraWorldClipPlanes[0]) > -cullEps) ? 1.0f : 0.0f ) +
                  (( UnityDistanceFromPlane(wpos2, unity_CameraWorldClipPlanes[0]) > -cullEps) ? 1.0f : 0.0f );
    // right
    planeTest.y = (( UnityDistanceFromPlane(wpos0, unity_CameraWorldClipPlanes[1]) > -cullEps) ? 1.0f : 0.0f ) +
                  (( UnityDistanceFromPlane(wpos1, unity_CameraWorldClipPlanes[1]) > -cullEps) ? 1.0f : 0.0f ) +
                  (( UnityDistanceFromPlane(wpos2, unity_CameraWorldClipPlanes[1]) > -cullEps) ? 1.0f : 0.0f );
    // top
    planeTest.z = (( UnityDistanceFromPlane(wpos0, unity_CameraWorldClipPlanes[2]) > -cullEps) ? 1.0f : 0.0f ) +
                  (( UnityDistanceFromPlane(wpos1, unity_CameraWorldClipPlanes[2]) > -cullEps) ? 1.0f : 0.0f ) +
                  (( UnityDistanceFromPlane(wpos2, unity_CameraWorldClipPlanes[2]) > -cullEps) ? 1.0f : 0.0f );
    // bottom
    planeTest.w = (( UnityDistanceFromPlane(wpos0, unity_CameraWorldClipPlanes[3]) > -cullEps) ? 1.0f : 0.0f ) +
                  (( UnityDistanceFromPlane(wpos1, unity_CameraWorldClipPlanes[3]) > -cullEps) ? 1.0f : 0.0f ) +
                  (( UnityDistanceFromPlane(wpos2, unity_CameraWorldClipPlanes[3]) > -cullEps) ? 1.0f : 0.0f );
 
    // has to pass all 4 plane tests to be visible
    return !all (planeTest);
}
 
 
// Desired edge length based tessellation:
// Approximate resulting edge length in pixels is "edgeLength".
// Does not take viewing FOV into account, just flat out divides factor by distance.
TessellationFactors UnityEdgeLengthBasedTess (float4 v0, float4 v1, float4 v2, float edgeLength)
{
    float3 pos0 = mul(unity_ObjectToWorld,v0).xyz;
    float3 pos1 = mul(unity_ObjectToWorld,v1).xyz;
    float3 pos2 = mul(unity_ObjectToWorld,v2).xyz;
    TessellationFactors tess;
    tess.edge[0] = UnityCalcEdgeTessFactor (pos1, pos2, edgeLength);
    tess.edge[1] = UnityCalcEdgeTessFactor (pos2, pos0, edgeLength);
    tess.edge[2] = UnityCalcEdgeTessFactor (pos0, pos1, edgeLength);
    tess.inside = (tess.edge[0] + tess.edge[1] + tess.edge[2]) / 3.0f;
    return tess;
}
 
 
// Same as UnityEdgeLengthBasedTess, but also does patch frustum culling:
// patches outside of camera's view are culled before GPU tessellation. Saves some wasted work.
TessellationFactors UnityEdgeLengthBasedTessCull (float4 v0, float4 v1, float4 v2, float edgeLength, float maxDisplacement)
{
    float3 pos0 = mul(unity_ObjectToWorld,v0).xyz;
    float3 pos1 = mul(unity_ObjectToWorld,v1).xyz;
    float3 pos2 = mul(unity_ObjectToWorld,v2).xyz;
    TessellationFactors tess;
 
    if (UnityWorldViewFrustumCull(pos0, pos1, pos2, maxDisplacement))
    {
        tess.edge[0] = 0.0f;
		tess.edge[1] = 0.0f;
		tess.edge[2] = 0.0f;
		tess.inside = 0.0f;
    }
    else
    {
        tess.edge[0] = UnityCalcEdgeTessFactor (pos1, pos2, edgeLength);
        tess.edge[1] = UnityCalcEdgeTessFactor (pos2, pos0, edgeLength);
        tess.edge[2] = UnityCalcEdgeTessFactor (pos0, pos1, edgeLength);
        tess.inside = (tess.edge[0] + tess.edge[1] + tess.edge[2]) / 3.0f;
    }
    return tess;
}
 
TessellationFactors patchConstantFunction(InputPatch<ControlPoint, 3> patch)
{
    float minDist = 2.0;
    float maxDist = _MaxTessDistance + minDist;
    TessellationFactors f;
 
    // distance based tesselation
    return DistanceBasedTess(patch[0].vertex, patch[1].vertex, patch[2].vertex, minDist, maxDist, _Tess);
 
    // continious tesselation based on _Tess being the edge length
   // return UnityEdgeLengthBasedTess(patch[0].vertex, patch[1].vertex, patch[2].vertex, _Tess);
    // continious tesselation based on _Tess being the edge length, with camrea culling
   // return UnityEdgeLengthBasedTessCull(patch[0].vertex, patch[1].vertex, patch[2].vertex, _Tess, _MaxTessDistance);
 
}
 
#define Interpolate(fieldName) v.fieldName = \
				patch[0].fieldName * barycentricCoordinates.x + \
				patch[1].fieldName * barycentricCoordinates.y + \
				patch[2].fieldName * barycentricCoordinates.z;
 
 
 
// second vertex, copy to shader
//Varyings vert(Attributes input)
//{
//	Varyings output;
//	// put your vertex manipulation here , ie: input.vertex.xyz += distortiontexture
//	output.vertex = TransformObjectToHClip(input.vertex.xyz);
//	output.color = input.color;
//	output.normal = input.normal;
//	output.uv = input.uv;
//	return output;
//}
 
 
 
// domain, copy to shader
//[UNITY_domain("tri")]
//Varyings domain(TessellationFactors factors, OutputPatch<ControlPoint, 3> patch, float3 barycentricCoordinates : SV_DomainLocation)
//{
//	Attributes v;
//
//
//	Interpolate(vertex)
//		Interpolate(uv)
//		Interpolate(color)
//		Interpolate(normal)
//
//		return vert(v);
//}