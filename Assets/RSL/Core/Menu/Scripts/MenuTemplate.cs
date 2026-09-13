using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace RSL.Core.Menu
{
    #if UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(MenuTemplate))]
    public class MenuTemplateEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MenuTemplate myScript = (MenuTemplate)target;
            if (GUILayout.Button("Setup Rows"))
            {
                myScript.SetupRows();
            }
            if (GUILayout.Button("Toggle Menu"))
            {
                myScript.ToggleMenu();
            }
            if (GUILayout.Button("Clean"))
            {
                myScript.Clean();
            }
        }
    }
    #endif

    /// <summary>
    /// Builds a menu list of sensor managers, discovered by tag.
    ///
    /// Two things changed from the uGUI original. It used to RE-PARENT each
    /// manager's world-space uGUI panel into a menu transform and stack them by
    /// hand; now each manager owns a UXML row (SensorManager.managerUi) that gets
    /// attached as a child of the list, so the container does the layout. And it
    /// used to list only what was ALREADY in the scene; now Setup Rows discovers
    /// the manager PREFABS in the project whose tag matches and spawns any that
    /// are missing -- so the menu offers everything installed, not just whatever
    /// someone remembered to drag into the scene.
    ///
    /// Managers keep owning their own row markup, so a manager type that needs
    /// extra controls ships them and overrides SensorManager.BindUi.
    /// </summary>
    public class MenuTemplate : MonoBehaviour
    {
        [Tooltip("Shown/hidden by ToggleMenu -- the same GameObject the palm menu's " +
                 "buttons have always toggled. Not where rows go any more.")]
        public GameObject menu;

        [Tooltip("The UIDocument holding the menu list. Rows are added inside its " +
                 "rowsContainer element.")]
        public UIDocument document;

        [Tooltip("Name of the element inside the document that rows are added to.")]
        public string rowsContainer = "Rows";

        [Tooltip("Name of the Label used as the list header. Its text is the tag " +
                 "filter, so each list says which group it is showing.")]
        public string headerLabel = "Header";

        [Tooltip("Shown as the header when no tag filter is set.")]
        public string untaggedHeader = "Sensors";

        [Tooltip("Only managers with this tag are listed. Empty lists every manager.")]
        public string tagFilter = "";

        [Tooltip("Row markup for managers that do not specify their own managerUi.")]
        public VisualTreeAsset defaultManagerUi;

        [Tooltip("EDITOR ONLY: Setup Rows searches the project for manager prefabs " +
                 "carrying the matching tag and spawns any that are not in the scene " +
                 "yet. AssetDatabase does not exist in a build, so at runtime the " +
                 "list is built from whatever those spawns left behind in the scene.")]
        public bool spawnFromAssets = true;

        [Tooltip("Folders searched for manager prefabs.")]
        public string[] assetSearchFolders = { "Assets/RSL" };

        [Tooltip("Unity tag marking a prefab as a sensor manager. Set on each " +
                 "manager prefab's ROOT; prefab variants inherit it from their base.")]
        public string managerAssetTag = "sensorManager";

        [Tooltip("Parent for spawned manager instances. Defaults to this transform.")]
        public Transform spawnParent;

        [Tooltip("List managers whose GameObject is disabled, so they can be switched " +
                 "back on from the row.")]
        public bool includeHiddenManagers = true;

        [Tooltip("Optional button in the document that rebuilds the list at runtime.")]
        public string refreshButton = "Refresh";

        public SensorManager[] managers;

        // The refresh button is wired once, not on every SetupRows -- rebuilding
        // would otherwise stack a fresh handler each time. It lives outside the
        // rows container, so a rebuild never destroys it.
        private bool _refreshWired;

        private void Start()
        {
            // Skipped in silence when the document is not enabled yet: these lists
            // usually sit inside submenus that start closed, and an inactive
            // UIDocument has no visual tree. That is the NORMAL case, not a fault --
            // MenuListHost rebuilds the rows when the document comes up.
            if (document == null || !document.isActiveAndEnabled) return;
            SetupRows();
        }

        public void SetupRows()
        {
            // Application.isPlaying as well as the #if: UNITY_EDITOR is defined
            // in PLAY MODE too, so without this, opening a menu at runtime ran
            // the asset scan and instantiated prefabs into the running scene --
            // through PrefabUtility, registering undo entries mid-play. Asset
            // discovery is an authoring step; at runtime the list is built from
            // whatever those spawns already left in the scene.
            #if UNITY_EDITOR
            if (spawnFromAssets && !Application.isPlaying) SpawnMissingFromAssets();
            #endif

            VisualElement rows = ResolveRowsContainer();
            if (rows == null) return;

            rows.Clear();
            SetHeader();
            WireRefresh();

            managers = CollectManagers();
            foreach (SensorManager manager in managers)
            {
                VisualTreeAsset template = manager.managerUi != null ? manager.managerUi : defaultManagerUi;
                if (template == null)
                {
                    Debug.LogWarning($"[MenuTemplate] '{manager.name}' has no managerUi and no " +
                                     "defaultManagerUi is set -- skipping its row.", this);
                    continue;
                }

                VisualElement row = template.Instantiate();
                // Instantiate wraps the tree in a TemplateContainer that does not
                // inherit the row's own layout; let it fill the list width.
                row.style.flexGrow = 1;
                rows.Add(row);
                // The manager wires its own row, so a subclass can add controls
                // without this class knowing anything about them.
                manager.BindUi(row);
            }

            Debug.Log($"[MenuTemplate] Listed {managers.Length} manager(s)" +
                      (string.IsNullOrEmpty(tagFilter) ? "" : $" tagged '{tagFilter}'"), this);
        }

        /// <summary>Empties the list without rebuilding it.</summary>
        public void Clean()
        {
            VisualElement rows = ResolveRowsContainer();
            rows?.Clear();
        }

        public void ToggleMenu()
        {
            if (menu == null) return;
            menu.SetActive(!menu.activeSelf);
        }

        /// <summary>
        /// "PointCloud" -> "Point Cloud". Splits camelCase/PascalCase runs so a tag
        /// reads as a title, leaving names that are already spaced untouched.
        /// </summary>
        public static string Humanize(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;

            var sb = new StringBuilder(raw.Length + 4);
            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];
                bool boundary = i > 0
                                && char.IsUpper(c)
                                && !char.IsWhiteSpace(raw[i - 1])
                                // Splits "Point|Cloud", but keeps an acronym whole:
                                // only break inside a run of capitals when the NEXT
                                // char is lower, i.e. "TF|Settings" not "T|F".
                                && (!char.IsUpper(raw[i - 1]) || (i + 1 < raw.Length && char.IsLower(raw[i + 1])));
                if (boundary) sb.Append(' ');
                sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Drops Unity's " Variant" suffix so a variant and its base compare and
        /// display as the same manager.
        /// </summary>
        // "PanoViewManager Variant" -> "PanoViewManager". Unity appends this when
        // a variant asset is created; it says how the prefab was MADE, not what
        // the manager is, so it has no business in a hierarchy full of managers
        // that were authored the other way. Cosmetic only -- the row label comes
        // from SensorManager.name, and so does the duplicate check.
        public static string NormalizeName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;
            const string suffix = " Variant";
            // A loop, because nesting variants stacks the suffix
            // ("X Variant Variant"), and Unity's uniquifier can append a counter.
            string name = raw.Trim();
            while (true)
            {
                // Strip any trailing " (1)"-style counter first so the suffix
                // underneath it is still visible to the check.
                int open = name.LastIndexOf(" (", StringComparison.Ordinal);
                if (open > 0 && name.EndsWith(")", StringComparison.Ordinal)
                             && int.TryParse(name.Substring(open + 2, name.Length - open - 3), out _))
                {
                    name = name.Substring(0, open);
                    continue;
                }
                if (name.EndsWith(suffix, StringComparison.Ordinal))
                {
                    name = name.Substring(0, name.Length - suffix.Length).TrimEnd();
                    continue;
                }
                return name;
            }
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Finds manager prefabs in the project whose tag matches, and instantiates
        /// any that are not in the scene yet.
        /// </summary>
        private void SpawnMissingFromAssets()
        {
            Transform parent = spawnParent != null ? spawnParent : transform;

            // Dedup by the manager's OWN name, not by prefab reference.
            //
            // Prefab identity is a trap here, because neither accessor covers both
            // shapes present in this project:
            //   GetCorrespondingObjectFromOriginalSource walks a variant chain to
            //     its root, so an instance of "StereoViewManager Variant" reports
            //     itself as ImageViewManager -- the variant looks absent and gets
            //     spawned again (measured: 5 listed for 4 prefabs).
            //   GetCorrespondingObjectFromSource handles variants, but for a NESTED
            //     instance it returns the object inside the ENCLOSING prefab
            //     (palmmenu.prefab), never the manager asset -- so every manager
            //     already nested in palmmenu looks absent (measured: 4 spawned).
            //
            // SensorManager.name is what the menu keys on anyway: rows show it and
            // sorting uses it. Two managers with the same name are a duplicate by
            // definition, however they were instantiated.
            //
            // BEWARE which "name" this is. SensorManager declares its own
            //     public string name = "DEFAULT";
            // which SHADOWS Component.name, so `manager.name` is the DISPLAY name
            // the rows show ("Grid Map", "Markers", "ImageView") and has nothing to
            // do with the GameObject or the asset file ("GridMapManager.prefab").
            // Both sides of this comparison must therefore come off the COMPONENT.
            // Keying it off the asset filename instead spawned a second copy of
            // every manager already in palmmenu, since none of the two ever match.
            var present = new HashSet<string>();
            foreach (SensorManager existing in FindObjectsByType<SensorManager>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (existing != null && !string.IsNullOrEmpty(existing.name)) present.Add(existing.name);
            }
            int spawned = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", assetSearchFolders))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                // The Unity TAG is the marker for "this asset is a sensor manager",
                // so the search does not have to load and inspect every prefab in
                // the project. Cheap to check and visible in the inspector.
                if (!string.IsNullOrEmpty(managerAssetTag) && !prefab.CompareTag(managerAssetTag)) continue;

                // Still require the component: the tag says what the asset IS, the
                // component is what the menu actually drives. GetComponent on the
                // abstract base picks up every concrete subclass (CameraManager,
                // LidarManager, ...) without naming them.
                var manager = prefab.GetComponent<SensorManager>();
                if (manager == null)
                {
                    Debug.LogWarning($"[MenuTemplate] '{path}' is tagged " +
                                     $"'{managerAssetTag}' but has no SensorManager.", this);
                    continue;
                }
                // The tag lives on the prefab asset. A prefab VARIANT inherits it
                // from its base unless overridden -- that is how PanoView and
                // StereoView are "Cameras" without restating it.
                if (!string.IsNullOrEmpty(tagFilter) && manager.tag != tagFilter) continue;
                if (present.Contains(manager.name)) continue;

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                // NormalizeName, not prefab.name: the object shows up in the
                // hierarchy as "PanoViewManager", not "PanoViewManager Variant".
                // Purely cosmetic -- nothing keys on the GameObject name -- but
                // "Variant" is prefab-authoring bookkeeping that only looks like a
                // stray duplicate sitting next to the managers it belongs with.
                instance.name = NormalizeName(prefab.name);
                Undo.RegisterCreatedObjectUndo(instance, "Spawn " + instance.name);
                present.Add(manager.name);
                spawned++;
            }

            if (spawned > 0)
                Debug.Log($"[MenuTemplate] Spawned {spawned} manager prefab(s) tagged " +
                          $"'{tagFilter}' from the project into '{parent.name}'.", this);
        }
        #endif

        private SensorManager[] CollectManagers()
        {
            SensorManager[] found = FindObjectsByType<SensorManager>(
                includeHiddenManagers ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            var matching = new List<SensorManager>();
            foreach (SensorManager manager in found)
            {
                if (manager == null) continue;
                if (!string.IsNullOrEmpty(tagFilter) && manager.tag != tagFilter) continue;
                matching.Add(manager);
            }

            // SensorManager.CompareTo sorts by name -- the same comparison the uGUI
            // version used. It then REVERSED the array, because it stacked rows
            // upward from a baseline and the reversal is what made the list read
            // A-to-Z top-to-bottom. A UI Toolkit container already flows downward,
            // so reversing here would invert the familiar order.
            matching.Sort();
            return matching.ToArray();
        }

        private void WireRefresh()
        {
            if (_refreshWired || string.IsNullOrEmpty(refreshButton)) return;
            Button button = document.rootVisualElement.Q<Button>(refreshButton);
            if (button == null) return;
            button.clicked += SetupRows;
            _refreshWired = true;
        }

        private void SetHeader()
        {
            VisualElement root = document != null ? document.rootVisualElement : null;
            Label header = root?.Q<Label>(headerLabel);
            if (header == null) return;

            header.text = Humanize(string.IsNullOrEmpty(tagFilter) ? untaggedHeader : tagFilter);
            // The header is the panel's title, so it carries h1 rather than being
            // styled per-row. AddToClassList is idempotent.
            header.AddToClassList("h1");
        }

        private VisualElement ResolveRowsContainer()
        {
            if (document == null)
            {
                Debug.LogWarning("[MenuTemplate] No UIDocument assigned -- nothing to build into.", this);
                return null;
            }

            VisualElement root = document.rootVisualElement;
            if (root == null)
            {
                Debug.LogWarning("[MenuTemplate] The UIDocument has no visual tree yet " +
                                 "(is it enabled, with a source asset?).", this);
                return null;
            }

            VisualElement rows = root.Q(rowsContainer);
            if (rows == null)
                Debug.LogWarning($"[MenuTemplate] No element named '{rowsContainer}' in " +
                                 $"{document.visualTreeAsset?.name}.", this);
            return rows;
        }
    }
}
