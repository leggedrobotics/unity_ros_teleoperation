/*
 * Copyright (C) 2020-2023 Tilt Five, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using UnityEngine;
#if TILT_FIVE_SRP
using UnityEngine.Rendering;
#endif
using System;

namespace TiltFive
{
    class SplitStereoTextures
    {
        #region Public Fields

        /// <summary>
        /// The left eye rendertexture
        /// </summary>
        /// <remarks>This is used to send frame data to the glasses.</remarks>
        public RenderTexture LeftTexture_GLS;
        /// <summary>
        /// The right eye rendertexture
        /// </summary>
        /// <remarks>This is used to send frame data to the glasses.</remarks>
        public RenderTexture RightTexture_GLS;


        /// <summary>
        /// The native pointer to the left eye rendertexture
        /// </summary>
        /// <remarks>This is used to pass the left eye texture to unmanaged code</remarks>
        private IntPtr LeftTexHandle { get; set; }
        /// <summary>
        /// The native pointer to the right eye rendertexture
        /// </summary>
        /// <remarks>This is used to pass the left eye texture to unmanaged code</remarks>
        private IntPtr RightTexHandle { get; set; }


        /// <summary>
        /// The rendertexture used to display onscreen previews for the left or right eye camera.
        /// </summary>
        public RenderTexture MonoPreviewTex;
        /// <summary>
        /// The rendertexture used to display onscreen previews for the left and right eye cameras in stereo.
        /// </summary>
        public RenderTexture StereoPreviewTex;

        #endregion Public Fields


        #region Public Functions

        /// <summary>
        /// Creates and configures the stereo rendertextures
        /// </summary>
        /// <param name="renderFormat_UGLS"></param>
        public void Initialize()
        {
            LeftTexture_GLS = new RenderTexture(
                DisplaySettings.monoWidth,
                DisplaySettings.height,
                DisplaySettings.depthBuffer,
                DisplaySettings.nativeTextureFormat);
            LeftTexture_GLS.name = "Left Eye Output RenderTexture";
            RightTexture_GLS = new RenderTexture(
                DisplaySettings.monoWidth,
                DisplaySettings.height,
                DisplaySettings.depthBuffer,
                DisplaySettings.nativeTextureFormat);
            RightTexture_GLS.name = "Right Eye Output RenderTexture";

            MonoPreviewTex = new RenderTexture(
                DisplaySettings.monoWidth,
                DisplaySettings.height,
                DisplaySettings.depthBuffer,
                RenderTextureFormat.Default);
            MonoPreviewTex.name = "Mono Preview RenderTexture";
            StereoPreviewTex = new RenderTexture(
                DisplaySettings.stereoWidth,
                DisplaySettings.height,
                DisplaySettings.depthBuffer,
                RenderTextureFormat.Default);
            StereoPreviewTex.name = "Stereo Preview RenderTexture";
        }

        /// <summary>
        /// Determines whether the left/right texture handles are still valid, and resets them if needed
        /// </summary>
        /// <remarks>This should be executed in OnPreRender(), otherwise IsCreated() always returns true</remarks>
        public void ValidateNativeTexturePointers()
        {
            /* Render textures have a state (created or not created), and that state can be invalidated.
            There are a few ways this can happen, including the game switching to/from fullscreen,
            or the system screensaver being displayed. When this happens, the native texture pointers we
            pass to the native plugin are also invalidated, and garbage data gets displayed by the glasses.

            To fix this, we can check whether the state has been invalidated and reacquire a valid native texture pointer.
            RenderTexture's IsCreated() function reports false if the render texture has been invalidated.
            We must detect this change in OnPreRender(), because IsCreated() reports true within Update().
            If we detect that the render textures have been invalidated, we null out the cached pointers and reacquire.
            */

            // Check whether the render textures' states have been invalidated,
            // and reset the cached texture handles if so.
            if (!LeftTexture_GLS.IsCreated() || !RightTexture_GLS.IsCreated())
            {
                LeftTexHandle = System.IntPtr.Zero;
                RightTexHandle = System.IntPtr.Zero;
            }
        }

        /// <summary>
        /// Acquires the native output textures upon startup or invalidaiton.
        /// </summary>
        /// <remarks>This should be executed after all rendering is complete, including UI and post processing.</remarks>
        public void GetNativeTexturePointers(out IntPtr leftTexHandle, out IntPtr rightTexHandle)
        {
            // If the native texture handles were reset by ValidateNativeTexturePointers(), reacquire them
            if(LeftTexHandle == IntPtr.Zero || RightTexHandle == IntPtr.Zero)
            {
                LeftTexHandle = LeftTexture_GLS.GetNativeTexturePtr();
                RightTexHandle = RightTexture_GLS.GetNativeTexturePtr();
            }

            leftTexHandle = LeftTexHandle;
            rightTexHandle = RightTexHandle;
        }

        /// <summary>
        /// Copies frame data from the HDR input textures to the onscreen preview textures.
        /// </summary>
        /// <param name="glassesMirrorMode"></param>
        public void SubmitPreviewTextures(GlassesMirrorMode glassesMirrorMode)
        {
            var previewTex = glassesMirrorMode == GlassesMirrorMode.Stereoscopic ? StereoPreviewTex : MonoPreviewTex;

            switch (glassesMirrorMode)
            {
                case GlassesMirrorMode.LeftEye:
                    CopyTexture(LeftTexture_GLS, previewTex);
                    break;
                case GlassesMirrorMode.RightEye:
                    CopyTexture(RightTexture_GLS, previewTex);
                    break;
                case GlassesMirrorMode.Stereoscopic:
                    // Copy the two eyes' target textures to a double-wide texture, then display it onscreen.
                    CopyTexture(LeftTexture_GLS, previewTex);
                    CopyTexture(RightTexture_GLS, previewTex, LeftTexture_GLS.width);
                    break;
            }
        }
