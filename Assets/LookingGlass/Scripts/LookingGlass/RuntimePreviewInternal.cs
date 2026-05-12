using System;
using UnityEngine;

namespace LookingGlass {
    /// <summary>
    /// <para>An internal class useful for querying the status of the editor preview in runtime assemblies.</para>
    /// <para>This is particularly important for code that needs to be able to run in editor AND in builds, such as playmode unit tests.</para>
    /// </summary>
    internal static class RuntimePreviewInternal {
        private static Func<bool> isEditorPreviewActive;
        private static Action toggleEditorPreview;

        public static bool IsEditorPreviewActive {
            get {
                if (isEditorPreviewActive == null) {
                    Debug.LogError("Failed to check if editor preview was active! " + nameof(RuntimePreviewInternal) + " is not initialized.");
                    return false;
                }
                return isEditorPreviewActive();
            }
        }

        public static void ToggleEditorPreview() {
            if (toggleEditorPreview == null) {
                Debug.LogError("Failed to toggle editor preview! " + nameof(RuntimePreviewInternal) + " is not initialized.");
                return;
            }
            toggleEditorPreview();
        }

        internal static void Initialize(Func<bool> isEditorPreviewActive, Action toggleEditorPreview) {
            if (RuntimePreviewInternal.isEditorPreviewActive != null)
                throw new InvalidOperationException(nameof(RuntimePreviewInternal) + " has already been initialized!");

            RuntimePreviewInternal.isEditorPreviewActive = isEditorPreviewActive;
            RuntimePreviewInternal.toggleEditorPreview = toggleEditorPreview;
        }
    }
}