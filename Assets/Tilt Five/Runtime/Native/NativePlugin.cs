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
using System.Runtime.InteropServices;
using UnityEngine;

namespace TiltFive
{
    public class NativePlugin
    {

#if (UNITY_IPHONE || UNITY_WEBGL) && !UNITY_EDITOR
        public const string PLUGIN_LIBRARY = @"__Internal";
#else
        public const string PLUGIN_LIBRARY = @"TiltFiveUnity";
#endif

        internal const int T5_RESULT_SUCCESS = 0;
        internal const int T5_RESULT_UNKNOWN_ERROR = 1;

        #region Native Functions

        // Init
        [DllImport(PLUGIN_LIBRARY)]
        internal static extern int SetApplicationInfo(
            T5_StringUTF8 appName,
            T5_StringUTF8 appId,
            T5_StringUTF8 appVersion);

        [DllImport(PLUGIN_LIBRARY)]
        internal static extern int SetPlatformContext(
            IntPtr platformContext);

        [DllImport(PLUGIN_LIBRARY)]
        [return: MarshalAs(UnmanagedType.I4)]
        internal static extern ServiceCompatibility GetServiceCompatibility();

        [DllImport(PLUGIN_LIBRARY)]
        internal static extern int IsTiltFiveUIRequestingAttention(ref T5_Bool attentionRequested);

        // Glasses Acquisition
        [DllImport(PLUGIN_LIBRARY)]
        internal static extern int RefreshGlassesAvailable();

        // Set Maximum Desired Glasses.  Count starts at 0.
        //
        // Note that using this to reduce the count will result in glasses being released back to
        // the set of available glasses, which can invalidate handles currently in use.  Generally,
        // this should only be called once on load to indicate how many glasses your application
        // wants.
        [DllImport(PLUGIN_LIBRARY)]
        internal static extern void SetMaxDesiredGlasses(byte maxCount);

        // Glasses Availability
        [DllImport(PLUGIN_LIBRARY)]
        internal static extern int GetGlassesHandles(
            ref byte handleCount,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)] UInt64[] glassesHandle);

        // Glasses Friendly Name
        [DllImport(PLUGIN_LIBRARY)]
        internal static extern int GetGlassesFriendlyName(UInt64 glassesHandle, ref T5_StringUTF8 glassesFriendlyName);

        // Head Pose
        [DllImport(PLUGIN_LIBRARY)]
        internal static extern int GetGlassesPose(
            UInt64 glassesHandle,
            ref T5_GlassesPose glassesPose,
            [MarshalAs(UnmanagedType.I4)] T5_GlassesPoseUsage glassesPoseUsage);

        [DllImport(PLUGIN_LIBRARY)]
        internal static extern int ConfigureCameraStream(UInt64 glassesHandle, T5_CameraStreamConfig cameraConfig);

        [DllImport(PLUGIN_LIBRARY)]
        internal static extern int GetFilledCamImageBuffer(UInt64 glassesHandle, ref T5_CamImage camImageBuffer);

        [DllImport(PLUGIN_LIBRARY)]
        internal static extern int SubmitEmptyCamImageBuffer(UInt64 glassesHandle, IntPtr camImageBuffer, UInt32 bufferSize);

        [DllImport(PLUGIN_LIBRARY)]
        internal static extern int CancelCamImageBuffer(UInt64 glassesHandle, IntPtr buffer);

#if TILT_FIVE_ENABLE_PROJECTOR_EXTRINSICS_ADJUSTMENTS
        [DllImport(PLUGIN_LIBRARY)]
        internal static extern int SetProjectorExtrinsicsAdjustment(UInt64 glassesHandle,
            [MarshalAs(UnmanagedType.LPArray, SizeConst = 14)] float[] args);
