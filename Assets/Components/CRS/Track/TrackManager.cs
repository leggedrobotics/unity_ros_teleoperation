using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(TrackManager))]
public class TrackManagerEditor : SensorManagerEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        TrackManager trackManager = (TrackManager)target;
    }
}
#endif

public class TrackManager : SensorManager
{
    private void Awake()
    {
        name = "Track";
    }
}