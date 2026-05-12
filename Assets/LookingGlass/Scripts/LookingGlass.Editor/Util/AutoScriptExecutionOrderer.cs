using UnityEngine;
using UnityEditor;

namespace LookingGlass.Editor {
    [InitializeOnLoad]
    public static class AutoScriptExecutionOrderer {
        static AutoScriptExecutionOrderer() {
            EditorApplication.update += AutoCheckOrder;
        }

        private static void AutoCheckOrder() {
            EditorApplication.update -= AutoCheckOrder;
            int defaultOrder = HologramCamera.DefaultExecutionOrder;

            string scriptName = nameof(HologramCamera) + ".cs";
            string guid = "1d8741a64bf406d4d837e88bbb59fe58";
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            MonoScript script = null;
            if (string.IsNullOrWhiteSpace(assetPath) || (script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath)) == null) {
                Debug.LogError("Failed to find the " + scriptName + " script by GUID (This is needed to check if its order is set to " + defaultOrder + ")! Did its GUID accidentally change from " + guid + "?");
                return;
            }

            int currentOrder = MonoImporter.GetExecutionOrder(script);

            if (currentOrder != defaultOrder) {
                Debug.Log("Automatically setting the " + scriptName + " script to order " + defaultOrder + " so it is ready before other scripts during Awake, OnEnable, Start, etc.");
                MonoImporter.SetExecutionOrder(script, defaultOrder);
            }
        }
    }
}