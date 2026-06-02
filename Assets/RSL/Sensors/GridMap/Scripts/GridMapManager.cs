using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RSL.Sensors.GridMap
{
    #if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(GridMapManager))]
    public class GridMapManagerEditor : RSL.Core.SensorManagerEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            GridMapManager gridMapManager = (GridMapManager)target;
        }
    }
    #endif

    public class GridMapManager : RSL.Core.SensorManager
    {
    }
}
