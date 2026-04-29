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

namespace TiltFive
{
    /// <summary>
    /// <see cref="GraphicsSettings"/> encapsulates configuration data related to the project's
    /// graphics settings, such as target framerate, vsync, etc.
    /// </summary>
    [System.Serializable]
    public class GraphicsSettings
    {
        /// <summary>
        /// Determines whether the Tilt Five SDK should optimize the project's framerate and vsync for sending frames to the glasses
        /// at a rate that the glasses can handle, reducing visible temporal artifacts like animations stuttering.
        /// </summary>
        /// <remarks>
        /// This setting ensures that a stable stream of frames reaches the glasses and helps to prevent animations from stuttering.
        /// It does this by limiting the maximum frames per second to the glasses' preferred framerate, preventing stalls / dropped frames.
        /// It also disables VSync so the frames don't need to wait for the user's monitor to refresh before being sent to the glasses.
        /// </remarks>
        public bool matchGlassesFramerate = true;

        internal int applicationVSyncCount = 0;
        internal int applicationTargetFramerate = 60;

        internal const int VSYNC_DISABLED = 0;
        internal const int PREFERRED_GLASSES_FRAMERATE = 60;    // TODO: Turn this into a NDK query?
    }
}
