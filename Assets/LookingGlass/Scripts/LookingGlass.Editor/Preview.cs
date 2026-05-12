//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace LookingGlass.Editor {
    [InitializeOnLoad]
    public static class Preview {
        /// This section ensures our Preview windows close before assembly reloads, and re-open (if needed) after assembly reloads.
        #region Persistence
        /// <summary>
        /// A data class that's serialized to a JSON file to remember whether or not the <see cref="Preview"/> was <see cref="Preview.IsActive">active</see>or not.
        /// </summary>
        [Serializable]
        private class PersistentPreviewData {
            public bool wasPreviewActive;
        }

        private static bool isChangingBetweenPlayMode = false;

        /// <summary>
        /// The JSON file that saves data between
        /// assembly reloads and playmode state changes.
        /// </summary>
        private static string PreviousStateFilePath => Path.Combine(Application.temporaryCachePath, "Preview Data.json");

        static Preview() {
            HologramCamera.onAnyCalibrationReloaded += OnAnyCalibrationReloaded;

            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
            EditorApplication.quitting += DeleteFile;
            EditorApplication.wantsToQuit += () => {
                CloseAllWindowsImmediate();
                return true;
            };
            EditorSceneManager.sceneOpened += RecheckDisplayTargetOnSceneOpened;
        }

        private static void OnAnyCalibrationReloaded(HologramCamera hologramCamera) {
            if (hologramCamera == HologramCamera.Instance)
                EditorUpdates.ForceUnityRepaint();
        }

        //NOTE: The order of operations is:
        //(so the AssemblyReloadEvents take care of ENTERING playmode)

        //The user enters playmode:
        //PlayModeStateChange.ExitingEditMode
        //AssemblyReloadEvents.beforeAssemblyReload
        //AssemblyReloadEvents.afterAssemblyReload
        //PlayModeStateChange.EnteredPlayMode

        //The user exits playmode:
        //PlayModeStateChange.ExitingPlayMode
        //PlayModeStateChange.EnteredEditMode

        //--- --- --- --- --- --- --- --- --- --- --- ---
        //UNITY BUG: Unity throws an internal NullReferenceException in 2019.4+ some time AFTER
        //our code in PlayModeStateChanged(PlayModeStateChange.EnteredEditMode) if we explicitly call
        //      Preview.CloseAllWindowsImmediate()
        //It seems to be because when we have the Preview window open and we return to edit mode, we call:
        //      ConsumeLoadPreview(string) --> Preview.TogglePreview --> Preview.CloseAllWindowsImmediate,
        //So we ALREADY closed our windows..
        //Not sure why this would be an issue though. We'd need to trace through all the calls to see which call even triggers the issue.
        //Lines related to all of this are clearly marked below "//TODO INVESTIGATE: NullReferences upon re-entering edit mode"
        //--- --- --- --- --- --- --- --- --- --- --- ---

        private static void OnBeforeAssemblyReload() {
            SavePreview(PreviousStateFilePath);

            if (!isChangingBetweenPlayMode)
                Preview.CloseAllWindowsImmediate();         //TODO INVESTIGATE: NullReferences upon re-entering edit mode

            EditorUpdates.ForceUnityRepaintImmediate();
        }

        private static void OnAfterAssemblyReload() {
            ConsumeLoadPreview(PreviousStateFilePath);
        }

        private static void PlayModeStateChanged(PlayModeStateChange state) {
            switch (state) {
                case PlayModeStateChange.ExitingEditMode:
                    isChangingBetweenPlayMode = true;
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    isChangingBetweenPlayMode = false;
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    isChangingBetweenPlayMode = true;
                    SavePreview(PreviousStateFilePath);     //TODO INVESTIGATE: NullReferences upon re-entering edit mode
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    isChangingBetweenPlayMode = false;
                    ConsumeLoadPreview(PreviousStateFilePath);
                    break;
            }
        }

        private static void SavePreview(string filePath) {
            //If we're already waiting to consume the previous data, DO NOT overwrite it (upon 2nd attempt to save our state) because we close the preview --
            //It'd ALWAYS write false on every subsequent call to this method!
            if (File.Exists(filePath))
                return;

            string json = JsonUtility.ToJson(new PersistentPreviewData() {
                wasPreviewActive = Preview.IsActive
            }, true);

            File.WriteAllText(filePath, json);

            //TODO INVESTIGATE: NullReferences upon re-entering edit mode
            //      (We used to explicitly close all preview windows here, but no longer)
        }

        private static void ConsumeLoadPreview(string filePath) {
            string json = !File.Exists(filePath) ? "{ }" : File.ReadAllText(filePath);
            PersistentPreviewData data = JsonUtility.FromJson<PersistentPreviewData>(json);
            File.Delete(filePath);

            //NOTE: Delay 1 frame because the LKGServiceLocator may also be initializing in editor on the same 1st frame:
            EditorApplication.delayCall += () => {
                _ = RestoreStateAsync(data.wasPreviewActive);
            };
        }

        private static async Task RestoreStateAsync(bool wasPreviewActive) {
            try {
                await LKGDisplaySystem.WaitForCalibrations();

                if (wasPreviewActive != Preview.IsActive)
                    Preview.TogglePreview();
                EditorUpdates.ForceUnityRepaint();
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        private static void DeleteFile() {
            File.Delete(PreviousStateFilePath);
        }

        private static void RecheckDisplayTargetOnSceneOpened(Scene openScene, OpenSceneMode openSceneMode) {
            if (openSceneMode == OpenSceneMode.Single)
                CloseAllWindowsImmediate();

            //NOTE: If we don't wait 1 frame, auto-clicking on the maximize button doesn't seem to work..
            //So let's just wait a frame then! ;)
            EditorUpdates.Delay(1, () => {
                if (!Preview.IsActive)
                    Preview.TogglePreview();
            });
        }
        #endregion

        public const string manualSettingsPath = "Assets/HologramCameraPreviewSettings.asset";
        private static ManualPreviewSettings manualPreviewSettings;

        public static event Action onToggled;

        public static bool IsActive {
            get {
                if (!HologramCamera.AnyEnabled)
                    return false;

                return PreviewWindow.Count > 0;
            }
        }

        public static bool UseManualPreview => manualPreviewSettings != null && manualPreviewSettings.manualPosition;
        public static ManualPreviewSettings ManualPreviewSettings => manualPreviewSettings;

        [InitializeOnLoadMethod]
        private static void InitPreview() {
            RuntimePreviewInternal.Initialize(() => IsActive, () => TogglePreview());
            EditorUpdates.Delay(1, AutoCloseExtraWindows);
        }

        [MenuItem("Assets/Create/LookingGlass/Manual Preview Settings")]
        private static void CreateManualPreviewAsset() {
            ManualPreviewSettings previewSettings = AssetDatabase.LoadAssetAtPath<ManualPreviewSettings>(manualSettingsPath);
            if (previewSettings == null) {
                previewSettings = ScriptableObject.CreateInstance<ManualPreviewSettings>();
                AssetDatabase.CreateAsset(previewSettings, manualSettingsPath);
                AssetDatabase.SaveAssets();
            }
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = previewSettings;
        }

        [MenuItem("LookingGlass/Toggle Preview %e", false, 1)]
        public static bool TogglePreview() {
            if (manualPreviewSettings == null)
                manualPreviewSettings = AssetDatabase.LoadAssetAtPath<ManualPreviewSettings>(manualSettingsPath);
            return TogglePreviewInternal();
        }

        private static void AutoCloseExtraWindows() {
            if (manualPreviewSettings != null && (LKGDisplaySystem.LKGDisplayCount <= 0)) {
                int count = PreviewWindow.Count;
                if (count > 0)
                    Debug.Log("[LookingGlass] Closing " + count + " extra Hologram Camera window(s).");

                PreviewWindow.CloseAll();
            }
        }

        private static bool TogglePreviewInternal() {
            bool wasActive = IsActive;

            CloseAllWindowsImmediate();
            EditorUpdates.Delay(5, () => {
                if (!wasActive)
                    _ = OpenAllWindows();
                else
                    EditorUpdates.ForceUnityRepaintImmediate();

                onToggled?.Invoke();
            });

            return !wasActive;
        }

        internal static async Task OpenAllWindows() {
            try {
                if (HologramCamera.Count == 0)
                    Debug.LogWarning("Unable to create a " + nameof(PreviewWindow) + ": there was no " + nameof(HologramCamera) + " instance available.");

                await LKGDisplaySystem.WaitForCalibrations();

                GameViewExtensions.UpdateUserGameViews();
                if (!UseManualPreview && (LKGDisplaySystem.LKGDisplayCount <= 0)) {
                    Debug.LogWarning("No Looking Glass detected. Please ensure your display is correctly connected, or use manual preview settings instead.");
                    CloseAllWindowsImmediate();
                    return;
                }

                foreach (HologramCamera camera in HologramCamera.All) {
                    if (camera.HasTargetDevice && PreviewWindow.IsPreviewOpenForCamera(camera)) {
                        Debug.LogWarning("Skipping preview for " + camera.name + " because its target LKG device already has a preview showing! The game views would overlap.");
                        continue;
                    }
                    PreviewWindow preview = PreviewWindow.Create(camera);
                }

                GameViewExtensions.UpdateUserGameViews();
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        internal static void CloseAllWindowsImmediate() {
            PreviewWindow.CloseAll();
        }
    }
}
