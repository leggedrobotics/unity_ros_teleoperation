using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RSL.Core.Menu
{
    // Deprecated: uGUI sprite-swap toggle from before the UI Toolkit
    // migration. New toggle-style buttons should use a UI Toolkit Button
    // with an "is-on"/"is-active"-style USS class instead (see
    // MenuShellPanel's "is-connected"/"is-stagnant" or ImageViewerPanel's
    // "is-open" for the established pattern). Not deleted: still driving
    // whatever uGUI prefabs haven't been ported yet.
    [System.Obsolete("uGUI-only toggle button, superseded by UI Toolkit's class-based state pattern (see MenuShellPanel/ImageViewerPanel). Do not use in new code.")]
    public class ToggleButton : MonoBehaviour
    {
        // Start is called before the first frame update
        public Sprite activeSprite;
        public Sprite inactiveSprite;
        public GameObject icon;

        void Start()
        {
            icon.GetComponent<UnityEngine.UI.Image>().sprite = inactiveSprite;
        }
        
        public void setActiveSprite()
        {
            icon.GetComponent<UnityEngine.UI.Image>().sprite = activeSprite;
        }

        public void setInactiveSprite()
        {
            icon.GetComponent<UnityEngine.UI.Image>().sprite = inactiveSprite;
        }
    }
}
