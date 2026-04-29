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
    public interface ISceneInfo
    {
        float GetScaleToUWRLD_UGBD();
        Pose GetGameboardPose();
        Camera GetEyeCamera();
        uint GetSupportedPlayerCount();
        bool IsActiveAndEnabled();
    }

    public static class TiltFiveSingletonHelper
    {
        public static ISceneInfo GetISceneInfo()
        {
            TryGetISceneInfo(out var singleplayerSingleton);
            return singleplayerSingleton;
        }

        public static bool TryGetISceneInfo(out ISceneInfo sceneInfo)
        {
            if (TiltFiveManager2.IsInstantiated)
            {
                sceneInfo = TiltFiveManager2.Instance;
                return true;
            }

            if (TiltFiveManager.IsInstantiated)
            {
                sceneInfo = TiltFiveManager.Instance;
                return true;
            }

            // The TiltFiveManager2 or TiltFiveManager won't appear to be instantiated
            // until their Awake() functions are called. If we're calling from the editor
            // outside of play mode, just scan the scene.
            // Presumably this is being called from a menu script, gizmo, etc.
            if (!Application.isPlaying)
            {
                var tiltFiveManager2 = GameObject.FindObjectOfType<TiltFiveManager2>();
                if (tiltFiveManager2 != null)
                {
                    sceneInfo = tiltFiveManager2;
                    return true;
                }

                var tiltFiveManager = GameObject.FindObjectOfType<TiltFiveManager>();
                if (tiltFiveManager != null)
                {
                    sceneInfo = tiltFiveManager;
                    return true;
                }
            }

            sceneInfo = null;
            return false;
        }
    }
}
