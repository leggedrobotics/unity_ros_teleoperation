using System;
using System.Threading.Tasks;
using UnityEngine;
using LookingGlass.Toolkit;

namespace LookingGlass.Demos {
    public class DemoGetAllLKGDisplays : MonoBehaviour {
        private void OnEnable() {
            _ = PerformExample();
        }

        private async Task PerformExample() {
            try {
                if (LKGDisplaySystem.IsLoading) {
                    Debug.Log("Waiting for displays & calibrations...");
                    await LKGDisplaySystem.WaitForCalibrations();
                } else {
                    Debug.Log("Reconnecting to LKG Bridge to get displays & calibrations...");
                    bool result = await LKGDisplaySystem.Reconnect();
                    if (!result)
                        Debug.LogWarning("Connecting to LKG Bridge and retrieving LKG displays & calibration failed to complete fully.");
                }

                for (int i = 0; i < LKGDisplaySystem.LKGDisplayCount; i++) {
                    Toolkit.Display display = LKGDisplaySystem.Get(i);

                    int xpos = display.hardwareInfo.windowCoordX;
                    int ypos = display.hardwareInfo.windowCoordY;
                    Calibration cal = display.calibration;
                    QuiltSettings defaultSettings = display.defaultQuilt;

                    string message = "Found a display at (arbitrary) index " + i + ":\n\n" +
                        "Display XY Screen Position: (" + xpos + ", " + ypos + ")\n\n" +
                        "Calibration: " + JsonUtility.ToJson(cal, true) + ",\n\n" +
                        "Default Quilt Settings: " + JsonUtility.ToJson(defaultSettings, true);
                    Debug.Log(message);
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }
    }
}
