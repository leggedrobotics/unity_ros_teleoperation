// Spawns the sensor VIEWER prefabs into the open scene so they can be
// photographed, and takes them away again.
//
// These prefabs are never in a scene at rest: a SensorManager instantiates one
// per feed at runtime. CANVAS_SHOT and UI_SHOT both find their target by name
// in the loaded scene, so without something like this there is no way to get a
// picture of a viewer's UI at all -- and no way to compare a port against the
// thing it replaces.
//
// Everything spawned is parented under one marker object, so cleanup is exact
// rather than name-matching whatever happens to be lying around.
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace RSL.EditorTools
{
    public static class SensorViewerCapture
    {
        private const string RigName = "__SensorViewerCapture__";

        private static readonly string[] ViewerPrefabs =
        {
            "Assets/RSL/Sensors/Service/ServiceButton.prefab",
            "Assets/RSL/Sensors/Lidar/LidarViewer.prefab",
            "Assets/RSL/Sensors/GridMap/GridMapViewer.prefab",
            "Assets/RSL/Sensors/Markers/Prefabs/MarkerViewer.prefab",
            "Assets/RSL/Sensors/Path/PathViewer.prefab",
            "Assets/RSL/Sensors/Pose/PoseViewer.prefab",
            "Assets/RSL/Sensors/Camera/Prefabs/CameraViewer.prefab",
            "Assets/RSL/Sensors/Camera/Prefabs/PanoViewer Variant.prefab",
            "Assets/RSL/Sensors/Camera/Prefabs/StereoViewer Variant.prefab",
        };

        [MenuItem("Tools/RSL/Sensor viewers: spawn for capture")]
        public static void Spawn()
        {
            Scene scene = SceneManager.GetActiveScene();
            Despawn();

            var rig = new GameObject(RigName);
            Undo.RegisterCreatedObjectUndo(rig, "Sensor viewer capture rig");
            // Well away from palmmenu so nothing of the menu can bleed into a
            // shot framed on one of these.
            rig.transform.position = new Vector3(0f, 0f, 40f);

            float x = 0f;
            foreach (string path in ViewerPrefabs)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) throw new Exception("missing " + path);

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, rig.transform);
                instance.name = prefab.name;
                instance.transform.localPosition = new Vector3(x, 0f, 0f);
                instance.transform.localRotation = Quaternion.identity;
                // Viewers ship with their submenu closed, the way a freshly
                // spawned one appears; open it so the shot shows the controls.
                SetActiveDeep(instance.transform, "FloatingMenu", true);
                SetActiveDeep(instance.transform, "Menu", true);
                SetActiveDeep(instance.transform, "TopMenu", true);

                // Neutralised, not left live: every viewer ships an
                // XRGrabInteractable (+ Rigidbody) for in-headset grabbing. In
                // Play mode the scene's own MR Interaction Setup rig runs a
                // simulated/default controller ray, and it found and grabbed
                // whichever viewer sat dead ahead of it (x=0, i.e. the FIRST
                // one spawned) -- which then behaved as a real grab: picked up
                // and, on some interaction managers' release/throw handling,
                // destroyed. Measured: with Service first in the array, ONLY
                // Service's GameObject vanished a few seconds into Play while
                // every other viewer (all off-centre) survived indefinitely.
                // This rig exists to test UI WIRING, not XR grabbing, so grab
                // support is switched off for the whole capture session.
                foreach (var grabbable in instance.GetComponentsInChildren<
                             UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(true))
                    grabbable.enabled = false;

                x += 1.5f;
                Debug.Log("[SensorViewerCapture] spawned " + instance.name);
            }

            EditorSceneManager.MarkSceneDirty(scene);
        }

        [MenuItem("Tools/RSL/Sensor viewers: remove capture rig")]
        public static void Despawn()
        {
            Scene scene = SceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != RigName) continue;
                UnityEngine.Object.DestroyImmediate(root);
                Debug.Log("[SensorViewerCapture] removed the capture rig");
            }
            EditorSceneManager.MarkSceneDirty(scene);
        }

        [MenuItem("Tools/RSL/Sensor viewers: dump rig tree (diag)")]
        public static void DumpRigTree()
        {
            GameObject rig = GameObject.Find(RigName);
            if (rig == null)
            {
                Debug.Log("[DumpRigTree] rig root '" + RigName + "' NOT FOUND (GameObject.Find, active-only)");
                // GameObject.Find only searches ACTIVE objects -- try inactive too.
                foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    if (root.name == RigName) { rig = root; break; }
                }
                if (rig == null) { Debug.Log("[DumpRigTree] not found among root objects either -- truly gone."); return; }
                Debug.Log("[DumpRigTree] found via root-object scan (was inactive/hidden from GameObject.Find)");
            }
            Walk(rig.transform, 0);
        }

        private static void Walk(Transform t, int depth)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(' ', depth * 2);
            sb.Append(t.name);
            sb.Append(" activeSelf=").Append(t.gameObject.activeSelf);
            var comps = t.GetComponents<Component>();
            foreach (var c in comps)
            {
                if (c == null) { sb.Append(" [MISSING SCRIPT]"); continue; }
                sb.Append(" [").Append(c.GetType().Name).Append("]");
            }
            Debug.Log("[DumpRigTree] " + sb);
            foreach (Transform child in t) Walk(child, depth + 1);
        }

        [MenuItem("Tools/RSL/Sensor viewers: find ServiceViewerUxml (diag)")]
        public static void FindServiceDiag()
        {
            int count = 0;
            foreach (UIDocument doc in UnityEngine.Object.FindObjectsByType<UIDocument>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (doc.name != "ServiceViewerUxml") continue;
                count++;
                Debug.Log("[FindServiceDiag] #" + count + " instanceID=" + doc.GetInstanceID() +
                          " scene=" + doc.gameObject.scene.name + " sceneValid=" + doc.gameObject.scene.IsValid() +
                          " activeSelf=" + doc.gameObject.activeSelf +
                          " activeInHierarchy=" + doc.gameObject.activeInHierarchy +
                          " path=" + GetPath(doc.transform));
            }
            Debug.Log("[FindServiceDiag] total=" + count);

            // Now try the EXACT same lookup FindDocumentRoot in the bridge uses,
            // reimplemented here so we can see if it disagrees.
            UIDocument viaResources = null;
            int rcount = 0;
            foreach (UIDocument candidate in Resources.FindObjectsOfTypeAll<UIDocument>())
            {
                if (candidate.name != "ServiceViewerUxml") continue;
                rcount++;
                bool valid = candidate.gameObject.scene.IsValid();
                Debug.Log("[FindServiceDiag] via Resources #" + rcount + " instanceID=" + candidate.GetInstanceID() +
                          " sceneValid=" + valid + " scene=" + candidate.gameObject.scene.name);
                if (valid && viaResources == null) viaResources = candidate;
            }
            Debug.Log("[FindServiceDiag] Resources total=" + rcount + " matched=" + (viaResources != null));
        }

        [MenuItem("Tools/RSL/Sensor viewers: list UIDocuments")]
        public static void ListDocuments()
        {
            foreach (UIDocument doc in UnityEngine.Object.FindObjectsByType<UIDocument>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Debug.Log("[SensorViewerCapture] UIDocument '" + doc.name + "' activeSelf=" +
                          doc.gameObject.activeSelf + " activeInHierarchy=" + doc.gameObject.activeInHierarchy +
                          " path=" + GetPath(doc.transform));
            }
        }

        private static string GetPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
            return path;
        }

        private static void SetActiveDeep(Transform root, string name, bool active)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) t.gameObject.SetActive(active);
        }
    }
}
