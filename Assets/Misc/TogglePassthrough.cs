using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class TogglePassthrough : MonoBehaviour
{
    public ARCameraManager arCameraManager;

    public void Toggle()
    {
        arCameraManager.enabled = !arCameraManager.enabled;
    }
}
