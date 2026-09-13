// One button on the palm strip: the UI Toolkit port of a single palmmenu
// ImageButton (WifiButton, SettingsButton, LidarButton, CameraButton).
//
// WHY ONE COMPONENT PER BUTTON rather than one bar holding all of them: the
// palm is curved and the buttons fan across it, each with its own rotation
// (40 degrees up out of the palm, then -50/-20/0/+20/+50 around). UI Toolkit
// has no per-element 3D transform, so any single panel containing all five is
// necessarily flat and the outer buttons lift off the surface. Each of these
// instead lives where its ImageButton lived and inherits that holder's rotation
// from the transform hierarchy, so the fan is never restated anywhere.
//
// IT CALLS MenuManager.ToggleMenu(int) -- the very method the ImageButton's
// UnityEvent invoked, with the very same index. The open/close-on-reselect
// behaviour is not reimplemented here: MenuManager owns it, and it is also
// driven from its own inspector and from joystick bindings, so a second owner
// would drift. The highlight is read back from MenuManager.menus for the same
// reason.
using UnityEngine;
using UnityEngine.UIElements;

namespace RSL.Core.Menu
{
    // ExecuteAlways because this is the ONE panel whose appearance is authored
    // per instance. Every other panel gets its whole look from markup and USS,
    // so it renders correctly in the scene view even though UIToolkitPanel only
    // binds in Play mode. Here the icon comes from a class Bind adds, so without
    // this all four palm tabs are blank blue tiles in the editor and there is no
    // way to see the strip you are positioning.
    //
    // Safe to run in edit mode: Bind looks a manager up, adds a class and wires a
    // click, and Update compares one bool. Nothing here writes to an asset.
    [ExecuteAlways]
    public class PalmTabPanel : UIToolkitPanel
    {
        [Tooltip("Index into MenuManager.menus -- the same int the uGUI " +
                 "ImageButton this replaces passed to ToggleMenu. palmmenu's " +
                 "wiring is 0 Wifi, 1 Cameras, 2 Points, 3 Settings.")]
        public int menuIndex;

        [Tooltip("USS class supplying the icon: tab-points, tab-settings, " +
                 "tab-wifi or tab-cameras. A class rather than a sprite field so " +
                 "one PalmTab.uxml serves every slot and a retheme can swap the " +
                 "whole icon set in the stylesheet.")]
        public string iconClass;

        private MenuManager _menuManager;
        private Button _button;
        private bool _wasOpen;

        protected override void Bind(VisualElement root)
        {
            _menuManager = FindFirstObjectByType<MenuManager>();

            _button = Require<Button>(root, "Tab");
            if (_button == null) return;

            if (!string.IsNullOrEmpty(iconClass)) _button.AddToClassList(iconClass);

            _button.clicked += Toggle;
            // Nothing to open without a MenuManager, so show the tab as inert
            // rather than let it look live. MenuTestScene has one; a scene that
            // does not gets a visibly dead strip instead of a silent no-op.
            _button.SetEnabled(InRange);
            Refresh();

            // Screen-space (2D/desktop) layout only: this class's own header
            // comment explains why there is no shared bar CSS could position --
            // in world space each tab gets its screen position for free, from
            // its own GameObject's 3D transform (part of the palm's curved
            // fan), which Screen Space Overlay ignores entirely. Every tab
            // then defaults to the document's own (0,0) origin, stacking on
            // top of each other and the status shell. Screen space gets an
            // explicit bottom row instead, ordered the same way the fan does
            // in world space (0 Wifi ... 3 Settings) -- inline style, not USS,
            // so the shared main.uss stays untouched and this cannot regress
            // the world-space original.
            if (IsScreenSpace)
            {
                // Absolute on Tab alone measured against SLOT, not the
                // screen: Slot's default position:Relative (VisualElement's
                // own default) makes it a positioning root for its own
                // children regardless of its size, and .palm-slot fixes that
                // size at one tile (var(--row-content), 54px) -- so
                // "bottom: 20" landed 20px above SLOT's own 54px box, not the
                // screen's (measured: worldBound.y came out negative).
                // root itself is the actual full-screen panel content and is
                // NOT position:Relative-constrained the same way, so it is
                // Slot that needs to move, sized to just its content so the
                // absolute offset means what it says.
                VisualElement slot = root.Q<VisualElement>("Slot") ?? root;
                slot.style.position = Position.Absolute;
                slot.style.bottom = 20;
                slot.style.left = 468 + menuIndex * 70;
            }
        }

        // PanelSettings.renderMode isn't accessible from user code in this
        // Unity version (PanelRenderMode is internal here), so this goes by
        // the known asset name instead -- ScreenPanelSettings vs
        // WorldPanelSettings, the only two PanelSettings assets this project
        // has (see PalmMenu2DVariantTool.cs).
        private bool IsScreenSpace =>
            Document != null && Document.panelSettings != null &&
            Document.panelSettings.name == "ScreenPanelSettings";

        /// <summary>
        /// Opens this menu, or closes it if it is already open -- MenuManager's
        /// own semantics, because this IS MenuManager doing it.
        /// </summary>
        public void Toggle()
        {
            if (!InRange) return;
            _menuManager.ToggleMenu(menuIndex);
            Refresh();
        }

        // Polled rather than event-driven because MenuManager raises nothing when
        // a menu opens: another tab's press, the inspector buttons and the
        // joystick bindings all change it silently. One bool compare per frame,
        // and EnableInClassList only touches the element when it actually flips.
        private void Update()
        {
            if (_button == null) return;
            bool open = IsOpen;
            if (open == _wasOpen) return;
            _wasOpen = open;
            _button.EnableInClassList("is-active", open);
        }

        private void Refresh()
        {
            _wasOpen = IsOpen;
            _button?.EnableInClassList("is-active", _wasOpen);
        }

        private bool InRange =>
            _menuManager != null && _menuManager.menus != null
            && menuIndex >= 0 && menuIndex < _menuManager.menus.Length
            && _menuManager.menus[menuIndex] != null;

        private bool IsOpen => InRange && _menuManager.menus[menuIndex].activeSelf;
    }
}
