using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RSL.Sensors.ServiceCaller
{
    #if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(ServiceManager))]
    public class ServiceManagerEditor : RSL.Core.SensorManagerEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            ServiceManager serviceManager = (ServiceManager)target;
        }
    }
    #endif

    public class ServiceManager : RSL.Core.SensorManager
    {
    }
}
