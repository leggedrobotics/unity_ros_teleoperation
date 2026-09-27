// UI Toolkit controller for SensorMarkerViewer.uxml.
//
// Topic, Refresh and Clear come from SensorViewerPanel; the size slider is the
// only control a marker viewer adds.
//
// No count: MarkerStream keeps none. The uGUI panel had a Count label, but
// nothing in the script ever wrote to it.
using UnityEngine;
using UnityEngine.UIElements;
using RSL.Core.Menu;

namespace RSL.Sensors.Markers
{
    public class MarkerViewerPanel : SensorViewerPanel
    {
        private MarkerStream Stream => stream as MarkerStream;

        protected override void BindExtra(VisualElement root)
        {
            MarkerStream s = Stream;
            if (s == null) return;

            var size = Require<Slider>(root, "SizeSlider");
            if (size == null) return;

            size.SetValueWithoutNotify(Mathf.Clamp(s.pointSize, size.lowValue, size.highValue));
            size.RegisterValueChangedCallback(evt => s.OnSizeChange(evt.newValue));
        }
    }
}
