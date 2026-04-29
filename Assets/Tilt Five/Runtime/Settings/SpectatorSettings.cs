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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TiltFive
{
    public enum GlassesMirrorMode
    {
        None,
        LeftEye,
        RightEye,
        Stereoscopic
    }

    [System.Serializable]
    public class SpectatorSettings
    {
        /// <summary>
        /// The camera used for rendering the onscreen preview
        /// </summary>
        public Camera spectatorCamera;

        /// <summary>
        /// The spectator camera will display content on screen depending on the mirroring mode.
        /// For example, if <see cref="GlassesMirrorMode.LeftEye"/> is set, the left eye camera's
        /// perspective will be displayed.
        /// </summary>
        /// <remarks>If no mirroring mode is selected (i.e. <see cref="GlassesMirrorMode.None"/>),
        /// the spectator camera will render at the screen resolution / aspect ratio using a
        /// smoothed preview pose. This is generally recommended for release builds to give a better
        /// onscreen viewing experience, and it is significantly better for livestreaming or
        /// recording.</remarks>
        public GlassesMirrorMode glassesMirrorMode = GlassesMirrorMode.None;

        /// <summary>
        /// The player that will have their perspective mirrored on screen.
        /// </summary>
        public PlayerIndex spectatedPlayer = PlayerIndex.One;

        /// <summary>
        /// The culling mask to be used by the <see cref="spectatorCamera">Spectator Camera</see>.
        /// </summary>
        /// <remarks>The spectator camera's culling mask will be dynamically modified by the plugin (it will cull everything)
        /// if <see cref="glassesMirrorMode"/> isn't set to <see cref="GlassesMirrorMode.None"/>.
        /// As a result, if developers need to change the spectator camera's culling mask at runtime, their changes may be lost.
        /// To get around this, set this value instead, and it will be used to overwrite the spectator camera culling mask
        /// once the Tilt Five classes are finshed with working with it.</remarks>
        public LayerMask cullingMask = ~0;

        /// <summary>
        /// The field of view to be used by the <see cref="spectatorCamera">Spectator Camera</see>.
        /// </summary>
        /// <remarks>The spectator camera's field of view will be dynamically modified by the plugin
        /// (it will conform to the eye cameras' field of view) if <see cref="glassesMirrorMode"/> isn't set to <see cref="GlassesMirrorMode.None"/>.
        /// As a result, if developers need to change the spectator camera's FOV at runtime, their changes may be lost.
        /// To get around this, set this value instead, and it will be used to overwrite the spectator camera FOV
        /// once the Tilt Five classes are finshed with working with it.</remarks>
        public float fieldOfView = 60f;

        /// <summary>
        /// The near clip plane to be used by the <see cref="spectatorCamera">Spectator Camera</see>.
        /// </summary>
        /// <remarks>The spectator camera's near clip plane will be dynamically modified by the plugin
        /// (it will conform to the eye cameras' near clip plane) if <see cref="glassesMirrorMode"/> isn't set to <see cref="GlassesMirrorMode.None"/>.
        /// As a result, if developers need to change the spectator camera's near clip plane at runtime, their changes may be lost.
        /// To get around this, set this value instead, and it will be used to overwrite the spectator camera near clip plane
        /// once the Tilt Five classes are finshed with working with it.</remarks>
        public float nearClipPlane = 0.3f;
        /// <summary>
        /// The far clip plane to be used by the <see cref="spectatorCamera">Spectator Camera</see>.
        /// </summary>
        /// <remarks>The spectator camera's far clip plane will be dynamically modified by the plugin
        /// (it will conform to the eye cameras' far clip plane) if <see cref="glassesMirrorMode"/> isn't set to <see cref="GlassesMirrorMode.None"/>.
        /// As a result, if developers need to change the spectator camera's far clip plane at runtime, their changes may be lost.
        /// To get around this, set this value instead, and it will be used to overwrite the spectator camera far clip plane
        /// once the Tilt Five classes are finshed with working with it.</remarks>
        public float farClipPlane = 1000f;

        /// <summary>
        /// The viewport rect used by the <see cref="spectatorCamera">Spectator Camera</see>.
        /// </summary>
        /// <remarks>The spectator camera's viewport rect will be dynamically modifed by the plugin
        /// (it will conform to the eye cameras' aspect ratio) if <see cref="glassesMirrorMode"/> isn't set to <see cref="GlassesMirrorMode.None"/>.
        /// As a result, if developers need to change the spectator camera's viewport rect at runtime, their changes may be lost.
        /// To get around this, set this value instead, and it will be used to overwrite the spectator camera viewport rect
        /// once the Tilt Five classes are finshed with working with it.</remarks>
        public Rect rect = new Rect(0, 0, 1, 1);

        /// <summary>
        /// The target texture used by the <see cref="spectatorCamera">Spectator Camera</see>.
        /// </summary>
        /// <remarks>The spectator camera's target texture will be dynamically modifed by the plugin
        /// (it will be blitted to by the eye camera(s)) if <see cref="glassesMirrorMode"/> isn't set to <see cref="GlassesMirrorMode.None"/>.
        /// As a result, if developers need to change the spectator camera's target texture at runtime, their changes may be lost.
        /// To get around this, set this value instead, and it will be used to overwrite the spectator camera target texture
        /// once the Tilt Five classes are finshed with working with it.</remarks>
        public RenderTexture targetTexture = null;

        internal void ResetSpectatorCamera()
        {
            spectatorCamera.cullingMask = cullingMask;
            spectatorCamera.fieldOfView = fieldOfView;
            spectatorCamera.nearClipPlane = nearClipPlane;
            spectatorCamera.farClipPlane = farClipPlane;
            spectatorCamera.rect = rect;
            spectatorCamera.targetTexture = targetTexture;
        }
    }
}
