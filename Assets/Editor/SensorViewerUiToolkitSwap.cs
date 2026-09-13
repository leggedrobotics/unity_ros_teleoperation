// Migration helper: Tools > RSL > Swap sensor viewers to UI Toolkit.
//
// Unlike the palmmenu swap, this edits the PREFAB ASSETS rather than a scene.
// It has to: these viewers are never in a scene at rest -- a SensorManager
// instantiates one per feed at runtime -- so there is no instance to override.
//
// Idempotent: re-running reuses the child and components it already made.
//
// The uGUI TopMenu is DELETED, not deactivated: every Stream subclass that
// used to reach into it directly (LidarStream's densitySlider/sizeSlider/
// colorModeDropdown/vizTypeDropdown, GridMapStream's colorDropdown/
// heightDropdown, and the bare topicDropdown.ClearOptions()/AddListener calls
// in MarkerStream/PathStream/StampedPoseStream/ServiceStream) has had that
// wiring removed in favour of the UI Toolkit panel calling the same
// OnXChange/OnXSelect methods directly -- so nothing is left holding a
// serialized reference into it that would go missing.
//
// The three camera viewers (CameraViewer, PanoViewer Variant, StereoViewer
// Variant) go through a DIFFERENT path, SwapImageViewer, not Swap -- but only
// because the document has to stay ALWAYS ACTIVE (a child riding topMenu's
// own SetActive(...), like every other viewer's panel, would mean the panel
// itself stops running -- and therefore stops being able to POLL topMenu --
// the moment topMenu closes). Styling and positioning otherwise match every
// other viewer exactly (.background.sensor-viewer, bottom-anchored on the old
// TopMenu's own rect): all three (Camera/Pano/Stereo) render their actual
// feed on the OLD mesh+material path regardless (a flat UI Toolkit Image
// cannot reproduce Stereo's per-eye shader or stand in for Pano's sphere --
// see SensorImageViewer.uxml's header comment), so what's ported is JUST the
// options menu, same shape as everyone else's. TopMenu is deleted here too
// (see SwapImageViewer's own comment) -- its two remaining jobs, the
// open/closed flag and the tracking icon, moved onto ImageView.MenuOpen /
// ImageView.TrackingState, plain properties ImageViewerPanel polls instead of
// something riding TopMenu's own activeSelf.
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using RSL.Core.Menu;
using RSL.Sensors.Camera;
using RSL.Sensors.GridMap;
using RSL.Sensors.Lidar;
using RSL.Sensors.Markers;
using RSL.Sensors.Path;
using RSL.Sensors.Pose;
using RSL.Sensors.Service;

namespace RSL.EditorTools
{
    public static class SensorViewerUiToolkitSwap
    {
        private const string PanelSettingsAsset = "Assets/RSL/Core/Menu/ToolkitAssets/WorldPanelSettings.asset";
        private const string Uxml = "Assets/RSL/Core/Menu/ToolkitAssets/";

