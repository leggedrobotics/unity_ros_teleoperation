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
    public enum LengthUnit
    {
        Kilometers,
        [Tooltip("A smoot is the height of Oliver R. Smoot, equivalent to 5ft 7in or 1.702m.")]
        Smoots,
        Meters,
        [Tooltip("An attoparsec is one quintillionth of a parsec, or slightly more than 3cm.")]
        Attoparsecs,
        Centimeters,
        [Tooltip("A mega beard-second is one million beard-seconds (the distance a typical beard grows in one second, about 5nm), or about 5mm.")]
        MegaBeardSeconds,
        Millimeters,
        Miles,
        [Tooltip("A furlong is 220 yards.")]
        Furlongs,
        Yards,
        Feet,
        Inches
    }

    public struct Length
    {

        #region Public Fields

        /// <summary>
        /// The Length, converted to inches.
        /// </summary>        
        public float ToInches => ConvertTo(LengthUnit.Inches);

        /// <summary>
        /// The Length, converted to feet.
        /// </summary>        
        public float ToFeet => ConvertTo(LengthUnit.Feet);

        /// <summary>
        /// The Length, converted to yards.
        /// </summary>
        public float ToYards => ConvertTo(LengthUnit.Yards);

        /// <summary>
        /// The Length, converted to furlongs.
        /// </summary>
        public float ToFurlongs => ConvertTo(LengthUnit.Furlongs);

        /// <summary>
        /// The Length, converted to miles.
        /// </summary>
        public float ToMiles => ConvertTo(LengthUnit.Miles);

        /// <summary>
        /// The Length, converted to millimeters.
        /// </summary>
        public float ToMillimeters => ConvertTo(LengthUnit.Millimeters);

        /// <summary>
        /// The Length, converted to mega-beard-seconds.
        /// </summary>
        public float ToMegaBeardSeconds => ConvertTo(LengthUnit.MegaBeardSeconds);

        /// <summary>
        /// The Length, converted to centimeters.
        /// </summary>
        public float ToCentimeters => ConvertTo(LengthUnit.Centimeters);

        /// <summary>
        /// The Length, converted to attoparsecs.
        /// </summary>
        public float ToAttoparsecs => ConvertTo(LengthUnit.Attoparsecs);

        /// <summary>
        /// The Length, converted to meters.
        /// </summary>
        public float ToMeters => ConvertTo(LengthUnit.Meters);

        /// <summary>
        /// The Length, converted to smoots.
        /// </summary>        
        public float ToSmoots => ConvertTo(LengthUnit.Smoots);

        /// <summary>
        /// The Length, converted to kilometers.
        /// </summary>
        public float ToKilometers => ConvertTo(LengthUnit.Kilometers);

        #endregion Public Fields


        #region Private Fields

        private float _meters;
        private const float METERS_TO_INCHES = 39.3701f,
                            METERS_TO_FEET = 3.28084f,
                            METERS_TO_YARDS = 1.0936f,
                            METERS_TO_FURLONGS = 0.004971f,
                            METERS_TO_MILLIMETERS = 1000f,
                            METERS_TO_MEGABEARDSECONDS = 200f,
                            METERS_TO_CENTIMETERS = 100f,
                            METERS_TO_ATTOPARSECS = 32.4078f,
                            METERS_TO_SMOOTS = 0.587613f,
                            METERS_TO_KILOMETERS = 0.001f,
                            MILES_TO_METERS = 1609.34f;

        #endregion Public Fields


        #region Public Functions

        public Length(float value, LengthUnit unit)
        {
            _meters = ConvertToMeters(value, unit);
        }

        /// <summary>
        /// Converts the length to a value in the provided units.
        /// </summary>
        /// <param name="unit">The provided length units.</param>
        /// <returns>The length, in the provided units.</returns>
        public float ConvertTo(LengthUnit unit)
        {
            return ConvertMetersTo(_meters, unit);
        }

        /// <summary>
        /// Converts the length to a double value in the provided units.
        /// </summary>
        /// <param name="unit">The provided length units.</param>
        /// <returns>The length, in the provided units.</returns>
        /// <remarks>
        /// Performs the conversion calculation using double arithmetic to reduce floating-point rounding errors.
        /// This may be less performant, so Drink Responsibly™.
        ///</remarks>
        public double PreciselyConvertTo(LengthUnit unit)
        {
            return PreciselyConvertMetersTo(_meters, unit);
        }

        /// <summary>
        /// Converts the provided length value to a value in meters.
        /// </summary>
        /// <param name="length">The provided length.</param>
        /// <param name="unit">The provided length units.</param>
        /// <returns>The length, in meters.</returns>
        public static float ConvertToMeters(float length, LengthUnit unit)
        {
            float meters = 0f;

            switch (unit)
            {
                case LengthUnit.Kilometers:
                    meters = length / METERS_TO_KILOMETERS;
                    break;
                case LengthUnit.Smoots:
                    meters = length / METERS_TO_SMOOTS;
                    break;
                case LengthUnit.Attoparsecs:
                    meters = length / METERS_TO_ATTOPARSECS;
                    break;
                case LengthUnit.Centimeters:
                    meters = length / METERS_TO_CENTIMETERS;
                    break;
                case LengthUnit.MegaBeardSeconds:
                    meters = length / METERS_TO_MEGABEARDSECONDS;
                    break;
                case LengthUnit.Millimeters:
                    meters = length / METERS_TO_MILLIMETERS;
                    break;
                case LengthUnit.Miles:
                    meters = length * MILES_TO_METERS;
                    break;
                case LengthUnit.Furlongs:
                    meters = length / METERS_TO_FURLONGS;
                    break;
                case LengthUnit.Yards:
                    meters = length / METERS_TO_YARDS;
                    break;
                case LengthUnit.Feet:
                    meters = length / METERS_TO_FEET;
                    break;
                case LengthUnit.Inches:
                    meters = length / METERS_TO_INCHES;
                    break;
                default:    // Meters
                    meters = length;
                    break;
            }

            return meters;
        }

        /// <summary>
        /// Converts the provided meter value to a value in the provided units.
        /// </summary>
        /// <param name="meters">The provided length in meters.</param>
        /// <param name="unit">The provided length units.</param>
        /// <returns>The length, in the provided units.</returns>
        public static float ConvertMetersTo(float meters, LengthUnit unit)
        {
            switch (unit)
            {
                case LengthUnit.Kilometers:
                    return meters * METERS_TO_KILOMETERS;
                case LengthUnit.Smoots:
                    return meters * METERS_TO_SMOOTS;
                case LengthUnit.Attoparsecs:
                    return meters * METERS_TO_ATTOPARSECS;
                case LengthUnit.Centimeters:
                    return meters * METERS_TO_CENTIMETERS;
                case LengthUnit.MegaBeardSeconds:
                    return meters * METERS_TO_MEGABEARDSECONDS;
                case LengthUnit.Millimeters:
                    return meters * METERS_TO_MILLIMETERS;
                case LengthUnit.Miles:
                    return meters / MILES_TO_METERS;
                case LengthUnit.Furlongs:
                    return meters * METERS_TO_FURLONGS;
                case LengthUnit.Yards:
                    return meters * METERS_TO_YARDS;
                case LengthUnit.Feet:
                    return meters * METERS_TO_FEET;
                case LengthUnit.Inches:
                    return meters * METERS_TO_INCHES;
                default:    // Meters
                    return meters;
            }
        }

        /// <summary>
        /// Converts the provided meter value to a double value in the provided units.
        /// </summary>
        /// <param name="meters">The provided length in meters.</param>
        /// <param name="unit">The provided length units.</param>
        /// <returns>The length, in the provided units.</returns>
        /// <remarks>
        /// Performs the conversion calculation using double arithmetic to reduce floating-point rounding errors.
        /// This may be less performant, so Drink Responsibly™.
        ///</remarks>
        public static double PreciselyConvertMetersTo(double meters, LengthUnit unit)
        {
            switch (unit)
            {
                case LengthUnit.Kilometers:
                    return meters * METERS_TO_KILOMETERS;
                case LengthUnit.Smoots:
                    return meters * METERS_TO_SMOOTS;
                case LengthUnit.Attoparsecs:
                    return meters * METERS_TO_ATTOPARSECS;
                case LengthUnit.Centimeters:
                    return meters * METERS_TO_CENTIMETERS;
                case LengthUnit.MegaBeardSeconds:
                    return meters * METERS_TO_MEGABEARDSECONDS;
                case LengthUnit.Millimeters:
                    return meters * METERS_TO_MILLIMETERS;
                case LengthUnit.Miles:
                    return meters / MILES_TO_METERS;
                case LengthUnit.Furlongs:
                    return meters * METERS_TO_FURLONGS;
                case LengthUnit.Yards:
                    return meters * METERS_TO_YARDS;
                case LengthUnit.Feet:
                    return meters * METERS_TO_FEET;
                case LengthUnit.Inches:
                    return meters * METERS_TO_INCHES;
                default:    // Meters
                    return meters;
            }
        }

        /// <summary>
        /// Multiplies a Length by some scalar value.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Length operator *(Length a, float scalar)
            => new Length(a.ToMeters * scalar, LengthUnit.Meters);

        /// <summary>
        /// Adds two Length values together.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Length operator +(Length a, Length b)
            => new Length(a.ToMeters + b.ToMeters, LengthUnit.Meters);

        /// <summary>
        /// Subtracts one Length value from another Length value.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Length operator -(Length a, Length b)
            => new Length(a.ToMeters - b.ToMeters, LengthUnit.Meters);

        #endregion Public Functions
    }
}
