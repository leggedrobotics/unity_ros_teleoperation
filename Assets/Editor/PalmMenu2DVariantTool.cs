// Migration helper: Tools > RSL > Create palmmenu 2D variant.
//
// The 2D scenes (2dMain.unity and friends) carried their own hand-maintained
// duplicate of the palm menu -- "2dMenu Variant.prefab" -- which is NOT an
// actual Unity Prefab Variant of palmmenu.prefab despite the name (it is a
// fully independent root GameObject hierarchy, confirmed by its own file
// starting with a plain GameObject block rather than a PrefabInstance one).
// It never tracked palmmenu's own migration to UI Toolkit and still carries
// the old per-manager uGUI rows (Points Header, Cameras Header, ...) this
// project retired everywhere else.
//
// This creates a REAL Prefab Variant of palmmenu.prefab, so every future
// change to the core menu is inherited here automatically instead of having
// to be hand-copied into a second, drifting prefab. The only override this
// variant needs is which PanelSettings its UIDocuments point at: palmmenu's
// own UIDocuments (SettingsMenu, GeneralSettings -- confirmed the only two)
// all reference WorldPanelSettings (render mode WorldSpace, meant for a
// panel anchored in 3D space on the palm). A 2D/desktop scene has no palm to
// anchor to and wants an ordinary full-screen overlay instead, so the
// variant repoints both at ScreenPanelSettings (identical scale/reference-
// resolution settings, just render mode ScreenSpaceOverlay) -- everything
// else (UXML, USS, the panel MonoBehaviours, the whole hierarchy) is
// inherited from palmmenu.prefab untouched.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace RSL.EditorTools
{
    public static class PalmMenu2DVariantTool
    {
        private const string PalmMenuPrefab = "Assets/RSL/Core/Menu/Prefabs/palmmenu.prefab";
        private const string ScreenPanelSettingsAsset = "Assets/RSL/Core/Menu/ToolkitAssets/ScreenPanelSettings.asset";
        private const string VariantPath = "Assets/RSL/Core/Menu/Prefabs/palmmenu 2D Variant.prefab";

        [MenuItem("Tools/RSL/Create palmmenu 2D variant")]
        public static void Create()
        {
            var baseAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PalmMenuPrefab);
            if (baseAsset == null) throw new System.Exception("missing " + PalmMenuPrefab);

            var screenSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(ScreenPanelSettingsAsset);
            if (screenSettings == null) throw new System.Exception("missing " + ScreenPanelSettingsAsset);

            // InstantiatePrefab (not LoadPrefabContents) specifically because
            // it keeps the instance linked to baseAsset as its Prefab source
            // -- SaveAsPrefabAsset on a still-linked instance, saved to a
            // NEW path, is what makes Unity record the result as a Variant
            // rather than an independent copy.
            Scene scratch = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(baseAsset, scratch);

                UIDocument[] docs = instance.GetComponentsInChildren<UIDocument>(true);
                if (docs.Length == 0) throw new System.Exception("no UIDocument under palmmenu");
                foreach (UIDocument doc in docs)
                {
                    doc.panelSettings = screenSettings;
                    EditorUtility.SetDirty(doc);
                }

                GameObject variant = PrefabUtility.SaveAsPrefabAsset(instance, VariantPath, out bool success);
                if (!success) throw new System.Exception("SaveAsPrefabAsset failed for " + VariantPath);

                bool isVariant = PrefabUtility.GetPrefabAssetType(variant) == PrefabAssetType.Variant;
                Debug.Log("[PalmMenu2DVariantTool] created " + VariantPath +
                          " isVariant=" + isVariant + " uiDocuments=" + docs.Length);

                Object.DestroyImmediate(instance);
            }
            finally
            {
                EditorSceneManager.CloseScene(scratch, true);
            }

            AssetDatabase.SaveAssets();
        }

        // Swaps whichever menu instance is currently in the open scene --
        // either the original broken "2dMenu" (hand-maintained duplicate) or
        // an already-swapped-but-stale "palmmenu 2D Variant" (a scene
        // PrefabInstance can carry its own per-instance property overrides
        // that outlive a later fix to the variant ASSET, e.g. after Create()
        // is re-run to catch UIDocuments it missed the first time -- a fresh
        // InstantiatePrefab has none of those, so re-running this after
        // fixing Create() output is the reliable way to pick up the fix) --
        // for a fresh instance of the current "palmmenu 2D Variant.prefab".
        // Same parent, same local transform. The old instance's own
        // poseController/twistController/activeController overrides (from
        // the original "2dMenu" case) are dead weight (HandPub.cs resolves
        // those as private InputActions via InputSystem.actions.FindAction
        // at runtime now, not serialized fields), so nothing needs replaying
        // onto the new one.
        [MenuItem("Tools/RSL/Swap 2dMenu to palmmenu 2D variant (this scene)")]
        public static void SwapInCurrentScene()
        {
            var variantAsset = AssetDatabase.LoadAssetAtPath<GameObject>(VariantPath);
            if (variantAsset == null) throw new System.Exception("missing " + VariantPath + " -- run Create() first");

            GameObject old = null;
            foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (!candidate.scene.IsValid()) continue;
                if (candidate.name == "2dMenu" || candidate.name == "palmmenu 2D Variant") { old = candidate; break; }
            }
            if (old == null) throw new System.Exception("no GameObject named 2dMenu or palmmenu 2D Variant in the open scene");

            Transform oldT = old.transform;
            Transform parent = oldT.parent;
            int siblingIndex = oldT.GetSiblingIndex();
            Vector3 localPos = oldT.localPosition;
            Quaternion localRot = oldT.localRotation;
            Vector3 localScale = oldT.localScale;
            Scene scene = old.scene;

            GameObject replacement = (GameObject)PrefabUtility.InstantiatePrefab(variantAsset, scene);
            replacement.transform.SetParent(parent, false);
            replacement.transform.localPosition = localPos;
            replacement.transform.localRotation = localRot;
            replacement.transform.localScale = localScale;
            replacement.transform.SetSiblingIndex(siblingIndex);
            replacement.name = "palmmenu 2D Variant";

            Object.DestroyImmediate(old);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PalmMenu2DVariantTool] replaced 2dMenu with palmmenu 2D Variant in " + scene.name);
        }

    }
}
