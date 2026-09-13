// Migration helper: Tools > RSL > Swap palmmenu to UI Toolkit (this scene).
//
// Applies the UI Toolkit swap to the palmmenu instance in the CURRENTLY OPEN
// scene as prefab-instance OVERRIDES, leaving palmmenu.prefab itself untouched
// so every other scene keeps the old uGUI menus. Run it again on Main.unity
// when that scene is ready to switch over.
//
// Idempotent: re-running reuses the components and child it already created
// rather than stacking duplicates.
//
// Done through Unity rather than by editing the scene YAML directly: overrides
// are not simple text. An added component needs an entry in the instance's
// m_AddedComponents list, a stripped GameObject stub, and a component document
// whose m_PrefabInstance points back at the instance -- all bookkeeping Unity
// owns. Writing it by hand means reimplementing that and getting it subtly
// wrong; letting Unity serialise it means it is correct by construction.
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using RSL.Core.Menu;

namespace RSL.EditorTools
{
    public static class PalmmenuUiToolkitSwap
    {
        private const string ConnectionUxml = "Assets/RSL/Core/Menu/ToolkitAssets/ConnectionSettings.uxml";
        private const string GeneralUxml = "Assets/RSL/Core/Menu/ToolkitAssets/GeneralSettings.uxml";
        private const string DebugUxml = "Assets/RSL/Core/Menu/ToolkitAssets/DebugSettings.uxml";
        private const string WifiEntryUxml = "Assets/RSL/Core/Menu/ToolkitAssets/WifiEntry.uxml";
        private const string PanelSettingsAsset = "Assets/RSL/Core/Menu/ToolkitAssets/WorldPanelSettings.asset";
        private const string SensorMenuUxml = "Assets/RSL/Core/Menu/ToolkitAssets/SensorMenu.uxml";
        private const string MenuShellUxml = "Assets/RSL/Core/Menu/ToolkitAssets/MenuShell.uxml";
        private const string PalmTabUxml = "Assets/RSL/Core/Menu/ToolkitAssets/PalmTab.uxml";
        private const string SensorRowUxml = "Assets/RSL/Core/Menu/ToolkitAssets/SensorRow.uxml";
        private const string SensorRowTrackedUxml = "Assets/RSL/Core/Menu/ToolkitAssets/SensorRowTracked.uxml";
        private const string SensorRowAudioUxml = "Assets/RSL/Core/Menu/ToolkitAssets/SensorRowAudio.uxml";

        // Scene-root objects that only ever existed to develop the panels in
        // isolation. testMenuManager is included: it spawns its own copies of the
        // Cameras managers, which then collide with the ones inside palmmenu.
        // Recreate it any time from Tools > RSL > Create testMenuManager.
        private static readonly string[] RootDebugObjects =
        {
            "UIDocument", "ConnectionUIDocument", "DebugUIDocument", "ShellUIDocument",
            "testMenuManager",
        };

        [MenuItem("Tools/RSL/Swap palmmenu to UI Toolkit (this scene)")]
        public static void Apply()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject palmmenu = Find(scene, "palmmenu");
            if (palmmenu == null) throw new Exception("no 'palmmenu' in the open scene");

            // Earlier runs parented the Points and Cameras lists one level too
            // high; leaving those behind would show two of each.
            foreach (string strayPath in new[]
                     {
                         "MenuCanvas/LidarManager/LidarMenu/BaseMenu/SensorMenuPoints",
                         "MenuCanvas/CameraManagers/SensorMenuCameras",
                     })
            {
                Transform stray = palmmenu.transform.Find(strayPath);
                if (stray == null) continue;
                Undo.DestroyObjectImmediate(stray.gameObject);
                Debug.Log($"[PalmmenuUiToolkitSwap] Removed misplaced list at {strayPath}.");
            }

