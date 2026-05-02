using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RSL.Sensors.PoseStreaming
{
    #if UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(StampedPoseManager))]
    public class StampedPoseManagerEditor : RSL.Core.SensorManagerEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            StampedPoseManager stampedPoseManager = (StampedPoseManager)target;
        }
    }
    #endif

    public class StampedPoseManager : RSL.Core.SensorManager
    {
        
    }
}
