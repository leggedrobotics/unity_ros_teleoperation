using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RSL.Core.Menu
{
    #if UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(ProfileManager))]
    public class ProfileManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ProfileManager myScript = (ProfileManager)target;
            if (GUILayout.Button("Clear Settings"))
            {
                myScript.ClearSettings();
            }
        }
    }
    #endif

    public class ProfileManager : MonoBehaviour
    {
        void Start()
        {
            
        }

        void Update()
        {
            
        }

        public void ClearSettings()
        {
            PlayerPrefs.DeleteAll();
        }
    }
}