#endif

        // Gameboard dimensions
        [DllImport(PLUGIN_LIBRARY)]
        internal static extern int GetGameboardDimensions(
            [MarshalAs(UnmanagedType.I4)] GameboardType gameboardType,
            ref T5_GameboardSize playableSpaceInMeters);

        // Wand Availability
        [DllImport(PLUGIN_LIBRARY)]
        internal static extern int GetWandAvailability(
            UInt64 glassesHandle,
            ref T5_Bool wandAvailable,
            [MarshalAs(UnmanagedType.I4)] ControllerIndex wandTarget);

        // Scan for Wands
        [DllImport(PLUGIN_LIBRARY)]
        internal static extern int ScanForWands();

        // Wand Controls State
        [DllImport(PLUGIN_LIBRARY)]
        internal static extern int GetControllerState(
            UInt64 glassesHandle,
            [MarshalAs(UnmanagedType.I4)] ControllerIndex controllerIndex,
            ref T5_ControllerState controllerState);

        [DllImport(PLUGIN_LIBRARY)]
        internal static extern int SendImpulse(UInt64 glassesHandle,
            [MarshalAs(UnmanagedType.I4)] ControllerIndex controllerIndex, float amplitude, ushort duration);

        // Submit Render Textures
        [DllImport(PLUGIN_LIBRARY)]
        internal static extern int QueueStereoImages(UInt64 glassesHandle, T5_FrameInfo frameInfo);

        [DllImport(PLUGIN_LIBRARY)]
        internal static extern IntPtr GetSendFrameCallback();

        [DllImport(PLUGIN_LIBRARY)]
        internal static extern int GetMaxDisplayDimensions(
            [MarshalAs(UnmanagedType.LPArray, SizeConst = 2)] int[] displayDimensions);

        [DllImport(PLUGIN_LIBRARY)]
        internal static extern int GetGlassesIPD(UInt64 glassesHandle, ref float glassesIPD);

        [DllImport(PLUGIN_LIBRARY)]
        internal static extern void UnloadWorkaround();

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport ("__Internal")]
        internal static extern void RegisterPlugin();
