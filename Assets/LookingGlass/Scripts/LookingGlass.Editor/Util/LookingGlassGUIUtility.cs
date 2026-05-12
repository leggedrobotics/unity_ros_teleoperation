using System;
using UnityEngine;

namespace LookingGlass.Editor {
    public static class LookingGlassGUIUtility {
        private const string CastKeyboardShortcut =
#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
            "⌘E";
#else
            "Ctrl + E";
#endif

        private static readonly Lazy<GUIContent> CastLabel = new Lazy<GUIContent>(() => new GUIContent("Cast to Looking Glass (" + CastKeyboardShortcut + ")"));
        private static readonly Lazy<GUIContent> StopCastingLabel = new Lazy<GUIContent>(() => new GUIContent("Stop Casting (" + CastKeyboardShortcut + ")"));

        public static bool ToggleCastToLKGButton() {
            GUIContent label = Preview.IsActive ? StopCastingLabel.Value : CastLabel.Value;

            if (GUILayout.Button(label)) {
                Preview.TogglePreview();
                return true;
            }
            return false;
        }
    }
}