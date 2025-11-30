using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(ControlInputManager))]
public class ControlInputManagerEditor : SensorManagerEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        ControlInputManager ControlInputManager = (ControlInputManager)target;
    }
}
#endif

public class ControlInputManager : SensorManager
{
    void Start()
    {
        name = "ControlInput";
        tag = "ControlInput";
    }
}