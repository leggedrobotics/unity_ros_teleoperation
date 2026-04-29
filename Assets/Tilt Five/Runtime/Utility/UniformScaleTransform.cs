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
    /// Enforces uniform scaling by setting the x, y, and z scale components of the associated transform to be equal.
    /// </summary>
    [RequireComponent(typeof(Transform))]
    [ExecuteInEditMode]
    public class UniformScaleTransform : MonoBehaviour
    {
        #region Public Fields

        /// <summary>
        /// The size of the object as a single float value, rather than a scale vector.
        /// </summary>
        public float localScale
        {
            get => transform.localScale.x;
            set
            {
                base.transform.localScale = Vector3.one * value;
                _previousScale = base.transform.localScale;
            }
        }

        /// <summary>
        /// The position vector for the associated transform.
        /// </summary>        
        public Vector3 position
        {
            get => transform.position;
            set => transform.position = value;
        }

        /// <summary>
        /// The rotation vector for the associated transform.
        /// </summary>
        public Quaternion rotation
        {
            get => transform.rotation;
            set => base.transform.rotation = value;
        }

        #endregion Public Fields


        #region Private Fields

        private Vector3 _previousScale;

        #endregion Private Fields


        #region Private Functions

        /// <summary>
        /// Synchronizes the component values of the game object's local scale vector (e.g. [1,2,3] becomes [3,3,3]).
        /// </summary>
        /// <remarks>
        /// The vector component with the most extreme deviation from the previous uniform scale vector will be selected.
        /// If the previous scale was [2,2,2] and the current scale is [5, 15, 50] then the result will be [50, 50, 50].
        /// This also applies for negative values: [5, -20, 10] would result in [-20,-20,-20].
        /// </remarks>
        protected void UnifyScale()
        {
            // Compare the current scale against the previous scale.                        
            if (transform.localScale == _previousScale)
            {
                return;
            }

            // Get the component that changed the most, and set the scale to that value.
            var deltaScale = transform.localScale - _previousScale;
            var largestPositiveChange = Mathf.Max(deltaScale.x, deltaScale.y, deltaScale.z);
            var largestNegativeChange = Mathf.Min(deltaScale.x, deltaScale.y, deltaScale.z);
            var largestAbsoluteChange = Mathf.Abs(largestPositiveChange) > Mathf.Abs(largestNegativeChange)
                                            ? largestPositiveChange
                                            : largestNegativeChange;

            transform.localScale = _previousScale + Vector3.one * largestAbsoluteChange;
            _previousScale = transform.localScale;
        }

        #endregion Private Functions


        #region Unity Functions

        public void Awake()
        {
            // Initial pass, in case the transform's scale is already non-uniform before UniformScaleTransform is initialized.
            _previousScale = Vector3.one;
            UnifyScale();

            _previousScale = transform.localScale;
        }

        // Update is called once per frame
        void Update()
        {
            UnifyScale();
        }

        #endregion Unity Functions
    }
}
