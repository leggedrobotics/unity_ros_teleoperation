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

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

using TiltFive;
using TiltFive.Logging;

namespace TiltFive
{
    /// <summary>
    /// The Camera Frame API and runtime.
    /// </summary>
    public class CameraImage : Singleton<CameraImage>
    {

        /// <summary>
        /// The glasses core runtimes.
        /// </summary>
        private Dictionary<GlassesHandle, CameraImageCore> cameraImageCores = new Dictionary<GlassesHandle, CameraImageCore>();

        #region Public Functions

        internal static void Initialize(GlassesHandle glassesHandle)
        {
            Instance.cameraImageCores[glassesHandle] = new CameraImageCore(glassesHandle);
        }

        internal static void RemoveCore(GlassesHandle glassesHandle){
            if(!Instance.cameraImageCores.TryGetValue(glassesHandle, out var cameraImageCore)) { return; }
            cameraImageCore.Dispose();
            Instance.cameraImageCores.Remove(glassesHandle);
        }

        /// <summary>
        /// Attempt to request a new filled buffer from the camera image stream. If an empty buffer has been submitted,
        /// the oldest available image will be placed in to camImage. If no image is available, camImage will not be modified.
        /// If a new image is available, camImage will wrap the previously provided buffer, now containing the frame data.
        /// </summary>
        /// <returns>true if a filled buffer was available and camImage has been set, false otherwise.
        /// </returns>
        public static bool TryGetFilledCameraImageBuffer(PlayerIndex playerIndex, ref T5_CamImage camImage)
        {
            if(!Player.TryGetGlassesHandle(playerIndex, out var glassesHandle))
            {
                return false;
            }

            return TryGetFilledCameraImageBuffer(glassesHandle, ref camImage);
        }

        internal static bool TryGetFilledCameraImageBuffer(GlassesHandle glassesHandle, ref T5_CamImage camImage)
        {
            if(!Glasses.IsConnected(glassesHandle) || !Instance.cameraImageCores.TryGetValue(glassesHandle, out var cameraImageCore))
            {
                return false;
            }

            return cameraImageCore.TryGetFilledCameraImageBuffer(ref camImage);
        }

        /// <summary>
        /// Submit a camera image buffer to be used by the camera image stream. As images become available
        /// the buffers will be filled and returned on subsequent calls to TryGetFilledCameraImageBuffer(). It's recommended
        /// to begin with at least 3 buffers when submitting buffers to the stream, and after an image is finished
        /// being processed, resubmitting that buffer back to the camera image stream.
        /// </summary>
        /// <returns>true if buffer was accepted, false otherwise.
        /// </returns>

        public static bool TrySubmitEmptyCameraImageBuffer(PlayerIndex playerIndex, IntPtr imageBuffer, UInt32 bufferSize)
        {
            if(!Player.TryGetGlassesHandle(playerIndex, out var glassesHandle))
            {
                return false;
            }

            return TrySubmitEmptyCameraImageBuffer(glassesHandle, imageBuffer, bufferSize);
        }

        internal static bool TrySubmitEmptyCameraImageBuffer(GlassesHandle glassesHandle, IntPtr imageBuffer, UInt32 bufferSize)
        {
            if(!Glasses.IsConnected(glassesHandle) || !Instance.cameraImageCores.TryGetValue(glassesHandle, out var cameraImageCore))
            {
                return false;
            }

            return cameraImageCore.TrySubmitEmptyCameraImageBuffer(imageBuffer, bufferSize);
        }

        /// <summary>
        /// Specify an image buffer to be released from the Tilt Five Service. If true is returned,
        /// the provided buffer is no longer in use by the service, and is available to be freed.
        /// Canceled buffers should not be utilized outside of freeing or resubmitting to the service,
        /// their data is not guaranteed to be in a specific state.
        /// </summary>
        /// <returns>true if buffer has been canceled, false otherwise.
        /// </returns>

        public static bool TryCancelCameraImageBuffer(PlayerIndex playerIndex, byte[] imageBuffer)
        {
            if(!Player.TryGetGlassesHandle(playerIndex, out var glassesHandle))
            {
                return false;
            }

            return TryCancelCameraImageBuffer(glassesHandle, imageBuffer);
        }

