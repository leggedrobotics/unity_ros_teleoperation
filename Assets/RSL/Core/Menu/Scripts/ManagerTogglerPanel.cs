// UI Toolkit port of ManagerToggler (Assets/RSL/Core/Menu/Scripts/ManagerToggler.cs,
// now removed) -- a debug-only list of every SensorManager in the scene, each
// with a toggle to turn it on/off. Confirmed debug-tooling, so ported directly
// rather than left on uGUI.
//
// Rows are built in code, not from a per-row UXML template: a label plus one
// icon-button is too small a shape to be worth its own .uxml file, and every
// other per-row template in this migration (WifiEntry, SensorRow*) exists
// because its row has several controls, not one.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using RSL.Core;

namespace RSL.Core.Menu
{
    public class ManagerTogglerPanel : UIToolkitPanel
    {
        private VisualElement _rows;
        private SensorManager[] _managers;
        private MenuTemplate[] _menuTemplates;

        protected override void Bind(VisualElement root)
        {
            _rows = Require<ScrollView>(root, "Rows");
            Refresh();
        }

        // Public: the same "Setup + PopulateMenu" combo ManagerToggler.Start
        // used to run, callable again later the same way the old inspector's
        // "Setup"/"Populate Menu" buttons did.
        public void Refresh()
        {
            _managers = FindObjectsByType<SensorManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            _menuTemplates = FindObjectsByType<MenuTemplate>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            System.Array.Sort(_managers, (x, y) => x.name.CompareTo(y.name));

            if (_rows == null) return;
            _rows.Clear();
            foreach (SensorManager manager in _managers)
                _rows.Add(BuildRow(manager));
        }

        private VisualElement BuildRow(SensorManager manager)
        {
            var row = new VisualElement();
            row.AddToClassList("row");

            var label = new Label(manager.name);
            label.AddToClassList("managerLabel");
            row.Add(label);

            var toggle = new Button();
            toggle.AddToClassList("icon-button");
            toggle.AddToClassList("toggle-icon");
            toggle.AddToClassList("manager-toggle");
            toggle.tooltip = "Enable or disable this manager";
            toggle.EnableInClassList("is-on", manager.gameObject.activeSelf);
            toggle.clicked += () => OnToggle(manager, toggle);
            row.Add(toggle);

            return row;
        }

        private void OnToggle(SensorManager manager, Button toggle)
        {
            bool wasActive = manager.gameObject.activeSelf;
            manager.ClearAll();
            manager.gameObject.SetActive(!wasActive);
            toggle.EnableInClassList("is-on", !wasActive);

            foreach (var menuTemplate in _menuTemplates)
                menuTemplate.SetupRows();
        }
    }
}
