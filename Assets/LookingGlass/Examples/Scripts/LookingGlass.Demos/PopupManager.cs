using LookingGlass.Demos;
using System;
using UnityEngine;

public class PopupManager : MonoBehaviour {
    [Tooltip("Reference to the pop-up GameObject for calibration prompts. This appears when the user needs to calibrate their display.")]
    [SerializeField] GameObject popUpPair;
    [Tooltip("Reference to the pop-up GameObject that displays the test image for visual calibration.")]
    [SerializeField] GameObject popUpTest;


    private void OnEnable()
    {
        DemoIOSUIController.TogglePairPopUp += TogglePairPopUp;
        SettingButtonController.TogglePairPopUp += TogglePairPopUp;
        DemoIOSUIController.ToggleTestPopUp += ToggleTestPopUp;

        ToggleTestPopUp(false);
        TogglePairPopUp(false);
    }

    private void OnDisable() {
        DemoIOSUIController.TogglePairPopUp -= TogglePairPopUp;
        SettingButtonController.TogglePairPopUp -= TogglePairPopUp;
        DemoIOSUIController.ToggleTestPopUp -= ToggleTestPopUp;
    }

    void TogglePairPopUp(bool isOn) {
        if (popUpPair != null) {
            popUpPair.SetActive(isOn);
        } else {
            Debug.LogError("PopupManager: popUpPair is null. Please assign the Pair Pop-Up GameObject in the Inspector.");
        }
    }

    void ToggleTestPopUp(bool isOn) {
        if (popUpTest != null) {
            popUpTest.SetActive(isOn);
        } else {
            Debug.LogError("PopupManager: popUpTest is null. Please assign the Test Pop-Up GameObject in the Inspector.");
        }
    }
}