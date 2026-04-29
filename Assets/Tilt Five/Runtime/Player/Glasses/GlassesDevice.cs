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

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using TiltFive.Logging;


namespace TiltFive
{
    public struct GlassesState : IInputStateTypeInfo
    {
        public FourCC format => new FourCC('T', '5');

        [InputControl(name = "devicePosition", alias = "Position", noisy = true, layout = "Vector3")]
        [InputControl(name = "centerEyePosition", synthetic = true, useStateFrom = "devicePosition")]
        public Vector3 devicePosition;

        [InputControl(name = "LeftEyePosition", noisy = true, layout = "Vector3")]
        public Vector3 LeftEyePosition;

        [InputControl(name = "RightEyePosition", noisy = true, layout = "Vector3")]
        public Vector3 RightEyePosition;

        [InputControl(name = "Position/Raw", noisy = true, layout = "Vector3")]
        public Vector3 RawDevicePosition;

        [InputControl(name = "LeftEyePosition/Raw", noisy = true, layout = "Vector3")]
        public Vector3 RawLeftEyePosition;

        [InputControl(name = "RightEyePosition/Raw", noisy = true, layout = "Vector3")]
        public Vector3 RawRightEyePosition;

        // This is a neat trick - if we want multiple InputControls to be driven by the same value,
        // we can stack them like this and mark the duplicates as synthetic controls.
        [InputControl(name = "deviceRotation", alias = "Rotation", noisy = true, layout = "Quaternion")]
        [InputControl(name = "centerEyeRotation", synthetic = true, useStateFrom = "deviceRotation", noisy = true)]
        [InputControl(name = "leftEyeRotation", synthetic = true, useStateFrom = "deviceRotation", noisy = true)]
        [InputControl(name = "rightEyeRotation", synthetic = true, useStateFrom = "deviceRotation", noisy = true)]
        public Quaternion deviceRotation;

        [InputControl(name = "Rotation/Raw", noisy = true, layout = "Quaternion")]
        public Quaternion RawRotation;
    }

    [InputControlLayout(stateType = typeof(GlassesState), displayName = "Tilt Five Glasses", updateBeforeRender = true)]
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class GlassesDevice : UnityEngine.InputSystem.XR.XRHMD
    {
        #region Public Fields

        public PlayerIndex PlayerIndex { get; internal set; }
        public WandDevice LeftWand = null;
        public WandDevice RightWand = null;

        #endregion


        #region Controls

        public new Vector3Control devicePosition { get; private set; }
        public new Vector3Control leftEyePosition { get; private set; }
        public new Vector3Control rightEyePosition { get; private set; }
        public new Vector3Control centerEyePosition { get; private set; }

        public Vector3Control RawPosition { get; private set; }
        public Vector3Control RawLeftEyePosition { get; private set; }
        public Vector3Control RawRightEyePosition { get; private set; }

        public new QuaternionControl deviceRotation { get; private set; }
        public new QuaternionControl leftEyeRotation { get; private set; }
        public new QuaternionControl rightEyeRotation { get; private set; }
        public new QuaternionControl centerEyeRotation { get; private set; }
        public QuaternionControl RawRotation { get; private set; }

        public new ButtonControl isTracked { get; private set; }

        public new IntegerControl trackingState { get; private set; }

        #endregion


        #region Overrides

        protected override void FinishSetup()
        {
            base.FinishSetup();

            devicePosition = GetChildControl<Vector3Control>("Position");
            centerEyePosition = GetChildControl<Vector3Control>("centerEyePosition");
            leftEyePosition = GetChildControl<Vector3Control>("LeftEyePosition");
            rightEyePosition = GetChildControl<Vector3Control>("RightEyePosition");

            RawPosition = GetChildControl<Vector3Control>("Position/Raw");
            RawLeftEyePosition = GetChildControl<Vector3Control>("LeftEyePosition/Raw");
            RawRightEyePosition = GetChildControl<Vector3Control>("RightEyePosition/Raw");

            deviceRotation = GetChildControl<QuaternionControl>("Rotation");
            leftEyeRotation = GetChildControl<QuaternionControl>("leftEyeRotation");
            rightEyeRotation = GetChildControl<QuaternionControl>("rightEyeRotation");
            centerEyeRotation = GetChildControl<QuaternionControl>("centerEyeRotation");
            RawRotation = GetChildControl<QuaternionControl>("Rotation/Raw");

            isTracked = GetChildControl<ButtonControl>("isTracked");
            trackingState = GetChildControl<IntegerControl>("trackingState");
        }

        #endregion

        static GlassesDevice()
        {
            InputSystem.RegisterLayout<GlassesDevice>(matches: new InputDeviceMatcher()
                .WithInterface("GlassesDevice"));
        }
    }

}

#endif