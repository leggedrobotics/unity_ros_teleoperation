using System.Collections;
using System.Collections.Generic; 
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(MarkerManager))]
public class MarkerManagerEditor : SensorManagerEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        MarkerManager markerManager = (MarkerManager)target;
    }
}
#endif

public class MarkerManager : SensorManager
{
    public GameObject[] vizPrefabs;
}
