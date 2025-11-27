using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
}
