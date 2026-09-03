using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RSL.Sensors.Path
{
    #if UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(PathManager))]
    public class PathManagerEditor : RSL.Core.SensorManagerEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }
    #endif


    public class PathManager : RSL.Core.SensorManager
    {
    }
}
