using UnityEngine;
using UnityEditor;

namespace LookingGlass.Editor {
    [CustomEditor(typeof(PluginVersionAsset))]
    public class PluginVersionAssetEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            using (new EditorGUI.DisabledScope(!HologramCamera.isDevVersion)) {
                DrawDefaultInspector();
            }
        }
    }
}
