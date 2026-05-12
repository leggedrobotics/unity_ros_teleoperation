//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

//NOTE: The fragment shader was manually converted from GLSL to HLSL based on the
//  LookingGlassBridge's "Lenticular_RGBA_With_Aspect" shader (in OpenGLEmbeddedShaders.cpp), which has a copy under tools/glsl_converter/lent_lent.glsl.
//  See the LookingGlassBridge Git repo at:     https://github.com/Looking-Glass/LookingGlassBridge

//SEE: /docs/LKG Bridge Lenticular Shader Modifications.md
//  For documentation on updating this lenticular shader.

Shader "LookingGlass/Lenticular" {
    Properties {
        _MainTex("Texture", 2D) = "white" {}
    }

    SubShader {
        Tags { "RenderType" = "Opaque" }

        Pass {
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM

            #pragma multi_compile _ LKG_BILINEAR

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.6

            #include "UnityCG.cginc"
            //WARNING: I'm writing all the shader source code here,
            //  Instead of doing this,
            //  #include "Looking Glass.hlslinc"

            // Because Unity/Unity's shader compiler have horrible file detection for shader include files.
            //  Sometimes it recognizes the file changed, sometimes it doesn't, and you have no idea what
            //  version of the shader is even being run. Wastes a lot of time. Right-click -> Reimport doesn't work.

            // --- --- ---

            //NOTE: This shader was manually converted from GLSL to HLSL based on the
            //  LookingGlassBridge's "Lenticular_RGBA_With_Aspect" shader (in OpenGLEmbeddedShaders.cpp), which has a copy under tools/glsl_converter/lent_lent.glsl.
            //  See the LookingGlassBridge Git repo at:     https://github.com/Looking-Glass/LookingGlassBridge

            //SEE: /docs/LKG Bridge Lenticular Shader Modifications.md
            //  For documentation on updating this lenticular shader.

            #pragma multi_compile _ ANTI_ALIASING

            struct SubpixelCell {
                float ROffsetX;
                float ROffsetY;
                float GOffsetX;
                float GOffsetY;
                float BOffsetX;
                float BOffsetY;
            };

            uniform sampler2D _MainTex;
            uniform float4 _MainTex_ST;

            uniform float pitch;
            uniform float slope;
            uniform float center;
            uniform float subpixelSize;
            uniform float screenW;                                      //NEW
            uniform float screenH;                                      //NEW
            uniform float tileCount;                                    //NEW
            uniform float4 viewPortion;
            uniform float4 tile;
            uniform float rawSlope;
            uniform float rawPitch;

            //NOTE: Not used for now in Unity, since the camera should be oriented for determining what it's focusing on.
            // uniform float focus;                                        //NEW

            uniform int subpixelCellCount;                              //NEW

            // Defines the arrangement of the RGB subpixels on the display, measured in viewport coordinates on the display that are normalized by the display's screenW and screenH.
            // For example, a value of approximately 0.0002170139 is about one-third of a full pixel (0.333333) over on a Looking Glass Portrait that has a screenW of 1536px.
            uniform StructuredBuffer<SubpixelCell> subpixelCells;       //NEW
            uniform int filterMode;                                     //NEW
            uniform int cellPatternType;                                //NEW

            // This is used as a boolean to turn ON view dimming when set to 1.
            // The view dimming feature is turned OFF when this is set to 0.
            uniform int filterEdge;                                     //NEW
            uniform float filterEnd;                                    //NEW
            uniform float filterSize;                                   //NEW

            uniform float gaussianSigma;                                //NEW
            uniform float edgeThreshold;                                //NEW

            //EXTRA NOT found in LKG Bridge:
            uniform float4 aspect;
            uniform float verticalOffset; // just a dumb fix for macos in 2019.3

            uniform float antiAliasingStrength;

            float2 rotate(float2 uv, float angle) {
                // float angle = atan2(-rawSlope, 1);
                float cosAngle = cos(angle);
                float sinAngle = sin(angle);

                return float2(
                    uv.x * cosAngle - uv.y * sinAngle,
                    uv.x * sinAngle + uv.y * cosAngle
                );
            }

            int getCellForPixel(float2 screenUV) {
                int xPos = int(screenUV.x * screenW);
                int yPos = int(screenUV.y * screenH);
                int cell;

                if (cellPatternType == 0) {
                    cell = 0;
                } else if (cellPatternType == 1) {
                    // Checkerboard pattern AB
                    //                      BA
                    if ((yPos % 2 == 0 && xPos % 2 == 0) || (yPos % 2 != 0 && xPos % 2 != 0)) {
                        cell = 0;
                    } else {
                        cell = 1;
                    }
                } else if (cellPatternType == 2) {
                    cell = xPos % 2;
                } else if (cellPatternType == 3) {
                    int offset = (xPos % 2) * 2;
                    cell = ((yPos + offset) % 4);
                } else if (cellPatternType == 4) {
                    cell = yPos % 2;
                }

                return cell;
            }

            float getPixelShift(float val, float subp, int axis, int cell) {
                if (axis == 0) {
                    if (subp == 0.0) return val + (subpixelCells[cell].ROffsetX);
                    if (subp == 1.0) return val + (subpixelCells[cell].GOffsetX);
                    if (subp == 2.0) return val + (subpixelCells[cell].BOffsetX);
                } else if (axis == 1) {
                    if (subp == 0.0) return val + (subpixelCells[cell].ROffsetY);
                    if (subp == 1.0) return val + (subpixelCells[cell].GOffsetY);
                    if (subp == 2.0) return val + (subpixelCells[cell].BOffsetY);
                }
                return val;
            }

            // calculates the view for each subpixel.
            float3 getSubpixelViews(float2 screenUV) {
                float3 views = 0;

                if (subpixelCellCount <= 0) {
                    // calculate x contribution for each cell
                    views[0] = screenUV.x + subpixelSize * 0;
                    views[1] = screenUV.x + subpixelSize * 1;
                    views[2] = screenUV.x + subpixelSize * 2;

                    // calculate y contribution for each cell
                    views[0] += screenUV.y * slope;
                    views[1] += screenUV.y * slope;
                    views[2] += screenUV.y * slope;
                } else {
                    // get the cell type for this screen space pixel
                    int cell = getCellForPixel(screenUV);

                    // calculate x contribution for each cell
                    views[0]  = getPixelShift(screenUV.x, 0, 0, cell);
                    views[1]  = getPixelShift(screenUV.x, 1, 0, cell);
                    views[2]  = getPixelShift(screenUV.x, 2, 0, cell);

                    // calculate y contribution for each cell
                    views[0] += getPixelShift(screenUV.y, 0, 1, cell) * slope;
                    views[1] += getPixelShift(screenUV.y, 1, 1, cell) * slope;
                    views[2] += getPixelShift(screenUV.y, 2, 1, cell) * slope;
                }

                views *= pitch;
                views -= center;
                views = 1 - frac(views);

                views = clamp(views, 0.00001, 0.999999);
                return views;
            }

            // this returns the texture coordinate corresponding to the tile index and the tile uv
            float2 getQuiltCoordinates(float2 tileUV, int viewIndex) {
                int totalTiles = max(1, tileCount-  1);
                float view = clamp(viewIndex, 0, totalTiles);
                // on some platforms this is required to fix some precision issue???
                float tx = tile.x - 0.00001; // just an incredibly dumb bugfix
                float tileXIndex = fmod(view, tx);
                float tileYIndex = floor(view / tx);

                float quiltCoordU = ((tileXIndex + tileUV.x) / tx) * viewPortion.x;
                float quiltCoordV = ((tileYIndex + tileUV.y) / tile.y) * viewPortion.y;

                float2 quiltUV = float2(quiltCoordU, quiltCoordV);

                // Flip the Y coordinate
                // quiltUV.y = 1.0 - quiltUV.y;

                return quiltUV;
            }

            // the next 4 functions all take in a pixel coordinate and a set of views for each subpixel.
            // then return the color for that pixel on the screen.

            // this is the simplest sampling mode where we just cast the viewIndex to int and take the color from that tile.
            float4 getViewsColors(float2 tileUV, float3 views) {
                float4 color = float4(0, 0, 0, 1);

                for(int channel = 0; channel < 3; channel++) {
                    int viewIndex = int(views[channel] * tileCount);

                    float viewDir = views[channel] * 2.0 - 1.0;

                    //NOTE: See uniform NOTE above on the "focus" uniform
                    float2 focusedUV = tileUV;
                    // focusedUV.x += viewDir * focus;

                    float2 quiltUV = getQuiltCoordinates(focusedUV, viewIndex);
#ifdef LKG_BILINEAR
                    color[channel] = tex2Dlod(_MainTex, float4(quiltUV, 0, 0))[channel];
#else
                    color[channel] = tex2D(_MainTex, quiltUV)[channel];
#endif
                }

                return color;
            }

            // this sampleing mode treats the view as the left most sample and samples one view to the right and lerps the colors
            float4 oldViewFiltering(float2 tileUV, float3 views) {
                float3 viewIndicies = views * tileCount;
                float viewSpaceTileSize = 1.0 / tileCount;

                // the idea here is to sample the closest two views and lerp between them
                float3 leftViews = views;
                float3 rightViews = leftViews + viewSpaceTileSize;

                float4 leftColor = getViewsColors(tileUV, leftViews);
                float4 rightColor = getViewsColors(tileUV, rightViews);

                float3 leftRightLerp = viewIndicies - floor(viewIndicies);

                return float4(
                    lerp(leftColor.x, rightColor.x, leftRightLerp.x),
                    lerp(leftColor.y, rightColor.y, leftRightLerp.y),
                    lerp(leftColor.z, rightColor.z, leftRightLerp.z),
                    1.0
                );
            }

            // the idea here is we center a gaussian on the ideal view and then
            // weight each sample based on its location on the gaussian
            // the gaussian is only an estimation it is possible to measure
            // the actual distribution of views, Alvin has some data about this
            // I just selected a sigma value I thought looked good
            float4 gaussianViewFiltering(float2 tileUV, float3 views) {
                float3 viewIndicies = views * tileCount;
                float viewSpaceTileSize = 1.0 / tileCount;

                // this is just sampling a center view and the left and right view
                float3 centerViews = views;
                float3 leftViews = centerViews - viewSpaceTileSize;
                float3 rightViews = centerViews + viewSpaceTileSize;

                float4 centerColor = getViewsColors(tileUV, centerViews);
                float4 leftColor   = getViewsColors(tileUV, leftViews);
                float4 rightColor  = getViewsColors(tileUV, rightViews);

                // Calculate the effective discrete view directions based on the tileCount
                float3 centerSnappedViews = floor(centerViews * tileCount) / tileCount;
                float3 leftSnappedViews = floor(leftViews * tileCount) / tileCount;
                float3 rightSnappedViews = floor(rightViews * tileCount) / tileCount;

                // Gaussian weighting
                float sigma = gaussianSigma;
                float multiplier = 2.0 * sigma * sigma;

                float3 centerDiff = views - centerSnappedViews;
                float3 leftDiff = views - leftSnappedViews;
                float3 rightDiff = views - rightSnappedViews;

                float3 centerWeight = exp(-centerDiff * centerDiff / multiplier);
                float3 leftWeight = exp(-leftDiff * leftDiff / multiplier);
                float3 rightWeight = exp(-rightDiff * rightDiff / multiplier);

                // Normalize the weights so they sum to 1 for each channel
                float3 totalWeight = centerWeight + leftWeight + rightWeight;
                centerWeight /= totalWeight;
                leftWeight /= totalWeight;
                rightWeight /= totalWeight;

                // Weighted averaging based on Gaussian weighting for each channel
                float4 outputColor = float4(
                    centerColor.r * centerWeight.x + leftColor.r * leftWeight.x + rightColor.r * rightWeight.x,
                    centerColor.g * centerWeight.y + leftColor.g * leftWeight.y + rightColor.g * rightWeight.y,
                    centerColor.b * centerWeight.z + leftColor.b * leftWeight.z + rightColor.b * rightWeight.z,
                    1.0
                );

                return outputColor;
            }

            float3 computeGaussianWeight(float3 targetViews, float3 sampledViews) {
                float sigma = gaussianSigma;  // Adjust as needed
                float multiplier = 2.0 * sigma * sigma;
                float3 diff = targetViews - sampledViews;
                float3 weight = exp(-diff * diff / multiplier);
                return weight;
            }

            // this currently does the same thing as the optimized gaussianViewFiltering
            // function but it does it for an arbitrary amount of views instead of just 3
            // this could also be updated to use real weights that we measure.
            float4 nrisViewFiltering(float2 tileUV, float3 views, int n) {
                float3 viewIndicies = views * tileCount;
                float viewSpaceTileSize = 1.0 / tileCount;

                float4 outputColor = 0;

                for (int i = -n; i <= n; i++) {
                    float offset = float(i) * viewSpaceTileSize;
                    float3 offsetViews = views + offset;

                    float4 sampleColor = getViewsColors(tileUV, offsetViews);

                    // Calculate the effective discrete view directions based on the tileCount
                    float3 snappedViews = floor(offsetViews * tileCount) / tileCount;
                    float3 weight = computeGaussianWeight(views, snappedViews);

                    // Accumulate color
                    outputColor.rgb += sampleColor.rgb * weight;
                }

                // Normalize the color
                float3 totalWeight = 0;
                for (int i = -n; i <= n; i++) {
                    float offset = float(i) * viewSpaceTileSize;
                    float3 offsetViews = views + offset;

                    float4 sampleColor = getViewsColors(tileUV, offsetViews);
                    float3 snappedViews = floor(offsetViews * tileCount) / tileCount;
                    float3 weight = computeGaussianWeight(views, snappedViews);

                    // Accumulate color and total weight
                    outputColor.rgb += sampleColor.rgb * weight;
                    totalWeight += weight;
                }

                // Normalize the color
                outputColor.rgb /= totalWeight;
                outputColor.a = 1.0;

                return outputColor;
            }

            float3 oldviewDimming(float3 views, float4 color) {
                // black: 0.0 -> 0.06,
                // gradient to color: 0.06 -> 0.15,
                // color: 0.15 -> 0.85,
                // gradient to black: 0.85 -> 0.94,
                // black: 0.94 -> 1.0
                return min(-0.7932489 + 14.0647 * views - 14.0647 * views * views, 1.0);
            }

            float3 viewDimming(float3 views, float4 color) {
                // Fade boundaries
                float fadeStart1 = filterEnd;
                float fadeEnd1 = filterEnd + filterSize;
                float fullColorEnd = 1.0 - (filterEnd + filterSize);
                float fadeEnd2 = 1.0 - filterEnd;

                float3 dimValues;

                // Initial dimming for lower edge and its gradient fade-in
                float3 lowerDim = smoothstep(0.0, fadeStart1, views);
                float3 fadeDim1 = smoothstep(fadeStart1, fadeEnd1, views);
                dimValues = lerp(0, lowerDim, fadeDim1);

                // Final dimming for upper edge and its gradient fade-out
                float3 upperDim = smoothstep(1.0, fadeEnd2, views);
                float3 fadeDim2 = smoothstep(fullColorEnd, fadeEnd2, views);
                dimValues = lerp(dimValues, upperDim, fadeDim2);

                // Full-color region and its blend
                float3 fullColorDim = smoothstep(fadeEnd1, fullColorEnd, views);
                dimValues = lerp(dimValues, 1, fullColorDim);

                return dimValues;
            }

            float calculateEdgeFade(float2 tileUV) {
                float fade = min(smoothstep(0.0, edgeThreshold, tileUV.x),
                                smoothstep(0.0, edgeThreshold, 1.0 - tileUV.x));
                fade *= min(smoothstep(0.0, edgeThreshold, tileUV.y),
                            smoothstep(0.0, edgeThreshold, 1.0 - tileUV.y));
                return fade;
            }

            // --- --- ---

            struct VertexInput {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VertexOutput {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0; //The unmodified UV0 channel.
                float2 transformedUV : TEXCOORD1; //The UV0 channel that has been transformed by the texture transform matrix (in the vertex shader).
            };

            uniform float4x4 textureTransform;

            VertexOutput vert(VertexInput v) {
                VertexOutput o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.transformedUV = mul(textureTransform, float4(v.uv, 1, 1)).xy;
                return o;
            }

            fixed4 frag(VertexOutput i) : SV_Target {
                float2 screenUV = i.uv; //Position on the display exactly
                float2 tileUV = i.transformedUV; //Position on the quilt tile (contains aspect ratio fixes, translation, zoom, cropping)

                // get the views for each subpixel
                float3 views = getSubpixelViews(screenUV);
                float4 outputColor = 0;

                // get the color for those views based on the filter mode
                #if ANTI_ALIASING
                    float aspect = screenW / screenH;
                    float angle = atan2(-rawSlope, 1);
                    float vpSize = float2(1 / rawPitch, 1 / rawPitch) * 0.5;
                    for (int y = 0; y <= 1; y++) {
                        for (int x = 0; x <= 1; x++) {
                            float2 blur = float2(x, y) - 0.5;
                            blur *= vpSize;
                            blur = rotate(blur, angle);
                            blur *= float2(1, aspect);
                            blur *= antiAliasingStrength;

                            if (filterMode == 0 || tileCount <= 1) {
                                outputColor += getViewsColors(tileUV + blur, views);
                            } else if (filterMode == 1) {
                                outputColor += oldViewFiltering(tileUV + blur, views);
                            } else if (filterMode == 2) {
                                outputColor += gaussianViewFiltering(tileUV + blur, views);
                            } else if (filterMode == 3) {
                                outputColor += nrisViewFiltering(tileUV + blur, views, 10);
                            }
                        }
                    }
                    outputColor *= 0.25;
                #else
                    if (filterMode == 0 || tileCount <= 1) {
                        outputColor = getViewsColors(tileUV, views);
                    } else if (filterMode == 1) {
                        outputColor = oldViewFiltering(tileUV, views);
                    } else if (filterMode == 2) {
                        outputColor = gaussianViewFiltering(tileUV, views);
                    } else if (filterMode == 3) {
                        outputColor = nrisViewFiltering(tileUV, views, 10);
                    }
                #endif

                // dim the edges of view space to fade for displays without privacy filter
                if (filterEdge == 1) {
                    outputColor.xyz *= viewDimming(views, outputColor);
                }

                // fade to black near the edges
                float fade = calculateEdgeFade(tileUV);
                float4 color = lerp(float4(0, 0, 0, 1), outputColor, fade);
                return color;
            }
            ENDHLSL
        }
    }
}
