using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class PassthroughToggle : MonoBehaviour
{
    private ARCameraManager arCameraManager;
    void Start()
    {
        arCameraManager = FindObjectOfType<ARCameraManager>();
        if (arCameraManager == null)
        {
            Debug.LogWarning("ARCameraManager not found in the scene.");
        }
    }

    public void SetPassthroughEnabled(int disabled)
    {
        if (arCameraManager != null)
        {
            arCameraManager.enabled = disabled == 0;
        }
    }
}
