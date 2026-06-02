using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RSL.Sensors.Markers
{
    #if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(MarkerManager))]
    public class MarkerManagerEditor : RSL.Core.SensorManagerEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MarkerManager markerManager = (MarkerManager)target;
        }
    }
    #endif

    public class MarkerManager : RSL.Core.SensorManager
    {
        public GameObject[] vizPrefabs;
    }
}
