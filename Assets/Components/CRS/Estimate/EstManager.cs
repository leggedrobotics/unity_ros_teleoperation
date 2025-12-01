using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine.UI;
using System;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(EstManager))]
public class EstManagerEditor : SensorManagerEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        EstManager EstManager = (EstManager) target;
        if (GUILayout.Button("Toggle Car Visibility"))
        {
            EstManager.ToggleCarVisibility();
        }
        if (GUILayout.Button("Toggle Trail Visibility"))
        {
            EstManager.ToggleTrailVisibility();
        }
    }
}
#endif

public class EstManager : SensorManager
{
    public GameObject carPrefab;
    private bool _showCar = false;
    private bool _showTrail = false;

    void Awake()
    {
        _ros = ROSConnection.GetOrCreateInstance();
        sensors = new List<GameObject>();
        CreateDummyCount();
    }

    public void ToggleCarVisibility()
    {
        _showCar = !_showCar;
        UpdateSensor();
    }

    public void ToggleTrailVisibility()
    {
        _showTrail = !_showTrail;
        UpdateSensor();
    }

    private void UpdateSensor()
    {
        bool hasSensor = sensors.Count > 0;

        if (hasSensor && !_showCar && !_showTrail)
        {
            ClearAll();
        }

        if (!hasSensor && (_showCar || _showTrail))
        {
            AddSensor();
            EstimatorCarStream estStream = sensors[0].GetComponent<EstimatorCarStream>();
            estStream.carPrefab = carPrefab;
            hasSensor = true;
        }
        if (hasSensor)
        {
            EstimatorCarStream estStream = sensors[0].GetComponent<EstimatorCarStream>();
            estStream.showEstimator = _showCar;
            estStream.showTrail = _showTrail;
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
