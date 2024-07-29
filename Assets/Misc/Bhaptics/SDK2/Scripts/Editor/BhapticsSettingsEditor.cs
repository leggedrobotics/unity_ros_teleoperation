using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Bhaptics.SDK2
{
    [CustomEditor(typeof(BhapticsSettings))]
    public class BhapticsSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            GUI.enabled = false;
            base.OnInspectorGUI();
            GUI.enabled = true;
        }
    }
}