            // Remove the standalone harness documents from the scene root. They
            // were scaffolding for developing the panels in isolation; now that
            // every panel lives inside palmmenu they are duplicates, and the
            // managers testMenuManager spawned compete with palmmenu's own.
            foreach (string debugName in RootDebugObjects)
            {
                GameObject stray = Find(scene, debugName);
                if (stray == null) continue;
                Undo.DestroyObjectImmediate(stray);
                Debug.Log($"[PalmmenuUiToolkitSwap] Removed scene-root harness object '{debugName}'.");
            }

            // Strip components whose script no longer exists. SensorMenuPanel was
            // an earlier design that MenuTemplate replaced; deleting the .cs left
            // its serialized components behind on the menus created back then, and
            // Unity reports those as "missing script" on every load.
            int removed = 0;
            foreach (GameObject rootGo in scene.GetRootGameObjects())
                foreach (Transform t in rootGo.GetComponentsInChildren<Transform>(true))
                    removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            if (removed > 0) Debug.Log($"[PalmmenuUiToolkitSwap] Removed {removed} missing-script component(s).");

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsAsset);
            if (panelSettings == null) throw new Exception("missing " + PanelSettingsAsset);

            // --- Wifi: adopt the existing placeholder ------------------------
            // NOTE the name: the ConnectionSettings placeholder is called
            // "SettingsMenu" and lives under Wifi/WifiMenu, while a DIFFERENT
            // GameObject also called "SettingsMenu" lives under Settings. Paths,
            // never bare names.
            SwapExisting(palmmenu,
                placeholderPath: "MenuCanvas/Wifi/WifiMenu/SettingsMenu",
                legacyPath: "MenuCanvas/Wifi/WifiMenu/GameObject",
                uxml: ConnectionUxml,
                panelSettings: panelSettings,
                configure: go =>
                {
                    var panel = go.GetComponent<ConnectionSettingsPanel>() ?? go.AddComponent<ConnectionSettingsPanel>();
                    panel.profileEntryTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(WifiEntryUxml);
                });

            // --- Settings: adopt the existing placeholder --------------------
            SwapExisting(palmmenu,
                placeholderPath: "MenuCanvas/Settings/SettingsMenu/GeneralSettings",
                legacyPath: "MenuCanvas/Settings/SettingsMenu/GameObject",
                uxml: GeneralUxml,
                panelSettings: panelSettings,
                configure: go =>
                {
                    if (go.GetComponent<GeneralSettingsPanel>() == null) go.AddComponent<GeneralSettingsPanel>();
                });

            // --- Debug: no placeholder exists, so create the child ------------
            Transform tfDebug = palmmenu.transform.Find("MenuCanvas/Debug Menus/TFDebug");
            if (tfDebug == null) throw new Exception("no MenuCanvas/Debug Menus/TFDebug under palmmenu");

