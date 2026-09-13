// UI Toolkit controller for SensorPoseViewer.uxml.
//
// Topic, Refresh and Clear come from SensorViewerPanel; an arrow size slider is
// the only control a pose viewer adds.
//
// No count: a stamped pose is one pose, and the uGUI panel had no count either.
using UnityEngine;
using UnityEngine.UIElements;
using RSL.Core.Menu;

namespace RSL.Sensors.Pose
{
    public class PoseViewerPanel : SensorViewerPanel
    {
        private StampedPoseStream Stream => stream as StampedPoseStream;

        protected override void BindExtra(VisualElement root)
        {
            StampedPoseStream s = Stream;
            if (s == null) return;

            var size = Require<Slider>(root, "SizeSlider");
            if (size == null) return;

            size.SetValueWithoutNotify(Mathf.Clamp(s.scale, size.lowValue, size.highValue));
            size.RegisterValueChangedCallback(evt => s.OnSizeUpdate(evt.newValue));
        }
    }
}
