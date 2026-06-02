// Helper functions for point cloud shaders

// Unpacks a 32-bit RGBA color into a float4
float4 UnpackRGBA(float rgba)
{
    int4 unpackedColor;
    int unpacked = asint(rgba);

    unpackedColor.r = unpacked >> 16 & 0xFF;
    unpackedColor.g = unpacked >> 8 & 0xFF;
    unpackedColor.b = unpacked & 0xFF;
    unpackedColor.a = 255;

    return unpackedColor / 255.0f;
}

float4 qmul(float4 q1, float4 q2)
{
    return float4(
        q2.xyz * q1.w + q1.xyz * q2.w + cross(q1.xyz, q2.xyz),
        q1.w * q2.w - dot(q1.xyz, q2.xyz)
    );
}

// Vector rotation with a quaternion
// http://mathworld.wolfram.com/Quaternion.html
float3 rotate_vector(float3 v, float4 r)
{
    float4 r_c = r * float4(-1, -1, -1, 1);
    return qmul(r, qmul(float4(v, 0), r_c)).xyz;
}

float3x3 CalcMatrixFromRotationScale(float4 rot, float3 scale)
{
    // Default scales are log scales
    scale = exp(scale);
    float3x3 ms = float3x3(
        scale.x, 0, 0,
        0, scale.y, 0,
        0, 0, scale.z
    );

    // Quaternions are stored as (w, x, y, z)
    float w = rot.x;
    float x = rot.y;
    float y = rot.z;
    float z = rot.w;
    float3x3 mr = float3x3(
        1-2*(y*y + z*z),   2*(x*y - w*z),   2*(x*z + w*y),
          2*(x*y + w*z), 1-2*(x*x + z*z),   2*(y*z - w*x),
          2*(x*z - w*y),   2*(y*z + w*x), 1-2*(x*x + y*y)
    );
    return mul(mr, ms);
}

void CalcCovariance3D(float3x3 rotMat, out float3 sigma0, out float3 sigma1)
{
    float3x3 sig = mul(rotMat, transpose(rotMat));
    sigma0 = float3(sig._m00, sig._m01, sig._m02);
    sigma1 = float3(sig._m11, sig._m12, sig._m22);
}


// from "EWA Splatting" (Zwicker et al 2002) eq. 31
float3 CalcCovariance2D(float3 worldPos, float3 cov3d0, float3 cov3d1, float4x4 matrixV, float4x4 matrixP, float4 screenParams)
{
    float4x4 viewMatrix = matrixV;
    float3 viewPos = mul(viewMatrix, float4(worldPos, 1)).xyz;

    // this is needed in order for splats that are visible in view but clipped "quite a lot" to work
    float aspect = matrixP._m00 / matrixP._m11;
    float tanFovX = rcp(matrixP._m00);
    float tanFovY = rcp(matrixP._m11 * aspect);
    float limX = 1.3 * tanFovX;
    float limY = 1.3 * tanFovY;
    viewPos.x = clamp(viewPos.x / viewPos.z, -limX, limX) * viewPos.z;
    viewPos.y = clamp(viewPos.y / viewPos.z, -limY, limY) * viewPos.z;

    float focal = screenParams.x * matrixP._m00 / 2;

    float3x3 J = float3x3(
        focal / viewPos.z, 0, -(focal * viewPos.x) / (viewPos.z * viewPos.z),
        0, focal / viewPos.z, -(focal * viewPos.y) / (viewPos.z * viewPos.z),
        0, 0, 0
    );
    float3x3 W = (float3x3)viewMatrix;
    float3x3 T = mul(J, W);
    float3x3 V = float3x3(
        cov3d0.x, cov3d0.y, cov3d0.z,
        cov3d0.y, cov3d1.x, cov3d1.y,
        cov3d0.z, cov3d1.y, cov3d1.z
    );
    float3x3 cov = mul(T, mul(V, transpose(T)));

    // Low pass filter to make each splat at least 1px size.
    cov._m00 += 0.3;
    cov._m11 += 0.3;
    return float3(cov._m00, cov._m01, cov._m11);
}


void DecomposeCovariance(float3 cov2d, out float2 v1, out float2 v2)
{
    // same as in antimatter15/splat
    float diag1 = cov2d.x, diag2 = cov2d.z, offDiag = cov2d.y;
    float mid = 0.5f * (diag1 + diag2);
    float radius = length(float2((diag1 - diag2) / 2.0, offDiag));
    float lambda1 = mid + radius;
    float lambda2 = max(mid - radius, 0.1);
    float2 diagVec = normalize(float2(offDiag, lambda1 - diag1));
    diagVec.y = -diagVec.y;
    float maxSize = 4096.0;
    v1 = min(sqrt(2.0 * lambda1), maxSize) * diagVec;
    v2 = min(sqrt(2.0 * lambda2), maxSize) * float2(diagVec.y, -diagVec.x);

}

// Scales the uv point of a unit circle by the 3D scale and rotation 
float2 ApplyCovariance(float3 scale, float4 orientation, float3 pos, float pointScale, float2 uv)
{
    float3x3 mat = CalcMatrixFromRotationScale(orientation, scale);


    float3 cov3d0, cov3d1;
    CalcCovariance3D(mat, cov3d0, cov3d1);
    float pointScale2 = pointScale * pointScale;
    cov3d0 *= pointScale2;
    cov3d1 *= pointScale2;
    float4 _VecScreenParams = float4(_ScreenParams.x, _ScreenParams.y, 0, 0);
    float3 cov2d = CalcCovariance2D(pos, cov3d0, cov3d1, UNITY_MATRIX_MV, UNITY_MATRIX_P, _VecScreenParams);
    
    // float2 view;


    float3x3 dcov = float3x3(
        9.0, 5.0, 0.0,
        5.0, 4.0, 0.0,
        0.0, 0.0, 1.0
    );


    // float e1 = 12.090;
    // float e2 = 0.910;

    // float sq2 = sqrt(2.0)/2.0;

    // float2 v1 = float2(sq2, sq2);
    // float2 v2 = float2(-sq2, sq2);

    float2 v1, v2;
    DecomposeCovariance(cov2d, v2, v1);



    // float3x3 invMat = inverse(mat);

    // float3x3 invCov = mul(invMat, mul(3dcov, transpose(invMat)));




    return (uv.x * v1 + uv.y * v2) * 2 / _ScreenParams.xy;

}