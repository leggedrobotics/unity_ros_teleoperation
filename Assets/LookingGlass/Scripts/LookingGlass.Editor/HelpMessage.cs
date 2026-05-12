using UnityEngine;
using UnityEditor;

namespace LookingGlass.Editor {
    /// <summary>
    /// <para>Represents a help message shown in editor GUI to the user of a <see cref="HologramCamera"/> component in the inspector.</para>
    /// <para>See also: <seealso cref="EditorGUILayout.HelpBox(string, MessageType)"/></para>
    /// </summary>
    public struct HelpMessage {
        public static HelpMessage None => default;

        /// <summary>
        /// Describes whether or not this is info, a warning, or an error.
        /// </summary>
        public MessageType type;

        /// <summary>
        /// The message (if any), to be displayed to the user in the inspector.
        /// </summary>
        public string message;

        /// <summary>
        /// Is there a message that should be shown?
        /// </summary>
        public bool HasMessage => !string.IsNullOrEmpty(message);

        public HelpMessage(MessageType type, string message) {
            this.type = type;
            this.message = message;
        }
    }
}