            Transform existing = tfDebug.Find("DebugSettings");
            GameObject debugGo = existing != null ? existing.gameObject : new GameObject("DebugSettings");
            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(debugGo, "Add DebugSettings");
                debugGo.transform.SetParent(tfDebug, false);
                // Matches the two hand-placed placeholders: world-space UI Toolkit
                // panels are authored a tenth of the size palmmenu is built at.
                debugGo.transform.localScale = Vector3.one * 10f;
                debugGo.layer = tfDebug.gameObject.layer;
            }
            ConfigureDocument(debugGo, DebugUxml, panelSettings);
            if (debugGo.GetComponent<DebugSettingsPanel>() == null) debugGo.AddComponent<DebugSettingsPanel>();

            // Hide TFDebug's uGUI chrome -- but NOT TFViz, which is the actual TF
            // visualiser the new panel drives.
            SetActive(tfDebug.Find("TF Header"), false);
            SetActive(tfDebug.Find("Settings Menu"), false);

            // --- Sensor lists: hand each existing MenuTemplate a document ----
            // palmmenu already has two MenuTemplates, split by tag ("System" in
            // the Settings menu, "Points" in the Lidar menu) and those tag
            // filters survive the refactor. So this does not create a parallel
            // list system -- it gives each existing template a UIDocument to
            // build into, and lets SetupRows do the rest.
            //
            // The System list is placed at local x=30 because that is where
            // MenuTemplate's old BaseMenu sat (anchoredPosition 30,-67) -- the
            // column beside the settings controls, matching the old layout.
            WireMenuTemplate(palmmenu,
                             templatePath: "MenuCanvas/Settings",
                             parentPath: "MenuCanvas/Settings/SettingsMenu",
                             goName: "SensorMenuSystem",
                             localPosition: new Vector3(30f, 0f, 0f),
                             panelSettings: panelSettings,
                             // No legacyPath: this list's old "System Header" plate
                             // sits under Settings/SettingsMenu/GameObject, which
                             // the SwapExisting call above already deactivates.
                             legacyPath: null,
                             tagFilterIfEmpty: null);

            // Points sits at LidarMenu, NOT inside its BaseMenu. Lidar has two
            // nested toggles: MenuManager enables/disables LidarMenu, and
            // MenuTemplate.ToggleMenu toggles BaseMenu within it. A list parented
            // inside BaseMenu never becomes active in hierarchy while LidarMenu is
            // off, so its UIDocument gets no visual tree and MenuListHost never
            // fires -- the list simply stayed empty. LidarMenu is the level the
            // Settings list uses too, and that one has worked throughout.
            //
            // BaseMenu is deliberately left ACTIVE: it is MenuTemplate.menu, so
            // deactivating it (as an earlier pass did) is undone by the next button
            // press anyway, and it holds no static content to hide.
            WireMenuTemplate(palmmenu,
                             templatePath: "MenuCanvas/LidarManager",
                             parentPath: "MenuCanvas/LidarManager/LidarMenu",
                             goName: "SensorMenuPoints",
                             localPosition: Vector3.zero,
                             panelSettings: panelSettings,
                             // The old uGUI title plate. SensorMenu.uxml draws its
                             // own H1 header from the tag filter, so leaving this on
                             // is what made the list look like two stacked menus.
                             legacyPath: "MenuCanvas/LidarManager/LidarMenu/BaseMenu/Points Header",
                             tagFilterIfEmpty: null);
            // The third MenuTemplate comes from a SensorsManager prefab nested
            // at MenuCanvas/CameraManagers. Its tagFilter is EMPTY, which under
            // the old code meant "list every manager" -- so the camera menu would
            // duplicate the System and Points lists wholesale. The managers are
            // tagged Cameras / System / Points, so "Cameras" is what it should
            // have been; set only when the field is still empty, never over an
            // existing value.
            // Same for Cameras: its BaseMenu is the toggled container, so the list
            // belongs inside it rather than hanging off CameraManagers itself,
            // which is always active.
            WireMenuTemplate(palmmenu,
                             templatePath: "MenuCanvas/CameraManagers",
                             parentPath: "MenuCanvas/CameraManagers/BaseMenu",
                             goName: "SensorMenuCameras",
                             localPosition: Vector3.zero,
                             panelSettings: panelSettings,
                             legacyPath: "MenuCanvas/CameraManagers/BaseMenu/Cameras Header",
                             tagFilterIfEmpty: "Cameras");

            WirePalmBar(palmmenu, panelSettings);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PalmmenuUiToolkitSwap] Applied as prefab-instance overrides and saved " + scene.name);
        }

        /// <summary>
        /// Apply, then build all three lists, then save.
        ///
        /// Apply saves before any list is built, because wiring is what it is for.
        /// The rows are built afterwards and SPAWN managers into the scene, so
        /// without a second save those spawns are lost on the next scene load --
        /// which is exactly what happens when this is driven headlessly and
        /// nothing ever presses Setup Rows by hand.
        /// </summary>
        [MenuItem("Tools/RSL/Swap palmmenu and build all lists (this scene)")]
        public static void ApplyAndBuildLists()
        {
            Apply();

            Scene scene = SceneManager.GetActiveScene();
            GameObject palmmenu = Find(scene, "palmmenu");
            foreach (string path in new[]
                     { "MenuCanvas/Settings", "MenuCanvas/LidarManager", "MenuCanvas/CameraManagers" })
            {
                Transform host = palmmenu.transform.Find(path);
                var template = host != null ? host.GetComponent<MenuTemplate>() : null;
                if (template == null) throw new Exception("no MenuTemplate at " + path);
                template.SetupRows();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PalmmenuUiToolkitSwap] Built all three lists and saved " + scene.name);
        }

        /// <summary>
        /// Drops a standalone sensor menu at the scene root, filtered to Cameras.
        /// A rig for exercising MenuTemplate on its own: nothing is nested inside a
        /// closed submenu, so pressing Setup Rows shows its effect immediately.
        /// </summary>
        /// <summary>
        /// Gives each manager PREFAB the row markup its own controls need, so the
        /// UI travels with the manager instead of the menu guessing.
        ///
        /// CameraManager gets the row with the TF tracking button; AudioManager
        /// gets the mic/speaker row; everything else falls back to the plain row
        /// via MenuTemplate.defaultManagerUi, so it needs nothing set.
        /// </summary>
        [MenuItem("Tools/RSL/Assign manager row UIs to prefabs")]
        public static void AssignManagerRowUis()
        {
            var tracked = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SensorRowTrackedUxml);
            var audio = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SensorRowAudioUxml);
            if (tracked == null || audio == null) throw new Exception("missing a row UXML");

            int assigned = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/RSL" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var manager = prefab != null ? prefab.GetComponent<RSL.Core.SensorManager>() : null;
                if (manager == null) continue;

                // A prefab VARIANT inherits managerUi from its base, so only assign
                // where the value would actually differ -- otherwise every variant
                // gains a pointless override.
                VisualTreeAsset wanted = manager is RSL.Sensors.Camera.CameraManager ? tracked
                                       : manager is RSL.Sensors.Audio.AudioManager ? audio
                                       : null;
                if (wanted == null || manager.managerUi == wanted) continue;

                manager.managerUi = wanted;
                EditorUtility.SetDirty(manager);
                assigned++;
                Debug.Log($"[AssignManagerRowUis] {System.IO.Path.GetFileName(path)} -> {wanted.name}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[AssignManagerRowUis] Assigned {assigned} manager row UI(s).");
        }

        /// <summary>
        /// Prints how world-space input is configured for these panels.
        /// </summary>
        // Worth having permanently: whether a world-space UIDocument can be
        // pointed at in VR hinges on PanelSettings' collider update mode, which
        // serialises as a bare int whose enum names are not guessable from the
        // YAML. Read through SerializedObject because the property is not public
        // in this Unity version -- enumDisplayNames also hands back the option
        // labels in declaration order, which is what makes the int readable.
        [MenuItem("Tools/RSL/Log world-space panel input setup")]
        public static void LogPanelInputSetup()
        {
            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsAsset);
            if (ps == null) throw new Exception("missing " + PanelSettingsAsset);

            var so = new SerializedObject(ps);
            SerializedProperty mode = so.FindProperty("m_ColliderUpdateMode");
            SerializedProperty trigger = so.FindProperty("m_ColliderIsTrigger");
            SerializedProperty render = so.FindProperty("m_RenderMode");

            string options = mode != null ? string.Join(" | ", mode.enumDisplayNames) : "<none>";
            Debug.Log($"[PanelInput] renderMode={(render != null ? render.intValue : -1)} " +
                      $"colliderUpdateMode={(mode != null ? mode.intValue : -1)} " +
                      $"colliderIsTrigger={(trigger != null && trigger.boolValue)}\n" +
                      $"[PanelInput] mode options in order: {options}\n" +
                      $"[PanelInput] current = " +
                      (mode != null && mode.intValue >= 0 && mode.intValue < mode.enumDisplayNames.Length
                          ? mode.enumDisplayNames[mode.intValue] : "?"));

            foreach (UIDocument doc in UnityEngine.Object.FindObjectsByType<UIDocument>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var box = doc.GetComponent<BoxCollider>();
                Debug.Log($"[PanelInput] {doc.name}: sizeMode={doc.worldSpaceSizeMode} " +
                          $"pivot={doc.pivot} collider={(box == null ? "none" : box.size.ToString())}");
            }
        }

        /// <summary>
        /// Ports ManagerToggler.prefab (the debug sensor-manager on/off list) to a
        /// UI Toolkit ManagerTogglerPanel. Edits the PREFAB ASSET directly, like
        /// SensorViewerUiToolkitSwap does for viewer TopMenus -- ManagerToggler has
        /// its own root ("BaseMenu") at rest, it is not scene-only content the way
        /// palmmenu's inline debug menus are, so there is no scene instance to
        /// override here.
        /// </summary>
        [MenuItem("Tools/RSL/Swap ManagerToggler to UI Toolkit")]
        public static void SwapManagerToggler()
        {
            const string prefabPath = "Assets/RSL/Core/Menu/Prefabs/ManagerToggler.prefab";
            const string uxml = "Assets/RSL/Core/Menu/ToolkitAssets/ManagerToggler.uxml";

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsAsset);
            if (panelSettings == null) throw new Exception("missing " + PanelSettingsAsset);

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                // Both are grandchildren, not direct children: the prefab's
                // true root is "ManagerToggler" itself (holding the old
                // MonoBehaviour), one level above "BaseMenu", which holds
                // "Stream Header" and "Menu".
                Transform header = root.transform.Find("BaseMenu/Stream Header");
                Transform menu = root.transform.Find("BaseMenu/Menu");
                if (menu == null) throw new Exception("no BaseMenu/Menu under " + prefabPath);

                const string goName = "ManagerTogglerUxml";
                Transform existing = root.transform.Find(goName);
                GameObject go = existing != null ? existing.gameObject : new GameObject(goName);
                if (existing == null)
                {
                    go.transform.SetParent(root.transform, false);
                    go.layer = root.gameObject.layer;
                }
                go.transform.position = root.transform.position;
                go.transform.rotation = root.transform.rotation;
                go.transform.localScale = Vector3.one * 0.1f;

                var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxml);
                if (asset == null) throw new Exception("missing " + uxml);

                UIDocument doc = go.GetComponent<UIDocument>() ?? go.AddComponent<UIDocument>();
                doc.panelSettings = panelSettings;
                doc.visualTreeAsset = asset;
                doc.worldSpaceSizeMode = UIDocument.WorldSpaceSizeMode.Dynamic;
                doc.pivot = Pivot.Center;
                doc.pivotReferenceSize = PivotReferenceSize.Layout;

                if (go.GetComponent<ManagerTogglerPanel>() == null) go.AddComponent<ManagerTogglerPanel>();

                if (header != null) header.gameObject.SetActive(false);
                menu.gameObject.SetActive(false);

                // The old ManagerToggler MonoBehaviour was fully replaced (not
                // left running alongside, unlike the OTHER retire-not-delete
                // swaps in this migration) and its component instance already
                // destroyed by an earlier run of this method, before the now-
                // deleted ManagerToggler.cs made referencing that type here a
                // compile error. Idempotent re-runs no longer need to check for
                // it.

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log("[PalmmenuUiToolkitSwap] ManagerToggler.prefab: added " + goName +
                          ", retired old uGUI content, removed the old ManagerToggler component.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // RemoveDeadComponents (Numpad/StatusIndicator/VersionIndicator cleanup)
        // ran once against palmmenu.prefab and NeRF's Viewer.prefab and was
        // removed here afterward -- it referenced those three types directly,
        // which stopped compiling the moment the now-dead scripts themselves
        // were deleted from disk. Nothing left to clean up; re-add a similar
        // method from source control history if another prefab surfaces one.

        [MenuItem("Tools/RSL/Create testMenuManager (Cameras)")]
        public static void CreateTestMenuManager()
        {
            Scene scene = SceneManager.GetActiveScene();
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsAsset);
            if (panelSettings == null) throw new Exception("missing " + PanelSettingsAsset);

            // Recreated from scratch each time: this is a test rig, and starting
            // clean is more useful than reconciling whatever the last run spawned
            // underneath it.
            GameObject existingRig = Find(scene, "testMenuManager");
            if (existingRig != null) Undo.DestroyObjectImmediate(existingRig);

            GameObject go = null;
            if (go == null)
            {
                go = new GameObject("testMenuManager");
                Undo.RegisterCreatedObjectUndo(go, "Create testMenuManager");
                // In front of the origin at 1:1 -- unlike the palmmenu placeholders
                // this is not inside a scaled-down canvas, so no x10 compensation.
                go.transform.position = new Vector3(0f, 0f, 1.2f);
            }

            ConfigureDocument(go, SensorMenuUxml, panelSettings);

            var template = go.GetComponent<MenuTemplate>() ?? go.AddComponent<MenuTemplate>();
            template.document = go.GetComponent<UIDocument>();
            template.defaultManagerUi = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SensorRowUxml);
            template.tagFilter = "Cameras";
            template.spawnFromAssets = true;
            // Spawned managers land under this object rather than the scene root,
            // so the rig stays self-contained and is deleted in one go.
            template.spawnParent = go.transform;

            var host = go.GetComponent<MenuListHost>() ?? go.AddComponent<MenuListHost>();
            host.template = template;

            EditorUtility.SetDirty(template);
            EditorUtility.SetDirty(host);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PalmmenuUiToolkitSwap] testMenuManager ready (tagFilter=Cameras). " +
                      "Press Setup Rows on it.");
        }

        // One palm slot: which uGUI object it takes over from, which menu it
        // opens, and the USS class carrying its icon.
        private struct PalmSlot
        {
            public string LegacyPath;
            public int MenuIndex;
            public string IconClass;
        }

        // Read off the ImageButtons' own UnityEvents. Mind the indices: they are
        // positions in MenuManager.menus, which is
        // [0] WifiMenu, [1] Cameras BaseMenu, [2] LidarMenu, [3] SettingsMenu --
        // NOT the left-to-right order the buttons sit in on the palm. CameraButton
        // is inside the SensorsManager prefab nested at CameraManagers, which is
        // why it is not beside the other three.
        private static readonly PalmSlot[] PalmSlots =
        {
            new PalmSlot { LegacyPath = "MenuCanvas/LidarManager/LidarButton",   MenuIndex = 2, IconClass = "tab-points" },
            new PalmSlot { LegacyPath = "MenuCanvas/Settings/SettingsButton",    MenuIndex = 3, IconClass = "tab-settings" },
            new PalmSlot { LegacyPath = "MenuCanvas/Wifi/WifiButton",            MenuIndex = 0, IconClass = "tab-wifi" },
            new PalmSlot { LegacyPath = "MenuCanvas/CameraManagers/CameraButton", MenuIndex = 1, IconClass = "tab-cameras" },
        };

        /// <summary>
        /// Replaces the strip of uGUI ImageButtons along the palm with one UI
        /// Toolkit document per slot.
        /// </summary>
        // ONE DOCUMENT PER BUTTON, not one bar holding all of them. The palm is
        // curved and the buttons fan across it -- 40 degrees up out of the palm,
        // then -50/-20/0/+20/+50 around -- and UI Toolkit has no per-element 3D
        // transform, so a single panel containing all five can only be flat and
        // the outer ones lift off the surface.
        //
        // Each replacement is therefore created as a SIBLING of the ImageButton it
        // replaces and given that button's own localPosition and localRotation. So
        // the fan is never written down here: it comes from the transforms that
        // already express it, and stays correct if someone moves a button.
        //
        // The uGUI originals are DEACTIVATED, not deleted, like every other step
        // here: the swap has to be reversible while both UIs are in flight.
        private static void WirePalmBar(GameObject palmmenu, PanelSettings panelSettings)
        {
            Transform canvas = palmmenu.transform.Find("MenuCanvas");
            if (canvas == null) throw new Exception("no MenuCanvas under palmmenu");

            foreach (PalmSlot slot in PalmSlots)
            {
                Transform legacy = palmmenu.transform.Find(slot.LegacyPath);
                if (legacy == null)
                {
                    Debug.LogWarning("[PalmmenuUiToolkitSwap] No " + slot.LegacyPath +
                                     " -- that palm button was not replaced.");
                    continue;
                }

                GameObject go = AdoptSlot(legacy, legacy.name + "Uxml", canvas, PalmTabUxml, panelSettings);
                var tab = go.GetComponent<PalmTabPanel>() ?? go.AddComponent<PalmTabPanel>();
                tab.menuIndex = slot.MenuIndex;
                tab.iconClass = slot.IconClass;
                EditorUtility.SetDirty(tab);

                SetActive(legacy, false);
            }

            // The status lamp in the centre of the strip, on StatusImg's own
            // transform (tilted 40 degrees with no fan).
            Transform status = palmmenu.transform.Find("MenuCanvas/StatusImg");
            if (status == null)
            {
                Debug.LogWarning("[PalmmenuUiToolkitSwap] No MenuCanvas/StatusImg -- " +
                                 "the palm strip has no status lamp.");
                return;
            }
            GameObject shell = AdoptSlot(status, "MenuShell", canvas, MenuShellUxml, panelSettings);
            if (shell.GetComponent<MenuShellPanel>() == null) shell.AddComponent<MenuShellPanel>();
            SetActive(status, false);
        }

        /// <summary>
        /// Creates (or reuses) a UI Toolkit document standing exactly where a uGUI
        /// object stands: same parent, same local position and rotation.
        /// </summary>
        private static GameObject AdoptSlot(Transform legacy, string name, Transform canvas,
                                            string uxml, PanelSettings panelSettings)
        {
            Transform parent = legacy.parent;
            Transform existing = parent.Find(name);
            GameObject go = existing != null ? existing.gameObject : new GameObject(name);
            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(go, "Add " + name);
                go.transform.SetParent(parent, false);
                go.layer = legacy.gameObject.layer;
            }
            go.transform.localPosition = legacy.localPosition;
            go.transform.localRotation = legacy.localRotation;
            go.transform.localScale = SlotScale(parent, canvas);

            ConfigureDocument(go, uxml, panelSettings);
            return go;
        }

        /// <summary>
        /// Scale that makes 100 authored pixels come out as 1 canvas unit x 10 --
        /// the same pixel-to-world ratio every other panel in this menu uses.
        /// </summary>
        // Computed from the parents rather than hard-coded, because the slots hang
        // off holders at different depths and scales: Wifi, Settings and
        // LidarManager sit at 0.1 under MenuCanvas (so 100 here), while the
        // sensor menus reach the same ratio through a different chain. Writing the
        // number in would silently be wrong the moment a holder is rescaled.
        private static Vector3 SlotScale(Transform parent, Transform canvas)
        {
            float target = canvas.lossyScale.x * 10f;
            float parentScale = parent.lossyScale.x;
            if (Mathf.Approximately(parentScale, 0f))
            {
                Debug.LogWarning("[PalmmenuUiToolkitSwap] '" + parent.name + "' has zero scale; " +
                                 "the palm slot under it will be sized wrong.");
                return Vector3.one;
            }
            return Vector3.one * (target / parentScale);
        }

        private static void WireMenuTemplate(GameObject palmmenu, string templatePath,
                                             string parentPath, string goName,
                                             Vector3 localPosition, PanelSettings panelSettings,
                                             string legacyPath, string tagFilterIfEmpty)
        {
            Transform templateHost = palmmenu.transform.Find(templatePath);
            if (templateHost == null) throw new Exception("no " + templatePath + " under palmmenu");
            var template = templateHost.GetComponent<MenuTemplate>();
            if (template == null) throw new Exception("no MenuTemplate on " + templatePath);

            Transform parent = palmmenu.transform.Find(parentPath);
            if (parent == null) throw new Exception("no " + parentPath + " under palmmenu");

            Transform existing = parent.Find(goName);
            GameObject go = existing != null ? existing.gameObject : new GameObject(goName);
            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(go, "Add " + goName);
                go.transform.SetParent(parent, false);
                go.layer = parent.gameObject.layer;
            }
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;
            // Same scale as the hand-placed placeholders.
            go.transform.localScale = Vector3.one * 10f;

            ConfigureDocument(go, SensorMenuUxml, panelSettings);

            // Grow UPWARD from a fixed bottom edge. These lists change height every
            // time Setup Rows runs, and with the default centre pivot the whole
            // panel shifts up and down around its middle as managers are added or
            // removed. Anchoring the bottom keeps it planted where it was placed.
            UIDocument listDoc = go.GetComponent<UIDocument>();
            listDoc.pivot = Pivot.BottomCenter;
            // Layout, not BoundingBox: the pivot should track the laid-out size,
            // which is what Dynamic sizing drives.
            listDoc.pivotReferenceSize = PivotReferenceSize.Layout;

            Undo.RecordObject(template, "Wire MenuTemplate");
            template.document = go.GetComponent<UIDocument>();
            // Managers that do not ship their own managerUi fall back to this.
            template.defaultManagerUi = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SensorRowUxml);
            // tagFilter is only filled in when it is EMPTY. The Settings and
            // Lidar templates already carry "System" and "Points"; overwriting
            // those would silently discard scene configuration.
            if (!string.IsNullOrEmpty(tagFilterIfEmpty) && string.IsNullOrEmpty(template.tagFilter))
            {
                template.tagFilter = tagFilterIfEmpty;
                Debug.Log("[PalmmenuUiToolkitSwap] " + templatePath +
                          " had no tagFilter; set to '" + tagFilterIfEmpty + "'.");
            }

            // The list document sits inside a submenu that starts closed, so
            // it has no visual tree when MenuTemplate.Start runs. This host
            // rebuilds the rows the moment the document is enabled.
            var host = go.GetComponent<MenuListHost>() ?? go.AddComponent<MenuListHost>();
            host.template = template;

            EditorUtility.SetDirty(template);
            EditorUtility.SetDirty(host);
            // A serialized change on a PREFAB INSTANCE component is only
            // recorded as an override when Unity is told to; SetDirty alone
            // does not always persist it.
            if (PrefabUtility.IsPartOfPrefabInstance(template))
                PrefabUtility.RecordPrefabInstancePropertyModifications(template);

            if (legacyPath != null) SetActive(palmmenu.transform.Find(legacyPath), false);
        }
        private static void SwapExisting(GameObject palmmenu, string placeholderPath, string legacyPath,
                                         string uxml, PanelSettings panelSettings, Action<GameObject> configure)
        {
            Transform placeholder = palmmenu.transform.Find(placeholderPath);
            if (placeholder == null) throw new Exception("no " + placeholderPath + " under palmmenu");

            ConfigureDocument(placeholder.gameObject, uxml, panelSettings);
            configure(placeholder.gameObject);
            SetActive(placeholder, true);

            // The old uGUI content is DEACTIVATED, never deleted -- the swap has to
            // be reversible while both UIs are in flight.
            SetActive(palmmenu.transform.Find(legacyPath), false);
        }

        private static void ConfigureDocument(GameObject go, string uxml, PanelSettings panelSettings)
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxml);
            if (asset == null) throw new Exception("missing " + uxml);

            UIDocument doc = go.GetComponent<UIDocument>();
            if (doc == null) doc = go.AddComponent<UIDocument>();
            doc.panelSettings = panelSettings;
            doc.visualTreeAsset = asset;
            // AddComponent<UIDocument> defaults to Fixed, which pins the panel at
            // worldSpaceSize (1920x1080) no matter what is in it -- that is why the
            // first sensor menus came out enormous while the hand-authored panels,
            // whose YAML already said 0 (= Dynamic), sized to their content.
            doc.worldSpaceSizeMode = UIDocument.WorldSpaceSizeMode.Dynamic;
        }

        private static void SetActive(Transform t, bool active)
        {
            if (t == null) return;
            Undo.RecordObject(t.gameObject, "SetActive");
            t.gameObject.SetActive(active);
        }

        private static GameObject Find(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name) return root;
            return null;
        }
    }
}
