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

    /// <summary>
    /// ScaleSettings contains the scale data used to translate between Unity units and the user's physical space.
    /// </summary>
    [System.Serializable]
    public class ScaleSettings
    {
        /// <summary>
        /// The real-world unit to be compared against when using <see cref="contentScaleRatio">.
        /// </summary>
        public LengthUnit contentScaleUnit = LengthUnit.Centimeters;

        /// <summary>
        /// The scaling ratio relates physical distances to world-space units.
        /// </summary>
        /// <remarks>
        /// This value defines how distance units in world-space should appear to players in the real world.
        /// This is useful for initially defining a game world's sense of scale, as well as for CAD applications.
        /// Use this value alongside <see cref="contentScaleUnit"> to choose the desired physical units (e.g. centimeters, inches, etc).
        /// Afterwards, use <see cref="gameBoardScale"> for cinematic or gameplay purposes.
        /// </remarks>
        /// <example>
        /// Suppose that we want to display a bedroom scene that is 10 units across in world space.
        /// Also suppose that a person standing in this virtual bedroom would measure that distance to be 4 meters.
        /// In this case, we want 10 in-game units to represent 4 meters. Dividing 10 by 4 gives us 2.5,
        /// so contentScaleRatio should be set to 2.5 for the player to perceive the virtual space at a 1:1 ratio with reality, assuming <see cref="contentScaleUnit"> is set to meters.
        /// If the room was now too large for a comfortable experience using the game board, we could change <see cref="contentScaleUnit"> to inches,
        /// and the room would appear to be 25 inches across, now entirely visible within the borders of the game board.
        /// </example>
        public float contentScaleRatio = 5f;

        /// <summary>
        /// The content scale, in terms of meters per world space unit.
        /// </summary>
        /// <remarks>
        /// This value can be useful for gravity scaling. Simply divide Earth gravity (9.81m/s^2) by the product of this value and the game board scale.
        /// </remarks>
        /// <example>
        /// Suppose the content scale is set to 1:10cm. Using Unity's default gravity setting,
        /// the player would see an object in freefall appear to accelerate 1/10th as fast as expected, which could feel
        /// unnatural if the game is meant to be perceived as taking place on the table in front of them in the player's space.
        /// To fix this, a script with a reference to the Tilt Five Manager could call the following on Awake():
        /// <code>Physics.gravity = new Vector3(0f, 9.81f / tiltFiveManager.glassesSettings.physicalMetersPerWorldSpaceUnit, 0f);</code>
        /// </example>
        public float physicalMetersPerWorldSpaceUnit => new Length(contentScaleRatio, contentScaleUnit).ToMeters;

        public float worldSpaceUnitsPerPhysicalMeter => 1 / Mathf.Max(physicalMetersPerWorldSpaceUnit, float.Epsilon);  // No dividing by zero.

        public float oneUnitLengthInMeters => (new Length(1, contentScaleUnit)).ToMeters;

        /// <summary>
        /// Whether to enable the old, incorrect, behavior whereby the inverse of the gameboard
        /// GameObject's scale was used to calculate scaleToUWRLD_UGBD.
        /// </summary>
        /// <remarks>
        /// Setting this to true reverts to the legacy method for scaling the gameboard scale.
        /// </remarks>
        public bool legacyInvertGameboardScale = false;

        public const float MIN_CONTENT_SCALE_RATIO = 0.0000001f;

        public float GetScaleToUWRLD_UGBD(float gameboardScale)
        {
            float scaleToUWRLD_UGBD = 0.0f;

            if (legacyInvertGameboardScale) {
                float scaleToUGBD_UWRLD = physicalMetersPerWorldSpaceUnit * gameboardScale;
                scaleToUWRLD_UGBD = scaleToUGBD_UWRLD > 0
                    ? 1f / scaleToUGBD_UWRLD
                    : 1f / float.Epsilon;
            } else {
                scaleToUWRLD_UGBD = gameboardScale / physicalMetersPerWorldSpaceUnit;
            }

            return scaleToUWRLD_UGBD;
        }

#if UNITY_EDITOR
        public bool copyPlayerOneScaleRatio = true;
        public bool copyPlayerOneScaleUnit = true;
#endif

        internal ScaleSettings Copy()
        {
            return (ScaleSettings)MemberwiseClone();
        }
    }
}
