// UI Toolkit controller for SensorGridMapViewer.uxml.
//
// Topic, Refresh and Clear come from SensorViewerPanel. The rest is what makes
// a grid map a grid map: an opacity slider and the two layer dropdowns.
//
// The layer dropdowns are populated from the stream's own layer list rather
// than hardcoded here, because a grid map advertises its layers at runtime and
// they differ per robot. OnColorChange/OnHeightChange take the INDEX into that
// list, which is exactly the dropdown's index.
using System.Collections.Generic;
using UnityEngine.UIElements;
using RSL.Core.Menu;

namespace RSL.Sensors.GridMap
{
    public class GridMapViewerPanel : SensorViewerPanel
    {
        private GridMapStream Stream => stream as GridMapStream;

        protected override void BindExtra(VisualElement root)
        {
            GridMapStream s = Stream;
            if (s == null) return;

            var opacity = Require<Slider>(root, "OpacitySlider");
            if (opacity != null)
            {
                opacity.SetValueWithoutNotify(
                    UnityEngine.Mathf.Clamp(s.opacity, opacity.lowValue, opacity.highValue));
                opacity.RegisterValueChangedCallback(evt => s.OnOpacityChange(evt.newValue));
            }

            // No Track binding: SensorGridMapViewer.uxml has no Track button.
            // See its own header comment for why (the uGUI original's onClick
            // was a dangling reference to a different stream type, and
            // GridMapStream.ToggleTrack is unimplemented).

            BindLayers(Require<DropdownField>(root, "HeightDropdown"), s.OnHeightChange);
            BindLayers(Require<DropdownField>(root, "ColorDropdown"), s.OnColorChange);
        }

        // Layers are not known until a grid map message arrives, and can change
        // when the robot does, so the choices are refreshed from the stream
        // rather than set once at bind time.
        private void BindLayers(DropdownField field, System.Action<int> onSelect)
        {
            if (field == null) return;
            field.choices = new List<string>();
            field.RegisterValueChangedCallback(_ =>
            {
                if (field.index >= 0) onSelect(field.index);
            });
            _layerFields.Add(field);
        }

        private readonly List<DropdownField> _layerFields = new List<DropdownField>();
        private int _knownLayers = -1;

        protected override void Update()
        {
            base.Update();

            GridMapStream s = Stream;
            string[] layers = s != null ? s.Layers : null;
            int count = layers != null ? layers.Length : 0;
            // Only rebuild when the set actually changes: reassigning choices
            // resets the selection, so doing it every frame would fight the user.
            if (count == _knownLayers) return;
            _knownLayers = count;

            foreach (DropdownField field in _layerFields)
            {
                string selected = field.value;
                field.choices = count > 0 ? new List<string>(layers) : new List<string>();
                if (count > 0)
                    field.SetValueWithoutNotify(
                        System.Array.IndexOf(layers, selected) >= 0 ? selected : layers[0]);
            }
        }
    }
}
