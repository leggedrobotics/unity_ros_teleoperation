using System;

namespace LookingGlass {
    /// <summary>
    /// Defines extended key codes for Looking Glass devices that are not supported by default in Unity.
    /// </summary>
    [Serializable]
    public enum PortraitKeyCode : int {
        //TODO: DOCUMENT: Why 0xB0, 0xB1, 0xB3? Anything special about these values,
        //other than the fact that UnityEngine.KeyCode probably doesn't use them?
        //How about their relations to UnityEngine.InputSystem.Key?
        
        // This enum adds the media keys as options in the KeyMap class in InputManager.cs. See Line 40.
        /// <summary>
        /// The media next key.
        /// </summary>
        MediaNext = 0xB0,

        /// <summary>
        /// The media previous key.
        /// </summary>
        MediaPrevious = 0xB1,

        /// <summary>
        /// The media play/pause key.
        /// </summary>
        MediaPlayPause = 0xB3,
    }
}
