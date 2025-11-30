using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(EstManager))]
public class EstManagerEditor : SensorManagerEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        EstManager EstManager = (EstManager)target;
    }
}
#endif

public class EstManager : SensorManager
{
}
