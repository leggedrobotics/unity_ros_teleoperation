// UI Toolkit controller for SensorLidarViewer.uxml -- the port of a spawned
// point cloud viewer's uGUI TopMenu.
//
// Every control routes through the LidarStream methods the uGUI panel's
// UnityEvents called, so both UIs drive the same code while the migration is in
// flight. Two of those methods used to bounds-check against their own uGUI
// dropdown's option count; they check LidarStream's name arrays now, which is
// what lets this panel call them with the widget gone.
//
// Topic is the exception: OnTopicSelect(int) reads the caption back out of the
// uGUI dropdown, so this calls OnTopicChange(string) directly -- the method
// OnTopicSelect itself ends up calling.
//
// The title bar is not ported: it carries UIPositionChanger over a BoxCollider
// and is how the viewer is grabbed and moved in the world.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using RSL.Core.Menu;

namespace RSL.Sensors.Lidar
{
    public class LidarViewerPanel : UIToolkitPanel
    {
        [Tooltip("The stream this panel drives. Assigned on the prefab, since a " +
                 "spawned viewer has exactly one and a scene search would find " +
                 "every other viewer's as well.")]
        public LidarStream stream;

        private DropdownField _topic;
        private Label _count;
        private Button _enable;
        private Button _track;
        private List<string> _topics = new List<string> { "None" };

        protected override void Bind(VisualElement root)
        {
            // The panel is a child of the viewer, so the stream is up the
            // hierarchy -- fall back to that rather than requiring the
            // serialized reference to survive every prefab edit.
            if (stream == null) stream = GetComponentInParent<LidarStream>();

            _topic = Require<DropdownField>(root, "TopicDropdown");
            _count = Require<Label>(root, "Count");
            _enable = Require<Button>(root, "Enabled");
            _track = Require<Button>(root, "Track");
            var density = Require<Slider>(root, "DensitySlider");
            var size = Require<Slider>(root, "SizeSlider");
            var vizType = Require<DropdownField>(root, "VizTypeDropdown");
            var colorMode = Require<DropdownField>(root, "ColorModeDropdown");
            var refresh = Require<Button>(root, "Refresh");
            var clear = Require<Button>(root, "Clear");

            if (!BindSection(stream != null, root)) return;

            BindTopic(refresh);
            BindEnumDropdown(vizType, LidarStream.VizTypeNames, (int)stream.vizType, stream.OnVizTypeSelect);
            BindEnumDropdown(colorMode, LidarStream.ColorModeNames, (int)stream.colorMode, stream.OnColorSelect);

            if (density != null)
            {
                // displayPts/maxPts is the fraction OnDensityChange multiplies
                // back up, so it is the value that round-trips.
                density.SetValueWithoutNotify(
                    Mathf.Clamp(stream.maxPts > 0 ? (float)stream.displayPts / stream.maxPts : 0f,
                                density.lowValue, density.highValue));
                density.RegisterValueChangedCallback(evt => stream.OnDensityChange(evt.newValue));
            }

            if (size != null)
            {
                // OnSizeChange divides by 10 on the way in, so multiply on the
                // way out to show the number the user last chose.
                size.SetValueWithoutNotify(Mathf.Clamp(stream.scale * 10f, size.lowValue, size.highValue));
                size.RegisterValueChangedCallback(evt => stream.OnSizeChange(evt.newValue));
            }

            if (_enable != null)
            {
                _enable.clicked += () =>
                {
                    stream.ToggleEnabled();
                    RefreshEnabled();
                };
                RefreshEnabled();
            }

            if (clear != null) clear.clicked += () => stream.Clear();

            // Cycles LidarStream.useTF, the uGUI original's own "Checkbox
            // Variant" (onStateChanged -> ToggleTrack) -- IncrementTrack is
            // that same click-cycles-state behaviour, just called directly
            // instead of through a StateButton's own index bookkeeping.
            if (_track != null)
            {
                _track.clicked += () =>
                {
                    stream.IncrementTrack();
                    RefreshTrack();
                };
                RefreshTrack();
            }
        }

        private void OnDisable()
        {
            if (stream != null) stream.TopicsChanged -= OnTopicsChanged;
        }

        // ---- Topic ----------------------------------------------------------

        private void BindTopic(Button refresh)
        {
            if (refresh != null) refresh.clicked += () => stream.RefreshTopics();

            // Subscribed rather than polled: the list arrives asynchronously from
            // a ROS round trip, long after this panel has bound.
            stream.TopicsChanged -= OnTopicsChanged;
            stream.TopicsChanged += OnTopicsChanged;

            if (_topic == null) return;
            ApplyTopics(_topics);

            _topic.RegisterValueChangedCallback(evt =>
            {
                // OnTopicChange, not OnTopicSelect(int): the int overload reads
                // the caption back out of the uGUI dropdown this replaces.
                // "None" is the list's own placeholder and means unsubscribe,
                // which OnTopicChange spells as null.
                string chosen = evt.newValue;
                stream.OnTopicChange(chosen == "None" ? null : chosen);
            });

            // The stream may already be subscribed (a viewer restored from a
            // saved layout), so ask for the live list to fill the dropdown.
            stream.RefreshTopics();
        }

        private void OnTopicsChanged(List<string> topics)
        {
            _topics = topics;
            ApplyTopics(topics);
        }

        private void ApplyTopics(List<string> topics)
        {
            if (_topic == null) return;
            _topic.choices = new List<string>(topics);
            // Without notify: this is reporting what the stream is already on,
            // and notifying would re-subscribe on every refresh.
            string current = string.IsNullOrEmpty(stream.topicName) ? "None" : stream.topicName;
            _topic.SetValueWithoutNotify(topics.Contains(current) ? current : "None");
        }

        // ---- Enum dropdowns -------------------------------------------------

        private static void BindEnumDropdown(DropdownField field, string[] names, int current,
                                             System.Action<int> onSelect)
        {
            if (field == null) return;
            field.choices = new List<string>(names);
            field.SetValueWithoutNotify(names[Mathf.Clamp(current, 0, names.Length - 1)]);
            field.RegisterValueChangedCallback(_ =>
            {
                if (field.index >= 0) onSelect(field.index);
            });
        }

        // ---- Live state -----------------------------------------------------

        private void RefreshEnabled()
        {
            _enable?.EnableInClassList("is-on", stream._enabled);
        }

        // Only two of .track-button's three states apply here: base
        // (free-floating, axis-arrow) and is-track-2 (locked to the TF
        // frame, axis-arrow-lock) -- Lidar has no head-locked mode.
        private void RefreshTrack()
        {
            _track?.EnableInClassList("is-track-2", stream.useTF);
        }

        // The point count is pushed by the stream into a TMP label it owns, with
        // no event to hang off, so this mirrors it. Cheap: two field reads and a
        // string compare, only while the panel is open.
        private void Update()
        {
            if (stream == null) return;
            RefreshEnabled();
            RefreshTrack();
            if (_count == null) return;
            string text = stream._numPts.ToString();
            if (_count.text != text) _count.text = text;
        }
    }
}
