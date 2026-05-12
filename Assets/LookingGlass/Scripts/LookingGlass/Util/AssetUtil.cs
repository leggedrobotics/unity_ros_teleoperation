using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LookingGlass {
    public static class AssetUtil {
#if UNITY_EDITOR
        /// <summary>
        /// Replaces an existing asset at <paramref name="assetPath"/> with <paramref name="newAsset"/>, or creates a new asset at that path if none exists yet.
        /// </summary>
        /// <param name="assetPath">The asset path to save the new asset at.</param>
        /// <param name="newAsset">The Unity object to save.</param>
        public static void EditorReplaceAsset(string assetPath, Object newAsset) {
            bool needsToOverwrite = AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null || File.Exists(assetPath);
            string originalMetaFilePath = null;
            string tempMetaFilePath = null;
            if (needsToOverwrite) {
                originalMetaFilePath = assetPath + ".meta";
                tempMetaFilePath = "Temp/" + Path.GetFileName(originalMetaFilePath);

                Directory.CreateDirectory(Path.GetDirectoryName(tempMetaFilePath));
                File.Copy(originalMetaFilePath, tempMetaFilePath, true);
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.Refresh();
            }

            AssetDatabase.CreateAsset(newAsset, assetPath);

            if (needsToOverwrite) {
                File.Copy(tempMetaFilePath, originalMetaFilePath, true);
                AssetDatabase.Refresh();
            }
        }
#endif
    }
}
