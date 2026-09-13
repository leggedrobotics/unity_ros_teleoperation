// UI Toolkit controller for SensorPathViewer.uxml.
//
// Topic, Refresh and Clear come from SensorViewerPanel. A path adds two
// sliders -- arrow size and line width -- and is the one non-lidar viewer that
// actually maintains a point count.
using UnityEngine;
using UnityEngine.UIElements;
using RSL.Core.Menu;

namespace RSL.Sensors.Path
{
    public class PathViewerPanel : SensorViewerPanel
    {
        private PathStream Stream => stream as PathStream;

        protected override string CountText
        {
            get
            {
                PathStream s = Stream;
                return s != null ? s.PointCount.ToString() : null;
            }
        }

        protected override void BindExtra(VisualElement root)
        {
            PathStream s = Stream;
            if (s == null) return;

            var size = Require<Slider>(root, "SizeSlider");
            if (size != null)
            {
                size.SetValueWithoutNotify(Mathf.Clamp(s.scale, size.lowValue, size.highValue));
                size.RegisterValueChangedCallback(evt => s.OnSizeUpdate(evt.newValue));
            }

            var width = Require<Slider>(root, "WidthSlider");
            if (width != null)
            {
                width.SetValueWithoutNotify(Mathf.Clamp(s.lineWidth, width.lowValue, width.highValue));
                width.RegisterValueChangedCallback(evt => s.OnWidthUpdate(evt.newValue));
            }
        }
    }
}