        // TEMPORARY, testing only: lists every live Canvas's GameObject name
        // and RectTransform size, to find the right CANVAS_SHOT target
        // without archaeology through prefab YAML. Remove after use.
        public static void ListCanvases()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var c in Resources.FindObjectsOfTypeAll<Canvas>())
            {
                if (!c.gameObject.scene.IsValid()) continue;
                var rt = c.GetComponent<RectTransform>();
                sb.Append(c.gameObject.name).Append(" activeInHierarchy=").Append(c.gameObject.activeInHierarchy)
                  .Append(" renderMode=").Append(c.renderMode)
                  .Append(" size=").Append(rt != null ? rt.rect.size.ToString() : "?").Append('\n');
            }
            Debug.Log(sb.ToString());
        }

        [MenuItem("Tools/RSL/Swap sensor viewers to UI Toolkit")]
        public static void Apply()
        {
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsAsset);
            if (panelSettings == null) throw new Exception("missing " + PanelSettingsAsset);

            // The CAMERA viewers are deliberately absent. They are the one group
            // still on uGUI, by request, and they are also the awkward ones: the
            // panel sits beside a live RawImage of the feed rather than standing
            // alone.
            Swap("Assets/RSL/Sensors/Service/ServiceButton.prefab",
                 "FloatingMenu/Menu/TopMenu", "ServiceViewerUxml", Uxml + "SensorServiceViewer.uxml",
                 panelSettings, go => Attach<ServiceViewerPanel, ServiceStream>(go, (p, s) => p.stream = s));

            Swap("Assets/RSL/Sensors/Lidar/LidarViewer.prefab",
                 "FloatingMenu/TopMenu", "LidarViewerUxml", Uxml + "SensorLidarViewer.uxml",
                 panelSettings, go => Attach<LidarViewerPanel, LidarStream>(go, (p, s) => p.stream = s));

            Swap("Assets/RSL/Sensors/GridMap/GridMapViewer.prefab",
                 "FloatingMenu/TopMenu", "GridMapViewerUxml", Uxml + "SensorGridMapViewer.uxml",
                 panelSettings, go => Attach<GridMapViewerPanel, GridMapStream>(go, (p, s) => p.stream = s));

            Swap("Assets/RSL/Sensors/Markers/Prefabs/MarkerViewer.prefab",
                 "FloatingMenu/TopMenu", "MarkerViewerUxml", Uxml + "SensorMarkerViewer.uxml",
                 panelSettings, go => Attach<MarkerViewerPanel, MarkerStream>(go, (p, s) => p.stream = s));

            Swap("Assets/RSL/Sensors/Path/PathViewer.prefab",
                 "FloatingMenu/TopMenu", "PathViewerUxml", Uxml + "SensorPathViewer.uxml",
                 panelSettings, go => Attach<PathViewerPanel, PathStream>(go, (p, s) => p.stream = s));

            Swap("Assets/RSL/Sensors/Pose/PoseViewer.prefab",
                 "FloatingMenu/TopMenu", "PoseViewerUxml", Uxml + "SensorPoseViewer.uxml",
                 panelSettings, go => Attach<PoseViewerPanel, StampedPoseStream>(go, (p, s) => p.stream = s));

            // CameraViewer.prefab is the base of PanoViewer Variant and
            // StereoViewer Variant -- swapping the base swaps the menu for all
            // three, since neither variant restates topMenu's own structure.
            SwapImageViewer("Assets/RSL/Sensors/Camera/Prefabs/CameraViewer.prefab");

            AssetDatabase.SaveAssets();
            Debug.Log("[SensorViewerUiToolkitSwap] Done.");
        }

        /// <summary>
        /// Adds the panel component and points it at the viewer's own stream.
        /// </summary>
        private static void Attach<TPanel, TStream>(GameObject go, Action<TPanel, TStream> assign)
            where TPanel : MonoBehaviour
            where TStream : Component
        {
            var panel = go.GetComponent<TPanel>() ?? go.AddComponent<TPanel>();
            assign(panel, go.GetComponentInParent<TStream>());
            EditorUtility.SetDirty(panel);
        }

        /// <summary>
        /// Swaps CameraViewer's TopMenu (and so, by variant inheritance,
        /// PanoViewer's and StereoViewer's) for the shared options-menu panel,
        /// then deletes TopMenu entirely.
        /// </summary>
        // UNLIKE Swap()/every other viewer, the new document is a SIBLING of
        // where TopMenu used to be, always active, not a child riding a
        // GameObject's own SetActive(...) -- it has to keep running (and
        // polling ImageView.MenuOpen) while visually hidden, which a document
        // that something else switches off could not do. Positioning
        // otherwise matches Swap() exactly (bottom-center anchor, Dynamic
        // content-driven sizing, the same 0.1 scale): this menu is styled
        // with the same .background.sensor-viewer class every other viewer's
        // uses, so it should look and size the same way too.
        //
        // TopMenu itself is DELETED, not just deactivated like the swap tool
        // does for its sibling viewers' legacy content. It earned that
        // exception: once its buttons/dropdown/tracking icon were all
        // retired in favour of the UI Toolkit options menu (the icon via
        // ImageView.TrackingState, the open/closed flag via
        // ImageView.MenuOpen), nothing was left reading or writing to it --
        // an entire uGUI GameObject kept alive purely to serve as a boolean.
        private static void SwapImageViewer(string prefabPath)
        {
            const string uxml = Uxml + "SensorImageViewer.uxml";
            const string goName = "ImageViewerUxml";

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                // Null, not thrown, when already run: TopMenu is DELETED (not
                // deactivated) by this method, so a second pass -- the whole
                // point of "Idempotent" above -- has nothing left to find.
                Transform topMenu = root.transform.Find("TopMenu");
                Transform cameraName = root.transform.Find("CameraName");
                if (cameraName == null) throw new Exception("no CameraName in " + prefabPath);

                // A stale ImageViewerUxml nested INSIDE TopMenu, left over
                // from an earlier pass before this became a sibling of
                // TopMenu, needs no special handling any more -- deleting
                // TopMenu wholesale, below, takes it with it either way.
                Transform existing = root.transform.Find(goName);
                GameObject go = existing != null ? existing.gameObject : new GameObject(goName);
                if (existing == null)
                {
                    go.transform.SetParent(root.transform, false);
                    go.layer = root.gameObject.layer;
                }

                // Anchored to the VIDEO QUAD's own rendered top edge (Img's
                // Renderer.bounds), not TopMenu's legacy authored rect. TopMenu
                // is anchored to the top of the whole viewer ROOT's rect
                // (anchorMin/Max (0.5,1), plus its own +1 anchoredPosition on
                // top of that) -- a fine anchor for the OLD uGUI popup, which
                // was authored against that same rect, but it sits well above
                // where the video quad's own bounds actually end, so the new
                // menu looked disconnected/"way too far" from the thing it is
                // a menu for. Bounds, not the rect, because that is the one
                // measure guaranteed to track the actual visible content
                // regardless of how TopMenu itself happened to be authored.
                Transform img = root.transform.Find("Img");
                if (img == null) throw new Exception("no Img in " + prefabPath);
                Renderer imgRenderer = img.GetComponent<Renderer>();
                if (imgRenderer == null) throw new Exception("Img has no Renderer in " + prefabPath);
                Bounds videoBounds = imgRenderer.bounds;
                Vector3 topCentre = new Vector3(videoBounds.center.x, videoBounds.max.y, videoBounds.center.z);
                go.transform.position = topCentre;
                go.transform.rotation = root.transform.rotation;
                // NOT a bare 0.1f copied from Swap() -- that produced a menu
                // about 100x too small to see (same failure mode, and the same
                // fix, as the earlier VideoImage scale bug). Swap()'s 0.1 only
                // works there because THOSE viewers' roots sit at lossyScale
                // (1,1,1) (confirmed on LidarViewer.prefab); CameraViewer's own
                // root is (0.01,0.01,0.01), so this has to divide that back out
                // to land at the SAME effective 0.1 total scale every other
                // viewer's panel uses, rather than hard-coding a second number
                // that would silently go wrong again if the root were ever
                // rescaled -- mirrors PalmmenuUiToolkitSwap.SlotScale's own
                // target-over-parent division for the same reason.
                float parentScale = root.transform.lossyScale.x;
                go.transform.localScale = Vector3.one * (0.1f / parentScale);

                var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxml);
                if (asset == null) throw new Exception("missing " + uxml);

                UIDocument doc = go.GetComponent<UIDocument>() ?? go.AddComponent<UIDocument>();
                doc.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsAsset);
                if (doc.panelSettings == null) throw new Exception("missing " + PanelSettingsAsset);
                doc.visualTreeAsset = asset;
                doc.worldSpaceSizeMode = UIDocument.WorldSpaceSizeMode.Dynamic;
                // Grows upward from the edge placed above, same as Swap().
                doc.pivot = Pivot.BottomCenter;
                doc.pivotReferenceSize = PivotReferenceSize.Layout;

                Attach<ImageViewerPanel, RSL.Core.SensorStream>(go, (p, s) => p.stream = s);

                // CameraName is LEFT ACTIVE now, unlike the frame-era version
                // of this swap: there is no more UI Toolkit-side topic label
                // to duplicate it, so ImageView's nameText keeps showing the
                // topic name exactly where it always did before any of this
                // migration touched Camera/Pano/Stereo.
                if (cameraName.gameObject.activeSelf == false)
                    cameraName.gameObject.SetActive(true);

                // TopMenu (its old buttons, dropdown, and Track/Image/Image
                // tracking icon all included) is DELETED wholesale here, not
                // deactivated -- see this method's own header comment for
                // why it earned that exception to the usual retire-in-place
                // rule the rest of this migration follows. Null when a
                // previous pass already deleted it.
                if (topMenu != null) UnityEngine.Object.DestroyImmediate(topMenu.gameObject, true);

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log("[SensorViewerUiToolkitSwap] " + System.IO.Path.GetFileName(prefabPath) +
                          ": added " + goName + ", deleted TopMenu.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

private static void Swap(string prefabPath, string legacyPath, string goName, string uxml,
                                 PanelSettings panelSettings, Action<GameObject> configure)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                // Null, not thrown, when already run: legacy is DELETED (not
                // deactivated) below, so a second pass has nothing left to
                // find there -- the parent GROUP survives either way, so it
                // is resolved by path rather than via legacy.parent.
                Transform legacy = root.transform.Find(legacyPath);
                int slash = legacyPath.LastIndexOf('/');
                Transform parent = slash < 0 ? root.transform
                                              : root.transform.Find(legacyPath.Substring(0, slash));
                if (parent == null) throw new Exception("no parent of " + legacyPath + " in " + prefabPath);

                Transform existing = parent.Find(goName);
                GameObject go = existing != null ? existing.gameObject : new GameObject(goName);

                if (existing == null)
                {
                    if (legacy == null) throw new Exception("no " + legacyPath + " in " + prefabPath);

                    go.transform.SetParent(parent, false);
                    go.layer = legacy.gameObject.layer;

                    // Stand on the BOTTOM EDGE of the uGUI panel, not its centre.
                    // These documents are content-sized, so the one thing that must
                    // not move as they grow is the edge that meets the title bar
                    // underneath. Anchoring there and pivoting BottomCenter means a
                    // panel taller than its predecessor grows upward into free space
                    // instead of down through the handle.
                    //
                    // Taken off the RectTransform's own rect rather than computed
                    // from anchors: legacy.rect is in the panel's local space
                    // whatever its pivot happens to be, so this is exact without
                    // special-casing any of them. Only needed on the FIRST pass --
                    // legacy will not survive to a second one to recompute this from.
                    var legacyRect = (RectTransform)legacy;
                    Vector3 bottomCentre = legacyRect.TransformPoint(
                        new Vector3(legacyRect.rect.center.x, legacyRect.rect.yMin, 0f));
                    go.transform.position = bottomCentre;
                    go.transform.localRotation = legacy.localRotation;
                    // 100px per canvas unit divided by the 10 these documents are
                    // authored at: the panel measures 30 units wide either way.
                    go.transform.localScale = Vector3.one * 0.1f;
                }

                var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxml);
                if (asset == null) throw new Exception("missing " + uxml);

                UIDocument doc = go.GetComponent<UIDocument>() ?? go.AddComponent<UIDocument>();
                doc.panelSettings = panelSettings;
                doc.visualTreeAsset = asset;
                doc.worldSpaceSizeMode = UIDocument.WorldSpaceSizeMode.Dynamic;
                // Grows upward from the edge placed above. Layout, not
                // BoundingBox: the pivot should track the laid-out size, which
                // is what Dynamic sizing drives.
                doc.pivot = Pivot.BottomCenter;
                doc.pivotReferenceSize = PivotReferenceSize.Layout;

                configure(go);

                // DELETED wholesale, not deactivated -- see this file's own
                // header comment for why it is safe now (nothing holds a
                // serialized reference into it any more). Null when a
                // previous pass already deleted it.
                if (legacy != null) UnityEngine.Object.DestroyImmediate(legacy.gameObject, true);

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log("[SensorViewerUiToolkitSwap] " + System.IO.Path.GetFileName(prefabPath) +
                          ": added " + goName + ", deleted " + legacyPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
