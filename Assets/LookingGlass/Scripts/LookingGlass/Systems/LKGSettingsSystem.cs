using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LookingGlass {
    /// <summary>
    /// A system that's always active in both edit mode and playmode, 
    /// </summary>
    public static class LKGSettingsSystem {
        [ExecuteAlways]
        private class BehaviourEvents : MonoBehaviour {
            public event Action onUpdate;

            private void Update() {
                onUpdate();
            }
        }

        /// <summary>
        /// The file containing settings for debugging and logging.
        /// </summary>
        /// <remarks>
        /// This file should go next to your Unity Assets folder (not inside), or next to your Unity build's main executable.<br />
        /// A <see cref="FileSystemWatcher"/> is used to detect and apply changes from it automatically as you edit and save the JSON file, so you do not need to re-build your project or exit playmode to have them take effect.
        /// </remarks>
        public const string FileName = "lkg-settings.json";

        private static object syncRoot = new();
        private static FileSystemWatcher fileWatcher;
        private static bool isDirty = false;
        private static string nextSettingsJSON = null;
        private static BehaviourEvents events;
        private static LKGSettings settings = LKGSettings.Default;

        /// <summary>
        /// The debugging and logging settings that are currently in-use for the LKG Unity Plugin.
        /// </summary>
        public static LKGSettings Settings => settings;

        internal static void InitializeSystem() {
            events = new GameObject(nameof(LKGSettingsSystem) + " Behaviour").AddComponent<BehaviourEvents>();
            events.gameObject.hideFlags = HideFlags.HideAndDontSave;
            events.onUpdate += OnUpdate;

#if HAS_NEWTONSOFT_JSON
            if (File.Exists(FileName)) {
                string fileText = File.ReadAllText(FileName);
                settings = UnityNewtonsoftJSONSerializer.Deserialize<LKGSettings>(fileText);
            }
#else
            settings = LKGSettings.Default;
#endif

            string folderPath = Path.GetDirectoryName(FileName);

            if (string.IsNullOrEmpty(folderPath))
                folderPath = Environment.CurrentDirectory;

            string fileName = Path.GetFileName(FileName);
            fileWatcher = new(folderPath, fileName);
            fileWatcher.EnableRaisingEvents = true;
            fileWatcher.Changed += OnFileChanged;
        }

        internal static void UninitializeSystem() {
            if (events != null)
                GameObject.DestroyImmediate(events);

            if (fileWatcher != null) {
                fileWatcher.Dispose();
                fileWatcher = null;
            }
        }

        private static void OnUpdate() {
            bool shouldUpdate = false;
            string json = null; //NOTE: Just for printing out what the new settings are to the console.
            lock (syncRoot) {
                shouldUpdate = isDirty;
                json = nextSettingsJSON;
                isDirty = false;
                nextSettingsJSON = null;
            }

            if (shouldUpdate) {
                Debug.Log("Detected file change on " + FileName + ": Applying new settings:\n" + json);
                ApplySettings();
            }

#if ENABLE_INPUT_SYSTEM
            if (settings.enableHologramDebugging) {
                if (!HologramDebugSnapshots.IsBusy()) {
                    Key[] keys = settings.hologramDebuggingKeys;
                    if (keys != null && keys.Length >= 1) {
                        Keyboard keyboard = InputSystem.GetDevice<Keyboard>();
                        if (keyboard != null) {
                            if (keyboard[keys[keys.Length - 1]].wasPressedThisFrame) {
                                bool allPressed = true;
                                for (int i = 0; i < keys.Length - 1; i++) {
                                    if (!keyboard[keys[i]].isPressed) {
                                        allPressed = false;
                                        break;
                                    }
                                }
                                if (allPressed) {
                                    HologramDebugSnapshots.SaveSnapshots();
                                }
                            }
                        }
                    }
                }
            }
#endif
        }

        private static void ApplySettings() {
            LKGSettings settings = LKGSettingsSystem.settings;
#if HAS_NEWTONSOFT_JSON
            LKGDisplaySystem.BridgeConnection.LoggingFlags = settings.loggingFlags;
#endif
        }

        private static void OnFileChanged(object sender, FileSystemEventArgs e) {
            _ = UpdateCalibrationAfterChanged(e.FullPath);
        }

        private static async Task UpdateCalibrationAfterChanged(string filePath) {
#if HAS_NEWTONSOFT_JSON
            string text = await File.ReadAllTextAsync(filePath);
            lock (syncRoot) {
                isDirty = true;
                nextSettingsJSON = text;
                settings = UnityNewtonsoftJSONSerializer.Deserialize<LKGSettings>(text);
            }
#endif
        }
    }
}
