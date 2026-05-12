using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace LookingGlass.Editor {
    public static class MenuItemUtil {
#if HAS_NEWTONSOFT_JSON
        [MenuItem("LookingGlass/lkg-settings.json/Reset", priority = 59)]
        private static void ResetLKGSettings() {
            string json = UnityNewtonsoftJSONSerializer.Serialize(LKGSettings.Default, true);
            File.WriteAllText(LKGSettingsSystem.FileName, json);
        }
#endif

        [MenuItem("LookingGlass/Retry LKG Bridge Connection", validate = true)]
        private static bool ValidateRetryLKGBridgeConnection() {
            return !LKGDisplaySystem.IsLoading;
        }

        [MenuItem("LookingGlass/Retry LKG Bridge Connection", validate = false)]
        private static void RetryLKGBridgeConnection() {
            _ = LKGDisplaySystem.Reconnect();
        }

        [MenuItem("Assets/Force Reserialize", priority = 41, validate = true)]
        private static bool ValidateReserializeSelectedAssets() {
            return Selection.assetGUIDs.Length > 0;
        }

        [MenuItem("Assets/Force Reserialize", priority = 41, validate = false)]
        private static void ForceReserializeSelectedAssets() {
            string[] assetGuids = Selection.assetGUIDs;

            HashSet<string> assetPaths = new HashSet<string>(assetGuids.Select(guid => AssetDatabase.GUIDToAssetPath(guid)));
            HashSet<string> allPaths = new HashSet<string>(assetPaths);

            void RecordAssetsUnderFolder(string folderPath) {
                string[] subfolderPaths = Directory.GetDirectories(folderPath);
                allPaths.Add(folderPath);

                foreach (string path in subfolderPaths)
                    RecordAssetsUnderFolder(path);

                string[] files = Directory.GetFiles(folderPath);
                foreach (string filePath in files)
                    if (!filePath.EndsWith(".meta"))
                        allPaths.Add(filePath);
            }

            foreach (string originalAssetPath in assetPaths) {
                if (Directory.Exists(originalAssetPath))
                    RecordAssetsUnderFolder(originalAssetPath);
            }

            AssetDatabase.ForceReserializeAssets(allPaths);
        }
    }
}