#endif

        #endregion Native Functions
    }

    /// <summary>
    /// Represents a boolean value.
    /// </summary>
    /// <remarks>This struct exists primarily to guarantee a common memory layout
    /// when marshaling bool values to/from the native plugin.</remarks>
    internal struct T5_Bool
    {
        private readonly byte booleanByte;

        public T5_Bool(bool boolean)
        {
            booleanByte = Convert.ToByte(boolean);
        }

        public static implicit operator bool(T5_Bool t5_boolean)
            => Convert.ToBoolean(t5_boolean.booleanByte);
        public static implicit operator T5_Bool(bool boolean) => new T5_Bool(boolean);
    }

    /// <summary>
    /// Represents a three dimensional position.
    /// </summary>
    /// <remarks>This struct exists primarily to guarantee a common memory layout
    /// when marshaling Vector3 values to/from the native plugin.</remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal struct T5_Position
    {
        public float X, Y, Z;

        public T5_Position(Vector3 position)
        {
            X = position.x;
            Y = position.y;
            Z = position.z;
        }

        public static implicit operator Vector3(T5_Position t5_position)
            => new Vector3(t5_position.X, t5_position.Y, t5_position.Z);
        public static implicit operator T5_Position(Vector3 position) => new T5_Position(position);
    }

    /// <summary>
    /// Represents a quaternion rotation.
    /// </summary>
    /// <remarks>This struct exists primarily to guarantee a common memory layout
    /// when marshaling quaternion values to/from the native plugin.</remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal struct T5_Rotation
    {
        public float W, X, Y, Z;

        public T5_Rotation(Quaternion rotation)
        {
            W = rotation.w;
            X = rotation.x;
            Y = rotation.y;
            Z = rotation.z;
        }

        public static implicit operator Quaternion(T5_Rotation t5_rotation)
            => new Quaternion(t5_rotation.X, t5_rotation.Y, t5_rotation.Z, t5_rotation.W);
        public static implicit operator T5_Rotation(Quaternion rotation) => new T5_Rotation(rotation);
    }

    internal struct GlassesHandle : IEquatable<GlassesHandle>
    {
        private UInt64 glassesHandle;
        public static implicit operator UInt64(GlassesHandle handle) => handle.glassesHandle;
        public static implicit operator GlassesHandle(UInt64 handle) => new GlassesHandle() { glassesHandle = handle };
        public override int GetHashCode() => glassesHandle.GetHashCode();
        public bool Equals(GlassesHandle other) => glassesHandle == other.glassesHandle;
        public override string ToString()
        {
            return glassesHandle.ToString();
        }
    }

    /// <summary>
    /// Headset pose information to be retrieved with <see cref="NativePlugin.GetGlassesPose(ref T5_GlassesPose)"/>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct T5_GlassesPose
    {
        public UInt64 TimestampNanos;

        private T5_Position posOfGLS_GBD;
        private T5_Rotation rotationToGLS_GBD;

        public GameboardType GameboardType;

        public Vector3 PosOfGLS_GBD { get => posOfGLS_GBD; set => posOfGLS_GBD = value; }
        public Quaternion RotationToGLS_GBD { get => rotationToGLS_GBD; set => rotationToGLS_GBD = value; }
    }

    /// <summary>
    /// Represents a wrapper on a buffer comtaining a camera image.
    /// </summary>
    /// <remarks>This struct exists primarily to guarantee a common memory layout
    /// when marshaling an image buffer to/from the native plugin.
    /// Note that it implements <see cref="IDisposable"/>, and that it should be wrapped
    /// in a "using" statement/block to avoid leaking memory.</remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct T5_CamImage
    {
        public UInt16 ImageBufferWidth_PIX;
        public UInt16 ImageBufferHeight_PIX;
        public UInt16 ImageBufferStride_PIX;
        public UInt32 ImageBufferSize_PIX;
        public IntPtr ImageBuffer;
        private T5_Position posOfCAM_GBD;
        private T5_Rotation rotToCAM_GBD;
        public Vector3 PosOfCAM_GBD { get => posOfCAM_GBD; set => posOfCAM_GBD = value; }
        public Quaternion RotToCAM_GBD { get => rotToCAM_GBD; set => rotToCAM_GBD = value; }

        public T5_CamImage(UInt16 bufferWidth, UInt16 bufferHeight, UInt16 bufferStride, UInt32 bufferSize)
        {
            ImageBuffer = IntPtr.Zero;
            ImageBufferWidth_PIX = bufferWidth;
            ImageBufferHeight_PIX = bufferHeight;
            ImageBufferStride_PIX = bufferStride;
            ImageBufferSize_PIX = bufferSize;
            posOfCAM_GBD = Vector3.zero;
            rotToCAM_GBD = Quaternion.identity;
        }
    }

    /// <summary>
    /// Camera Stream Configuration.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct T5_CameraStreamConfig
    {
        public byte cameraIndex;
        private T5_Bool streamEnabled;
        public bool enabled { get => streamEnabled; set => streamEnabled = value; }
    }

    /// <summary>
    /// Contains wand related information (Pose, Buttons, Trigger, Stick, Battery)
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct T5_ControllerState
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Joystick
        {
            public float X, Y;

            public static implicit operator Vector2(Joystick joystick) => new Vector2(joystick.X, joystick.Y);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Buttons
        {
            private T5_Bool t5,
                one,
                two,
                three,
                a,
                b,
                x,
                y;

            public bool T5 { get => t5; set => t5 = value; }
            public bool One { get => one; set => one = value; }
            public bool Two { get => two; set => two = value; }
            public bool Three { get => three; set => three = value; }
            public bool A { get => a; set => a = value; }
            public bool B { get => b; set => b = value; }
            public bool X { get => x; set => x = value; }
            public bool Y { get => y; set => y = value; }
            [Obsolete("Buttons.System is deprecated, please use Wandbutton.T5 instead.")]
            public bool System => T5;
            [Obsolete("Buttons.Z is deprecated, please use Wandbutton.Three instead.")]
            public bool Z => Three;
        }

        public UInt64 TimestampNanos;

        private T5_Bool analogValid;
        private T5_Bool batteryValid;
        private T5_Bool buttonsValid;
        private T5_Bool poseValid;

        public float Trigger;
        public Joystick Stick;
        public byte Battery;
        public Buttons ButtonsState;

        private T5_Rotation rotToWND_GBD;
        private T5_Position aimPos_GBD;
        private T5_Position fingertipsPos_GBD;
        private T5_Position gripPos_GBD;

        public T5_Hand Hand;

        public bool AnalogValid { get => analogValid; set => analogValid = value; }
        public bool BatteryValid { get => batteryValid; set => batteryValid = value; }
        public bool ButtonsValid { get => buttonsValid; set => buttonsValid = value; }
        public bool PoseValid { get => poseValid; set => poseValid = value; }

        public Quaternion RotToWND_GBD { get => rotToWND_GBD; set => rotToWND_GBD = value; }
        public Vector3 AimPos_GBD { get => aimPos_GBD; set => aimPos_GBD = value; }
        public Vector3 FingertipsPos_GBD { get => fingertipsPos_GBD; set => fingertipsPos_GBD = value; }
        public Vector3 GripPos_GBD { get => gripPos_GBD; set => gripPos_GBD = value; }
    }

    /// <summary>
    /// Represents the image rectangle in the normalized (z=1) image space of the virtual cameras
    /// </summary>
    /// <remarks>This struct exists primarily to guarantee a common memory layout
    /// when marshaling rectangle values to/from the native plugin.</remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal struct T5_VCI
    {
        public float StartX_VCI;
        public float StartY_VCI;
        public float Width_VCI;
        public float Height_VCI;

        public T5_VCI(Rect rect)
        {
            StartX_VCI = rect.x;
            StartY_VCI = rect.y;
            Width_VCI = rect.width;
            Height_VCI = rect.height;
        }

        public static implicit operator Rect(T5_VCI vci)
            => new Rect(vci.StartX_VCI, vci.StartY_VCI, vci.Width_VCI, vci.Height_VCI);
        public static implicit operator T5_VCI(Rect rect) => new T5_VCI(rect);
    }

    /// <summary>
    /// Render information to be used with <see cref="NativePlugin.QueueStereoImages(T5_FrameInfo)"/>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct T5_FrameInfo
    {
        public IntPtr LeftTexHandle;
        public IntPtr RightTexHandle;
        public UInt16 TexWidth_PIX;
        public UInt16 TexHeight_PIX;

        private T5_Bool isSrgb;
        private T5_Bool isUpsideDown;

        private T5_VCI vci;

        private T5_Rotation rotToLVC_GBD;
        private T5_Position posOfLVC_GBD;

        private T5_Rotation rotToRVC_GBD;
        private T5_Position posOfRVC_GBD;

        public bool IsSrgb { get => isSrgb; set => isSrgb = value; }
        public bool IsUpsideDown { get => isUpsideDown; set => isUpsideDown = value; }
        public Rect VCI { get => vci; set => vci = value; }
        public Quaternion RotToLVC_GBD { get => rotToLVC_GBD; set => rotToLVC_GBD = value; }
        public Vector3 PosOfLVC_GBD { get => posOfLVC_GBD; set => posOfLVC_GBD = value; }
        public Quaternion RotToRVC_GBD { get => rotToRVC_GBD; set => rotToRVC_GBD = value; }
        public Vector3 PosOfRVC_GBD { get => posOfRVC_GBD; set => posOfRVC_GBD = value; }
    }

    /// <summary>
    /// Represents a string value.
    /// </summary>
    /// <remarks>This struct exists primarily to guarantee a common memory layout
    /// when marshaling string values to/from the native plugin.
    /// Note that it implements <see cref="IDisposable"/>, and that it should be wrapped
    /// in a "using" statement/block to avoid leaking memory.</remarks>
    [StructLayout(LayoutKind.Explicit, Pack = 4)]
    internal struct T5_StringUTF8 : IDisposable
    {
        [FieldOffset(0)] private UInt32 maxBufferSize;
        [FieldOffset(4)] private UInt32 length;
        [FieldOffset(8)] private IntPtr pStringBytesUTF8;

        public T5_StringUTF8(string text)
        {
            pStringBytesUTF8 = IntPtr.Zero;
            length = 0;
            maxBufferSize = 16 * 1024;

            if (text != null)
            {
                // Allocate enough unmanaged memory to store the string
                byte[] textBytesUTF8 = System.Text.Encoding.UTF8.GetBytes(text);

                // If the string is too long to fit in the unmanaged buffer, truncate it.
                length = (UInt32)Math.Min(textBytesUTF8.Length, maxBufferSize);
                pStringBytesUTF8 = Marshal.AllocHGlobal((int)maxBufferSize);

                // Store the string data
                Marshal.Copy(textBytesUTF8, 0, pStringBytesUTF8, (int)length);
            }
        }

        public static implicit operator string(T5_StringUTF8 t5_StringUTF8)
            => ToString(t5_StringUTF8);

        public static implicit operator T5_StringUTF8(string text) => new T5_StringUTF8(text);

        private static string ToString(T5_StringUTF8 t5_StringUTF8)
        {
            if (t5_StringUTF8.pStringBytesUTF8 == IntPtr.Zero)
            {
                return null;
            }

            var managedBytes = new byte[t5_StringUTF8.length];
            try
            {
                Marshal.Copy(t5_StringUTF8.pStringBytesUTF8, managedBytes, 0, (int)t5_StringUTF8.length);
                return System.Text.Encoding.UTF8.GetString(managedBytes);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to copy string from unmanaged memory: {e}");
                return null;
            }
        }

        /// <summary>
        /// Safely disposes of this T5_StringUTF8 and any unmanaged memory allocated during its construction.
        /// </summary>
        public void Dispose()
        {
            // Don't forget to free that unmanaged memory we allocated.
            // Marshal.FreeHGlobal() will safely do nothing if IntPtr.Zero is passed in.
            Marshal.FreeHGlobal(pStringBytesUTF8);
            pStringBytesUTF8 = IntPtr.Zero;
        }
    }

    /// <summary>
    /// Whether the running service is compatible.
    /// </summary>
    public enum ServiceCompatibility : Int32
    {
        // <summary>
        // The running service is incompatible with this client.
        // </summary>
        Incompatible = 0,

        // <summary>
        // The running service is compatible with this client.
        // </summary>
        Compatible = 1,

        // <summary>
        // Don't know yet whether the running service is compatible with this client.
        // </summary>
        Unknown = 2,
    }

    /// <summary>
    /// The Player index (e.g. Player One, Player Two, etc)
    /// </summary>
    public enum PlayerIndex
    {
        [HideInInspector]
        None = 0,
        One = 1,
        Two = 2,
        Three = 3,
        Four = 4
    }

    /// <summary>
    /// Since wands are all physically identical (they have no "handedness"), it doesn't make sense to address them using "left" or "right".
    /// Instead we use hand dominance, and allow applications to swap the dominant and offhand wand according to the user's preference.
    /// </summary>
    public enum ControllerIndex : Int32
    {
        /// <summary>
        /// The wand held in the player's right hand.
        /// </summary>
        Right = 0,

        /// <summary>
        /// The wand held in the player's left hand.
        /// </summary>
        Left = 1,

        [Obsolete("ControllerIndex.Primary is obsolete, please update to use left/right based on user preference instead.", true)]
        Primary = -1,

        [Obsolete("ControllerIndex.Secondary is obsolete, please update to use left/right based on user preference instead.", true)]
        Secondary = -2,
    }

    /// <summary>
    /// The type of Gameboard being tracked by the glasses
    /// </summary>
    public enum GameboardType : Int32
    {
        /// <summary>
        /// No Gameboard at all.
        /// </summary>
        /// <remarks>
        /// If the glasses pose is in respect to GameboardType.GameboardType_None
        /// </remarks>
        GameboardType_None = 1,

        /// <summary>
        /// The LE Gameboard.
        /// </summary>
        GameboardType_LE = 2,

        /// <summary>
        /// The XE Gameboard, laid out flat.
        /// </summary>
        GameboardType_XE = 3,

        /// <summary>
        /// The XE Gameboard, folded upward using its kickstand.
        /// </summary>
        GameboardType_XE_Raised = 4,
    }

    /// <summary>
    /// Physical dimensions of a gameboard, in meters.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct T5_GameboardSize
    {
        /// <summary>
        /// The distance in meters from the gameboard origin to the edge of the viewable area in the positive X direction.
        /// </summary>
        public float ViewableExtentPositiveX;

        /// <summary>
        /// The distance in meters from the gameboard origin to the edge of the viewable area in the negative X direction.
        /// </summary>
        public float ViewableExtentNegativeX;

        /// <summary>
        /// The distance in meters from the gameboard origin to the edge of the viewable area in the positive Z direction.
        /// </summary>
        public float ViewableExtentPositiveZ;

        /// <summary>
        /// The distance in meters from the gameboard origin to the edge of the viewable area in the negative Z direction.
        /// </summary>
        public float ViewableExtentNegativeZ;

        /// <summary>
        /// The distance in meters above the gameboard origin that the viewable area extends in the positive Y direction.
        /// </summary>
        public float ViewableExtentPositiveY;

        public T5_GameboardSize(float viewableExtentPositiveX, float viewableExtentNegativeX,
            float viewableExtentPositiveZ, float viewableExtentNegativeZ,
            float viewableExtentPositiveY)
        {
            ViewableExtentPositiveX = viewableExtentPositiveX;
            ViewableExtentNegativeX = viewableExtentNegativeX;
            ViewableExtentPositiveZ = viewableExtentPositiveZ;
            ViewableExtentNegativeZ = viewableExtentNegativeZ;
            ViewableExtentPositiveY = viewableExtentPositiveY;
        }

        [Obsolete("This version of the T5_GameboardSize constructor is obsolete. " +
            "Please use T5_GameboardSize(float viewableExtentPositiveX, float viewableExtentNegativeX, " +
            "float viewableExtentPositiveZ, float viewableExtentNegativeZ, float viewableExtentPositiveY) instead.")]
        public T5_GameboardSize(float playableSpaceX, float playableSpaceZ, float borderWidth)
        {
            ViewableExtentPositiveX = playableSpaceX / 2f;
            ViewableExtentNegativeX = playableSpaceX / 2f;
            ViewableExtentPositiveZ = playableSpaceZ / 2f;
            ViewableExtentNegativeZ = playableSpaceZ / 2f;
            ViewableExtentPositiveY = 0f;
        }
    }

    /// <summary>
    /// Points of interest along the wand controller, such as the handle position or wand tip.
    /// </summary>
    public enum ControllerPosition : Int32
    {
        /// <summary>
        /// The center of the wand handle.
        /// </summary>
        Grip = 0,

        /// <summary>
        /// The typical resting position of the player's fingertips, near the wand joystick and trigger.
        /// </summary>
        Fingertips = 1,

        /// <summary>
        /// The tip of the wand.
        /// </summary>
        Aim = 2
    }

    /// <summary>
    /// Reported wand hand
    /// </summary>
    public enum T5_Hand : byte
    {
        /// <summary>
        /// Hand unknown
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Left hand
        /// </summary>
        Left = 1,

        /// <summary>
        /// Right hand
        /// </summary>
        Right = 2,
    }

    /// <summary>
    /// Glasses pose usage indicator
    /// </summary>
    internal enum T5_GlassesPoseUsage : Int32
    {
        /// <summary>
        /// The pose will be used to render images to be presented on the glasses.
        /// </summary>
        /// <remarks>
        /// Querying a glasses pose for this usage will return a pose prediction intended to account
        /// for the render and presentation latency. The predicted pose is prone to include errors,
        /// and the rendered images may appear very jittery if they are displayed on a device other
        /// than the glasses. When displayed via the glasses, the on-glasses image stabilization
        /// compensates for this prediction error, so the image should not appear jittery.
        /// </remarks>
        GlassesPresentation = 1,

        /// <summary>
        /// The pose will be used to render images to be presented on a device other than the
        /// glasses, such at the host system's primary display.
        /// </summary>
        /// <remarks>
        /// Querying a glasses pose for this usage will return a pose with less noise than that
        /// intended for presentation via the glasses.
        /// </remarks>
        SpectatorPresentation = 2,
    }
}
