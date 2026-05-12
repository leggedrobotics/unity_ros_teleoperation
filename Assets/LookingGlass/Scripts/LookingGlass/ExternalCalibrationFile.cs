#if HAS_NEWTONSOFT_JSON
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using LookingGlass.Toolkit;

namespace LookingGlass {
    /// <summary>
    /// Allows you to read <see cref="Calibration"/> from a file outside of the Unity project, and set it to the <see cref="HologramCameraDebugging.ManualCalibration"/> for rendering.
    /// </summary>
    public class ExternalCalibrationFile : MonoBehaviour {
        [SerializeField] private HologramCamera hologramCamera;

        [Tooltip("The calibration JSON file to read and watch for change events.\n\n" +
            "WARNING: You should not use your built-in calibration visual.json file. Making a copy is highly recommended, to avoid tampering with the original copy.")]
        [SerializeField] private string calibrationFilePath = "E:/LKG_calibration/visual (Copy).json";

        private object syncRoot = new();
        private FileSystemWatcher fileWatcher;

        private bool externalFileLoaded;
        private bool isDirty = false;
        private Calibration externalCalibration;

        private void OnEnable() {
            if (hologramCamera == null) {
                Debug.LogError("The " + nameof(HologramCamera) + " to target for using an external calibration file was not assigned.");
                return;
            }
            if (!File.Exists(calibrationFilePath)) {
                Debug.LogError("The calibration JSON file does not exist at: " + calibrationFilePath);
                return;
            }
            _ = LoadCalibrationFromExternalFile();
        }

        private void OnDisable() {
            if (fileWatcher != null) {
                fileWatcher.Dispose();
                fileWatcher = null;
            }
        }

        private void Update() {
            if (!externalFileLoaded)
                return;
            bool shouldUpdate = false;
            Calibration c;
            lock (syncRoot) {
                shouldUpdate = isDirty;
                isDirty = false;
                c = externalCalibration;
            }

            if (shouldUpdate) {
                hologramCamera.Debugging.UseManualCalibration(externalCalibration);
            }
        }

        private async Task LoadCalibrationFromExternalFile() {
            try {
                Task<string> readTask = File.ReadAllTextAsync(calibrationFilePath);
                string folderPath = Path.GetDirectoryName(calibrationFilePath);
                string fileName = Path.GetFileName(calibrationFilePath);
                fileWatcher = new(folderPath, fileName);
                fileWatcher.EnableRaisingEvents = true;
                string text = await readTask;

                Calibration c;
                lock (syncRoot) {
                    c = externalCalibration = Calibration.Parse(text);
                }

                await hologramCamera.WaitForInitialization();
                hologramCamera.Debugging.UseManualCalibration(c);
                fileWatcher.Changed += OnFileChanged;

                externalFileLoaded = true;
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e) {
            _ = UpdateCalibrationAfterChanged(e.FullPath);
        }

        private async Task UpdateCalibrationAfterChanged(string filePath) {
            string text = await File.ReadAllTextAsync(filePath);
            lock (syncRoot) {
                isDirty = true;
                externalCalibration = Calibration.Parse(text);
            }
        }
    }
}
#endif
