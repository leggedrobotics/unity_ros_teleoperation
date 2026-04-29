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

namespace TiltFive
{
    [System.Serializable]
    public abstract class TrackableSettings
    {
        /*
         * We may add common functionality for settings classes here in the future.
         *
         * For now, this class just places a constraint on the types of objects
         * that can be used with TrackableCore<T>.
        */

        public bool RejectUntrackedPositionData = true;
        public TrackingFailureMode FailureMode = TrackingFailureMode.FreezePosition;

        public enum TrackingFailureMode
        {
            FreezePosition = 0,
            FreezePositionAndRotation = 1,
            SnapToDefault = 2
        }
    }
}