#if TILT_FIVE_SRP
        public void SubmitPreviewTexturesSRP(
            GlassesMirrorMode glassesMirrorMode,
            Camera headPoseCamera,
            CommandBuffer commandBuffer,
            Vector2 frameScale)
        {
            var previewTex = glassesMirrorMode == GlassesMirrorMode.Stereoscopic ? StereoPreviewTex : MonoPreviewTex;

            switch (glassesMirrorMode)
            {
                case GlassesMirrorMode.LeftEye:
                    CopyTextureToPreviewTextureSRP(commandBuffer, LeftTexture_GLS, previewTex);
                    break;
                case GlassesMirrorMode.RightEye:
                    CopyTextureToPreviewTextureSRP(commandBuffer, RightTexture_GLS, previewTex);
                    break;
                case GlassesMirrorMode.Stereoscopic:
                    // Copy the two eyes' target textures to a double-wide texture, then display it onscreen.
                    CopyTextureToPreviewTextureSRP(commandBuffer, LeftTexture_GLS, previewTex);
                    CopyTextureToPreviewTextureSRP(commandBuffer, RightTexture_GLS, previewTex, LeftTexture_GLS.width);
                    break;
            }

            // We're going to composite the left/right eye cameras into a temporary render texture.
            var tempRTIdentifier = Shader.PropertyToID("TiltFiveCanvas");
            int tempRTWidth = (int)(previewTex.width * frameScale.x);
            int tempRTHeight = (int)(previewTex.height * frameScale.y);

            RenderTargetIdentifier currentRenderTarget = BuiltinRenderTextureType.CurrentActive;

            // As of Unity 2020.3.24f1, it appears that this will result in an error if MSAA is
            // enabled with D3D11, which will look something like:
            //
            //   D3D11: Failed to create RenderTexture (1432 x 768 fmt 27 aa 2), error 0x80070057
            //
            // This does not appear to prevent the onscreen preview from working, so it's unclear
            // whether this is really a problem.  Disabling MSAA will get the error to go away, but
            // is not recommended.  This code is only executed if using one of the eye camera mirror
            // modes, and we recommend setting the glasses mirror mode to None in release builds.
            //
            // See: https://answers.unity.com/questions/1592194/getting-d3d11-failed-to-create-render-texture-erro.html
            commandBuffer.GetTemporaryRT(tempRTIdentifier,
                tempRTWidth,
                tempRTHeight,
                previewTex.descriptor.depthBufferBits,
                previewTex.filterMode,
                previewTex.format,
                RenderTextureReadWrite.Default,
                previewTex.antiAliasing,
                true // needs random access for the CopyTexture below to work with nonzero offset
                );
            commandBuffer.SetRenderTarget(tempRTIdentifier);
            commandBuffer.ClearRenderTarget(true, true, Color.black);
            commandBuffer.CopyTexture(previewTex, 0, 0, 0, 0,
                previewTex.width, previewTex.height,
                tempRTIdentifier, 0, 0,
                (tempRTWidth - previewTex.width) / 2, (tempRTHeight - previewTex.height) / 2);
            commandBuffer.Blit(tempRTIdentifier, headPoseCamera.targetTexture, Vector2.one, Vector2.zero);
            commandBuffer.ReleaseTemporaryRT(tempRTIdentifier);
            commandBuffer.SetRenderTarget(currentRenderTarget);
        }
#endif  // TILT_FIVE_SRP

        #endregion Public Functions


        #region Private Functions

        void CopyTexture(RenderTexture sourceTex, RenderTexture destinationTex, int xOffset = 0)
        {
            Graphics.CopyTexture(
                        sourceTex,
                        0,      // srcElement
                        0,      // srcMip
                        0, 0,   // src offset
                        sourceTex.width, sourceTex.height,  // src size
                        destinationTex,
                        0,      // dstElement
                        0,      // dstMip
                        xOffset, 0);  // dst offset
        }
#if TILT_FIVE_SRP
        void CopyTextureToPreviewTextureSRP(CommandBuffer cmd, RenderTexture sourceTex, RenderTexture destinationTex, int xOffset = 0)
        {
            cmd.CopyTexture(
                sourceTex,
                0,
                0,
                0, 0,
                sourceTex.width, sourceTex.height,
                destinationTex,
                0, 0, xOffset, 0);
        }
#endif

        #endregion Private Functions
    }
}