        internal static bool TryCancelCameraImageBuffer(GlassesHandle glassesHandle, byte[] imageBuffer)
        {
            if(!Glasses.IsConnected(glassesHandle) || !Instance.cameraImageCores.TryGetValue(glassesHandle, out var cameraImageCore))
            {
                return false;
            }

            return cameraImageCore.TryCancelCameraImageBuffer(imageBuffer);
        }

        /// <summary>
        /// Attempt to configure the Camera Stream
        /// </summary>
        /// <returns>true if stream has been conffigured, false otherwise.
        /// </returns>

        public static bool TryConfigureCameraImageStream(PlayerIndex playerIndex, T5_CameraStreamConfig config)
        {
            if(!Player.TryGetGlassesHandle(playerIndex, out var glassesHandle))
            {
                return false;
            }

            return TryConfigureCameraImageStream(glassesHandle, config);
        }

        internal static bool TryConfigureCameraImageStream(GlassesHandle glassesHandle, T5_CameraStreamConfig config)
        {
            if(!Glasses.IsConnected(glassesHandle) || !Instance.cameraImageCores.TryGetValue(glassesHandle, out var cameraImageCore))
            {
                return false;
            }

            return cameraImageCore.TryConfigureCameraImageStream(config);
        }

        #endregion Public Functions


        /// <summary>
        /// Internal Camera Image core.
        /// </summary>
        private class CameraImageCore : IDisposable
        {
            public GlassesHandle glassesHandle;

            public CameraImageCore(GlassesHandle glassesId)
            {
                this.glassesHandle = glassesId;
            }

            public virtual void Dispose(){
                Log.Info($"CameraImageCore for {glassesHandle} disconnected");
                return;
            }

            public bool TryConfigureCameraImageStream(T5_CameraStreamConfig config)
            {
                int result = NativePlugin.T5_RESULT_UNKNOWN_ERROR;
                try
                {
                    result = NativePlugin.ConfigureCameraStream(glassesHandle, config);
                }
                catch (System.Exception e)
                {
                    Log.Error($"Error enabling Camera Stream: {e.Message}");
                }
                return result == NativePlugin.T5_RESULT_SUCCESS;
            }

            public bool TryGetFilledCameraImageBuffer(ref T5_CamImage camImage)
            {
                int result = NativePlugin.T5_RESULT_UNKNOWN_ERROR;

                try
                {
                    result = NativePlugin.GetFilledCamImageBuffer(glassesHandle, ref camImage);
                }
                catch (System.Exception e)
                {
                    Log.Error($"Error getting Camera Image: {e.Message}");
                }
                return result == NativePlugin.T5_RESULT_SUCCESS;
            }

            public bool TrySubmitEmptyCameraImageBuffer(IntPtr imageBuffer, UInt32 bufferSize)
            {
                int result = NativePlugin.T5_RESULT_UNKNOWN_ERROR;
                try
                {
                    result = NativePlugin.SubmitEmptyCamImageBuffer(glassesHandle, imageBuffer, bufferSize);
                }
                catch (System.Exception e)
                {
                    Log.Error($"Error submitting Camera Image Buffer: {e.Message}");
                }

                return result == NativePlugin.T5_RESULT_SUCCESS;
            }

            public bool TryCancelCameraImageBuffer(byte[] imageBuffer)
            {
                int result = NativePlugin.T5_RESULT_UNKNOWN_ERROR;
                try
                {
                    GCHandle handle = GCHandle.Alloc(imageBuffer, GCHandleType.Pinned);
                    var ImageBufferHandle = handle.AddrOfPinnedObject();
                    result = NativePlugin.CancelCamImageBuffer(glassesHandle, ImageBufferHandle);
                    handle.Free();
                }
                catch (System.Exception e)
                {
                    Log.Error($"Error clearing Camera Image Buffers: {e.Message}");
                }

                return result == NativePlugin.T5_RESULT_SUCCESS;
            }
        }
    }
}
