using System;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace LookingGlass.Editor {
    [InitializeOnLoad]
    public class EditorFocusUtility {
        public static event Action<bool> onFocusChanged = (focus) => { };

        private static bool wasFocused;

        static EditorFocusUtility() {
            EditorApplication.update += Update;
            onFocusChanged += ForceRepaintOnRefocus;
        }

        private static void Update() {
            bool isFocused = InternalEditorUtility.isApplicationActive;

            if (wasFocused != isFocused) {
                onFocusChanged?.Invoke(isFocused);
                wasFocused = isFocused;
            }
        }

        //NOTE: This helps the user's GameView update and prevent being stretched oddly.. Not sure why Unity doesn't update properly with our plugin
        private static void ForceRepaintOnRefocus(bool isFocused) {
            if (isFocused)
                EditorUpdates.ForceUnityRepaintImmediate();
        }
    }
}
