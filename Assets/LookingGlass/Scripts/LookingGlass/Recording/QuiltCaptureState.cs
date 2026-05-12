using System;

namespace LookingGlass {
    /// <summary>
    /// Describes the state of a <see cref="QuiltCapture"/>, including whether it is recording or not, or if it's paused.
    /// </summary>
    [Serializable]
    public enum QuiltCaptureState {
        NotRecording = 0,
        Recording,
        Paused
    }
}
