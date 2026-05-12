// Inspired by https://github.com/needle-mirror/com.unity.recorder/blob/master/Editor/Sources/RecorderSettings.cs
using System;
using UnityEngine;

namespace LookingGlass {
    /// <summary>
    /// Determines the type of capture to be recorded or screenshotted.
    /// </summary>
    [Serializable]
    public enum QuiltCaptureMode {
        /// <summary>
        /// Records every frame between the moment recording is started and when it is stopped (either using the UI or through API methods).
        /// </summary>
        Manual,

        /// <summary>
        /// <para>Records all frames within an interval of frames according to the specified start and end frame values.</para>
        /// <para>See also:
        /// <list type="bullet">
        /// <item><seealso cref="QuiltCapture.StartFrame"/></item>
        /// <item><seealso cref="QuiltCapture.EndFrame"/></item>
        /// </list>
        /// </para>
        /// </summary>
        FrameInterval,

        /// <summary>
        /// <para>Records all frames within a time interval according to the specified start and end times.</para>
        /// <para>See also:
        /// <list type="bullet">
        /// <item><seealso cref="QuiltCapture.StartTime"/></item>
        /// <item><seealso cref="QuiltCapture.EndTime"/></item>
        /// </list>
        /// </para>
        /// </summary>
        TimeInterval,

        /// <summary>
        /// Record all frames within an interval of a depth video
        /// </summary>
        ClipLength,

        /// <summary>
        /// <para>Records a single-frame as a screenshot quilt texture.</para>
        /// <para>This is the one capture mode that does NOT produce a video. A texture is generated instead when using this mode.</para>
        /// </summary>
        [InspectorName("Single Frame (Screenshot)")]
        SingleFrame
    }
}
