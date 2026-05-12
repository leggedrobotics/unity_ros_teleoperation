#ifndef UNITY_POSTFX_SAMPLING
#define UNITY_POSTFX_SAMPLING

#include "StdLib.hlsl"

//Looking Glass:
float4 hp_quiltViewSize;
int hp_useQuilt;
float2 ViewClampedUV(float2 uvOriginal, float2 uvNew, float2 texelSize) {
    if (hp_useQuilt == 0)
        return uvNew;
    // first check if our new uvs bleed onto the other view
    int2 viewIndexOriginal = uvOriginal / hp_quiltViewSize.xy;
    int2 viewIndexNew = uvNew / hp_quiltViewSize.xy;
    int2 compare = saturate(abs(viewIndexOriginal - viewIndexNew)); // 0 if same view, 1 if different
    float2 uv = float2(
        compare.x * uvOriginal.x + (1.0 - compare.x) * uvNew.x,
        compare.y * uvOriginal.y + (1.0 - compare.y) * uvNew.y
    );
    // then let's check if this was downsampled from a quilt.
    // if so, if we're within 1 pixel of the border, jump back to 1 pixel away
    // first, get the value of the closest border in uv coords
    float2 border = round(uv / hp_quiltViewSize.xy) * hp_quiltViewSize.xy;
    float2 distFromBorder = uv - border;
    float2 borderSign = FastSign(distFromBorder);
    float2 minDistFromBorder = (borderSign + saturate(borderSign) * 0.5) * texelSize;
    float2 distToPush = minDistFromBorder - distFromBorder;
    distToPush *= saturate(abs(FastSign(distToPush) + borderSign)); // will return 1 if they're in the same direction
    uv += distToPush;
    return uv;
}

// Better, temporally stable box filtering
// [Jimenez14] http://goo.gl/eomGso
// . . . . . . .
// . A . B . C .
// . . D . E . .
// . F . G . H .
// . . I . J . .
// . K . L . M .
// . . . . . . .
half4 DownsampleBox13Tap(TEXTURE2D_ARGS(tex, samplerTex), float2 uv, float2 texelSize)
{
    half4 A = SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv + texelSize * float2(-1.0, -1.0), texelSize)));
    half4 B = SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv + texelSize * float2( 0.0, -1.0), texelSize)));
    half4 C = SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv + texelSize * float2( 1.0, -1.0), texelSize)));
    half4 D = SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv + texelSize * float2(-0.5, -0.5), texelSize)));
    half4 E = SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv + texelSize * float2( 0.5, -0.5), texelSize)));
    half4 F = SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv + texelSize * float2(-1.0,  0.0), texelSize)));
    half4 G = SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv                                 , texelSize)));
    half4 H = SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv + texelSize * float2( 1.0,  0.0), texelSize)));
    half4 I = SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv + texelSize * float2(-0.5,  0.5), texelSize)));
    half4 J = SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv + texelSize * float2( 0.5,  0.5), texelSize)));
    half4 K = SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv + texelSize * float2(-1.0,  1.0), texelSize)));
    half4 L = SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv + texelSize * float2( 0.0,  1.0), texelSize)));
    half4 M = SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv + texelSize * float2( 1.0,  1.0), texelSize)));

    half2 div = (1.0 / 4.0) * half2(0.5, 0.125);

    half4 o = (D + E + I + J) * div.x;
    o += (A + B + G + F) * div.y;
    o += (B + C + H + G) * div.y;
    o += (F + G + L + K) * div.y;
    o += (G + H + M + L) * div.y;

    return o;
}

// Standard box filtering
half4 DownsampleBox4Tap(TEXTURE2D_ARGS(tex, samplerTex), float2 uv, float2 texelSize)
{
    float4 d = texelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0);

    half4 s;
    s =  (SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv + d.xy, texelSize))));
    s += (SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv + d.zy, texelSize))));
    s += (SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv + d.xw, texelSize))));
    s += (SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv + d.zw, texelSize))));

    return s * (1.0 / 4.0);
}

// 9-tap bilinear upsampler (tent filter)
half4 UpsampleTent(TEXTURE2D_ARGS(tex, samplerTex), float2 uv, float2 texelSize, float4 sampleScale)
{
    float4 d = texelSize.xyxy * float4(1.0, 1.0, -1.0, 0.0) * sampleScale;

    half4 s;
    s =  SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv - d.xy, texelSize)));
    s += SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv - d.wy, texelSize))) * 2.0;
    s += SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv - d.zy, texelSize)));

    s += SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv + d.zw, texelSize))) * 2.0;
    s += SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv       , texelSize))) * 4.0;
    s += SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv + d.xw, texelSize))) * 2.0;

    s += SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv + d.zy, texelSize)));
    s += SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv + d.wy, texelSize))) * 2.0;
    s += SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv + d.xy, texelSize)));

    return s * (1.0 / 16.0);
}

// Standard box filtering
half4 UpsampleBox(TEXTURE2D_ARGS(tex, samplerTex), float2 uv, float2 texelSize, float4 sampleScale)
{
    float4 d = texelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0) * (sampleScale * 0.5);

    half4 s;
    s =  (SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv + d.xy, texelSize))));
    s += (SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv + d.zy, texelSize))));
    s += (SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv + d.xw, texelSize))));
    s += (SAMPLE_TEXTURE2D(tex, samplerTex, UnityStereoTransformScreenSpaceTex(ViewClampedUV(uv, uv + d.zw, texelSize))));

    return s * (1.0 / 4.0);
}

#endif // UNITY_POSTFX_SAMPLING
