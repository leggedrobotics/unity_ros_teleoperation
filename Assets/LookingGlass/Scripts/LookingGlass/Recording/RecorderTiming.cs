using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace LookingGlass {
    /// <summary>
    /// <para>Represents the number of frames, and video time that has elapsed so far during recording.</para>
    /// <para>See also: <seealso cref="QuiltCapture"/></para>
    /// </summary>
    [Serializable]
    public struct RecorderTiming {
        private float frameRate;
        private float startTime;
        private int frameCount;
        private float pauseTime;
        private int frameDropCount;

        public float FrameRate => frameRate;
        public float FrameTime => startTime + pauseTime + (frameCount - 0.5f) / frameRate;

        //TODO: WARNING: This reflects the number of FFmpeg frames pushed, NOT the number of Unity update frames passed.
        public int FrameCount => frameCount;

        public RecorderTiming(float frameRate) {
            Assert.IsTrue(frameRate > 0);
            this.frameRate = frameRate;
            startTime = Time.time;

            frameCount = 0;
            pauseTime = 0;
            frameDropCount = 0;
        }

        public void CatchUp(float gapToRealtime) {
            frameCount += Mathf.FloorToInt(gapToRealtime * FrameRate);
        }

        public void OnFramePushed() {
            frameCount++;
        }

        public void OnFrameDropped() {
            frameDropCount++;
            if (frameDropCount != 10)
                return;

            Debug.LogWarning(
                "Significant frame droppping was detected. This may introduce " +
                "time instability into output video. Decreasing the recording " +
                "frame rate is recommended."
            );
        }
    }
}