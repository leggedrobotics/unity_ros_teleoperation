using System;
using UnityEngine;
using LookingGlass.Toolkit;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LookingGlass {
    /// <summary>
    /// Represents the central entry point for LKG systems that require being active in both edit mode and playmode.
    /// </summary>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    internal static class UnityLKGSystems {
        private static bool initialized = false;

        public static bool Initialized => initialized;

#if UNITY_EDITOR
        static UnityLKGSystems() {
            InitializeAllSystems();

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state) {
            switch (state) {
                case PlayModeStateChange.ExitingEditMode:
                    UninitializeAllSystems();
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    InitializeAllSystems();
                    break;
            }
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInitialize() {
            InitializeAllSystems();
        }

        private static bool InitializeAllSystems() {
            if (initialized)
                return false;
            initialized = true;
            Application.quitting += () => UninitializeAllSystems();
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload += () => UninitializeAllSystems();
#endif
            PerformSafe(LKGSettingsSystem.InitializeSystem);
            PerformSafe(LKGDisplaySystem.InitializeSystem);
            return true;
        }

        private static bool UninitializeAllSystems() {
            if (!initialized)
                return false;
            initialized = false;
            PerformSafe(LKGDisplaySystem.UninitializeSystem);
            PerformSafe(LKGSettingsSystem.UninitializeSystem);

            if (ServiceLocator.Instance != null) {
                ServiceLocator.Instance.Dispose();
                ServiceLocator.Instance = null;
            }
            return true;
        }

        private static void PerformSafe(Action callback) {
            try {
                callback();
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }
    }
}
