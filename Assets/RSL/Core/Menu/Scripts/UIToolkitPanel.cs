// Shared plumbing for the UI Toolkit panels replacing the uGUI palmmenu.
//
// Two things every panel in this migration needs, and neither is free:
//
//  * Element lookups that FAIL LOUDLY. Panels null-check every control so one
//    missing element cannot take the rest down -- which means a element renamed
//    in the UI Builder would otherwise leave a control that looks correct and
//    silently does nothing. Require<T> turns that into a warning naming the
//    element and the asset.
//
//  * Sections that DEGRADE when their backing manager is absent. These panels
//    are developed in bare test scenes (MenuTestScene has no managers at all)
//    and run in Main.unity where everything exists. BindSection dims and
//    disables rather than hides, so the layout you see in the Builder is the
//    layout you get either way.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RSL.Core.Menu
{
    [RequireComponent(typeof(UIDocument))]
    public abstract class UIToolkitPanel : MonoBehaviour
    {
        protected UIDocument Document { get; private set; }

        [Tooltip("Log one line per bind saying which sections gated themselves off. " +
                 "On by default: during the migration it is the only positive signal " +
                 "that a panel bound at all, since every control is null-tolerant and " +
                 "a panel wired to nothing looks identical to a working one.")]
        public bool logBindSummary = true;

        private readonly List<string> _gatedOff = new List<string>();

        protected virtual void OnEnable()
        {
            Document = GetComponent<UIDocument>();

            // rootVisualElement only exists once the UIDocument is enabled, and
            // is null when the source asset is unassigned or failed to load.
            VisualElement root = Document.rootVisualElement;
            if (root == null)
            {
                Debug.LogError($"[{GetType().Name}] UIDocument has no visual tree -- " +
                               "is a source asset assigned?", this);
                enabled = false;
                return;
            }

            _gatedOff.Clear();
            Bind(root);

            if (logBindSummary)
            {
                string asset = Document.visualTreeAsset != null ? Document.visualTreeAsset.name : "<no asset>";
                string gated = _gatedOff.Count == 0 ? "none" : string.Join(", ", _gatedOff);
                Debug.Log($"[{GetType().Name}] bound {asset}; sections unavailable: {gated}", this);
            }
        }

        protected abstract void Bind(VisualElement root);

        /// <summary>
        /// Queries a named element, warning if the UXML no longer contains it.
        /// Returns null in that case -- callers stay null-tolerant.
        /// </summary>
        protected T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            var element = root.Q<T>(name);
            if (element == null)
                Debug.LogWarning($"[{GetType().Name}] '{name}' ({typeof(T).Name}) not found in " +
                                 $"{Document.visualTreeAsset?.name} -- that control will do nothing.", this);
            return element;
        }

        /// <summary>
        /// Dims and disables <paramref name="rows"/> when their backing system is
        /// missing. Returns <paramref name="available"/>, so callers read as
        /// "bind only if the system is actually there".
        /// </summary>
        protected bool BindSection(bool available, params VisualElement[] rows)
        {
            foreach (VisualElement row in rows)
            {
                if (row == null) continue;
                row.EnableInClassList("unavailable", !available);
                row.SetEnabled(available);
                if (!available && !string.IsNullOrEmpty(row.name)) _gatedOff.Add(row.name);
            }
            return available;
        }
    }
}
