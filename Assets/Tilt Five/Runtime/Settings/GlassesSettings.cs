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
    /// <summary>
    /// GlassesSettings encapsulates all configuration data used by the Glasses'
    /// tracking runtime to compute the Head Pose and apply it to the Camera.
    /// </summary>
    [System.Serializable]
    public class GlassesSettings : TrackableSettings
    {
        #region Constants

        /// <summary>
        /// The maximum number of supported glasses
        /// </summary>
        internal const uint MAX_SUPPORTED_GLASSES_COUNT = 4;     // TODO: Expand this to 4

        public const float MIN_FOV = 35f;
        public const float MAX_FOV = 64f;
        public const float DEFAULT_FOV = 48f;

        // A default value will be returned by the client API if a custom IPD hasn't been set for
        // the glasses starting in version 1.1.0+, which means this constant won't be needed.  It
        // is kept for compatibility with older releases.
        public const float DEFAULT_IPD_UGBD = 0.059f;

        // Enforce a near clip plane that keeps objects from getting too close to the user's head.
        // TODO: Determine the threshold for discomfort (plus a small amount of margin) via usability testing.
        public const float MIN_NEAR_CLIP_DISTANCE_IN_METERS = 0.1f;

        public static readonly string DEFAULT_FRIENDLY_NAME = "Tilt Five Glasses";

        #endregion


#if UNITY_EDITOR
        /// <summary>
        /// Editor only configuration to disable/enable stereo-rendering.
        /// </summary>
        public bool tiltFiveXR = true;

        public bool copyPlayerOneCameraTemplate = true;
        public bool copyPlayerOneObjectTemplate = true;
#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
        public bool copyPlayerOnePlayerTemplate = true;
#endif
        public bool copyCloneCameraTemplateChildren = true;
        public bool copyPlayerOneCullingMask = true;
        public bool copyPlayerOneFOVToggle = true;
        public bool copyPlayerOneFOV = true;
        public bool copyPlayerOneNearClipPlane = true;
        public bool copyPlayerOneFarClipPlane = true;
#endif
        /// <summary>
        /// The main camera used for rendering the Scene when the glasses are unavailable, and the gameobject used for the glasses pose.
        /// </summary>
        [System.Obsolete("headPoseCamera is deprecated, and its functionality has been split between " +
            "GlassesSettings.cameraTemplate (for instantiating eye cameras) " +
            "and SpectatorSettings.spectatorCamera (for the onscreen preview).")]
        public Camera headPoseCamera;

        // TODO: make this the default name and get rid of headPoseCamera when we update API in
        // 2.0.0.  We want to hold off until then because changing this will clear every scene's
        // TiltFiveManager reference to the preview/headpose camera, requiring a manual fix of the
        // broken reference in every scene.
        /// <summary>
        /// The camera used as a template for creating the eye cameras at runtime.
        /// </summary>
        /// <remarks>Alias for the obsolete field <see cref="headPoseCamera"/>.</remarks>
        public Camera cameraTemplate {
            #pragma warning disable 618 // this is for compatibility; disable obsolete warning
            get { return headPoseCamera; }
            set { headPoseCamera = value; }
            #pragma warning restore 618
        }

        /// <summary>
        /// The object used as a template for creating the base Game Object when a specific playerIndex connects.
        /// </summary>
        public GameObject objectTemplate;

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
        /// <summary>
        /// The object used as a template for creating a Game Object indepedent from the Glasses object when a pair of glasses connects.
        /// Unity maintains an internal list of players when new InputUsers connect. This object will be created
        /// each time a player connects, we use a Player Index mapping to correlate Tilt Five Players with Unity Players
        /// </summary>
        public GameObject playerTemplate;
#endif

        /// <summary>
        /// Whether or not the camera template's child gameobjects should be cloned during
        /// eye camera instantiation at runtime.
        /// </summary>
        public bool cloneCameraTemplateChildren = false;

        /// <summary>
        /// The culling mask to be used by the eye cameras for this pair of glasses.
        /// </summary>
        public LayerMask cullingMask = ~0;

        /// <summary>
        /// The near clip plane in physical space (meters), to adjust for content scale and gameboard size
        /// </summary>
        public float nearClipPlane = MIN_NEAR_CLIP_DISTANCE_IN_METERS;

        /// <summary>
        /// The far clip plane in physical space (meters), to adjust for content scale and gameboard size
        /// </summary>
        public float farClipPlane = 100f;

        public bool overrideFOV = false;
        public float customFOV = DEFAULT_FOV;
        public float fieldOfView => overrideFOV
            ? Mathf.Clamp(customFOV, MIN_FOV, MAX_FOV)
            : DEFAULT_FOV;

        public GlassesMirrorMode glassesMirrorMode = GlassesMirrorMode.LeftEye;

        public bool usePreviewPose = true;
        public Transform previewPose;

        public string friendlyName = DEFAULT_FRIENDLY_NAME;

        internal GlassesSettings Copy()
        {
            return (GlassesSettings)MemberwiseClone();
        }
    }
}
