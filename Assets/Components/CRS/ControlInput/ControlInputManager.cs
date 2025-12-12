using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine.UI;
using System;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(ControlInputManager))]
public class ControlInputManagerEditor : SensorManagerEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        ControlInputManager ControlInputManager = (ControlInputManager)target;
        if (GUILayout.Button("Set Visualization Mode: Hide"))
        {
            ControlInputManager.SetVisualizationMode(0);
        }
        if (GUILayout.Button("Set Visualization Mode: Follow Car"))
        {
            ControlInputManager.SetVisualizationMode(1);
        }
        if (GUILayout.Button("Set Visualization Mode: Screen HUD"))
        {
            ControlInputManager.SetVisualizationMode(2);
        }
    }
}
#endif

public class ControlInputManager : SensorManager
{
    public GameObject gaugesHudPrefab;

    private ControlInputStream.VisualizationMode _currentMode = ControlInputStream.VisualizationMode.Hide;
    private GameObject _gaugesHudInstance;

    void Awake()
    {
        _ros = ROSConnection.GetOrCreateInstance();
        sensors = new List<GameObject>();

        if (transform.parent == null)
        {
            Debug.LogError("ControlInputManager must be a child of the Palm Menu Canvas.");
            return;
        }

        Canvas hud = transform.parent.GetComponentInParent<Canvas>();
        if (hud != null)
        {
            // create prefab instance on HUD canvas
            _gaugesHudInstance = Instantiate(gaugesHudPrefab, hud.transform);
            _gaugesHudInstance.name = "Gauges";
            _gaugesHudInstance.SetActive(false);
        } 
        else
        {
            Debug.LogError("No HUD Canvas found in ControlInputManager children.");
        }

        SetVisualizationMode((int)_currentMode);
        CreateDummyCount();
    }

    public void SetVisualizationMode(int mode)
    {
        _currentMode = (ControlInputStream.VisualizationMode) mode;
        
        bool sensorActive = sensors.Count > 0;
        if (_currentMode == ControlInputStream.VisualizationMode.Hide && sensorActive)
        {
            ClearAll();
        }
        else if (_currentMode != ControlInputStream.VisualizationMode.Hide && !sensorActive)
        {
            AddSensor();
        }

        UpdateSensor();
        _gaugesHudInstance.SetActive(_currentMode == ControlInputStream.VisualizationMode.ScreenHUD);
    }

    private void UpdateSensor()
    {
        bool hasSensor = sensors.Count > 0;

        if (hasSensor)
        {
            ControlInputStream controlInputStream = sensors[0].GetComponent<ControlInputStream>();
            if (controlInputStream != null)
            {
                controlInputStream.SetVisualizationMode((int)_currentMode);
                
                if (_currentMode == ControlInputStream.VisualizationMode.ScreenHUD)
                {
                    controlInputStream.torqueBarPanel = _gaugesHudInstance.transform.Find("TopMenu/TorqueBarPanel").gameObject;
                    controlInputStream.steeringWheelPanel = _gaugesHudInstance.transform.Find("TopMenu/SteeringWheelPanel").gameObject;
                }
            }
        }
    }

    private void CreateDummyCount()
    {
        if (count == null)
        {
            GameObject dummyObj = new GameObject("Dummy Count");
            dummyObj.transform.parent = this.transform;
            count = dummyObj.AddComponent<TMPro.TextMeshProUGUI>();
            count.gameObject.SetActive(false);
        }
    }
}