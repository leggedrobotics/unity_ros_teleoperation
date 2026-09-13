// Rebuilds a MenuTemplate's list whenever the document holding it becomes
// active.
//
// WHY THIS EXISTS: the sensor lists live inside submenus that start CLOSED
// (MenuManager disables every menu in Awake), and a UIDocument on an inactive
// GameObject has no rootVisualElement at all. So MenuTemplate.Start cannot
// build the rows -- it runs while there is nothing to build into, and the list
// stays empty for the rest of the session.
//
// MenuTemplate itself cannot solve this: it lives on the object the palm menu's
// buttons toggle (their UnityEvents target its ToggleMenu), not on the document.
// This sits on the document instead, where OnEnable fires at exactly the moment
// the visual tree exists.
using UnityEngine;
using UnityEngine.UIElements;

namespace RSL.Core.Menu
{
    [RequireComponent(typeof(UIDocument))]
    public class MenuListHost : MonoBehaviour
    {
        [Tooltip("The template that owns this list. It is asked to rebuild each " +
                 "time this document is enabled.")]
        public MenuTemplate template;

        private void OnEnable()
        {
            if (template == null)
            {
                Debug.LogWarning("[MenuListHost] No MenuTemplate assigned -- this list " +
                                 "will never be populated.", this);
                return;
            }
            // Rebuild rather than build-once: managers can be added, removed or
            // hidden while the menu is closed, so what was built last time may
            // no longer match the scene.
            template.SetupRows();
        }
    }
}
