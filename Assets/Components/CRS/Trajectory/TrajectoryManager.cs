using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine.UI;
using System;

#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(TrajectoryManager))]
public class TrajectoryManagerEditor : SensorManagerEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        TrajectoryManager markerManager = (TrajectoryManager)target;
    }
}
#endif

public class TrajectoryManager : SensorManager
{
    public GameObject[] vizPrefabs;

    void Awake()
    {
        _ros = ROSConnection.GetOrCreateInstance();
        sensors = new List<GameObject>();
        CreateDummyCount();
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
