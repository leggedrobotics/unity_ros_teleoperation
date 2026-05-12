//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using UnityEngine;
using UnityEditor;

namespace LookingGlass.Editor {
    public class ManualPreviewSettings : ScriptableObject {
        public bool manualPosition = true;
        public Vector2Int position = new Vector2Int(0, 0);
        public Vector2Int resolution = new Vector2Int(1536, 2048);

    }

    [CustomEditor(typeof(ManualPreviewSettings))]
    public class ManualPreviewSettingsEditor : UnityEditor.Editor {

        private void OnEnable() {
            Preview.onToggled += Repaint;
        }

        private void OnDisable() {
            Preview.onToggled -= Repaint;
        }

        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            LookingGlassGUIUtility.ToggleCastToLKGButton();
        }
    }
}
