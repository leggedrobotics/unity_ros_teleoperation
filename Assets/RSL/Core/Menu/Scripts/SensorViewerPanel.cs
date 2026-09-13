// Shared controller for a spawned sensor viewer's control panel.
//
// Every viewer except the service button is the same shape: a topic dropdown, a
// Refresh that re-scans topics, a Clear that removes the viewer, and usually a
// count. Only the middle differs -- one size slider here, two sliders and two
// enum dropdowns there -- so that part is left to BindExtra and the rest lives
// once.
//
// Controls route through the SensorStream methods the uGUI panels' UnityEvents
// called, so both UIs drive the same code while the migration is in flight.
//
// TOPIC SELECTION calls OnTopicChange(string), not the OnTopicSelect(int) each
// stream also offers: those read the caption back out of the uGUI dropdown this
// replaces, so they cannot work once it is gone. OnTopicChange is what they
// call anyway.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using RSL.Core;

namespace RSL.Core.Menu
{
    public abstract class SensorViewerPanel : UIToolkitPanel
    {
        [Tooltip("The stream this panel drives. Assigned on the prefab, since a " +
                 "spawned viewer has exactly one and a scene search would find " +
                 "every other viewer's as well.")]
        public SensorStream stream;

        private DropdownField _topic;
        private Label _count;
        private List<string> _topics = new List<string> { "None" };

        /// <summary>
        /// Text for the panel's Count label, or null when this sensor keeps no
        /// count -- in which case the label is hidden rather than left at zero.
        /// </summary>
        protected virtual string CountText => null;

        /// <summary>Binds the controls specific to one kind of viewer.</summary>
        protected virtual void BindExtra(VisualElement root) { }

        protected override void Bind(VisualElement root)
        {
            // The panel is a child of the viewer, so the stream is up the
            // hierarchy -- fall back to that rather than requiring the
            // serialized reference to survive every prefab edit.
            if (stream == null) stream = GetComponentInParent<SensorStream>();

            _topic = Require<DropdownField>(root, "TopicDropdown");
            _count = root.Q<Label>("Count");
            var refresh = Require<Button>(root, "Refresh");
            var clear = Require<Button>(root, "Clear");

            if (!BindSection(stream != null, root)) return;

            if (refresh != null) refresh.clicked += () => stream.RefreshTopics();
            if (clear != null) clear.clicked += () => stream.Clear();

            BindTopic();
            RefreshCount();
            BindExtra(root);
        }

        protected virtual void OnDisable()
        {
            if (stream != null) stream.TopicsChanged -= OnTopicsChanged;
        }

        private void BindTopic()
        {
            // Subscribed rather than polled: the list arrives asynchronously
            // from a ROS round trip, long after this panel has bound.
            stream.TopicsChanged -= OnTopicsChanged;
            stream.TopicsChanged += OnTopicsChanged;

            if (_topic != null)
            {
                ApplyTopics(_topics);
                _topic.RegisterValueChangedCallback(evt =>
                {
                    // "None" is the list's own placeholder for "not subscribed",
                    // which OnTopicChange spells as null.
                    string chosen = evt.newValue;
                    stream.OnTopicChange(chosen == "None" ? null : chosen);
                });
            }

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
            // Without notify: this reports what the stream is already on, and
            // notifying would re-subscribe on every refresh.
            string current = string.IsNullOrEmpty(stream.topicName) ? "None" : stream.topicName;
            _topic.SetValueWithoutNotify(topics.Contains(current) ? current : "None");
        }

        // Counts are pushed by the streams straight into a TMP label they own,
        // with no event to hang off, so this mirrors them. Cheap: a string
        // compare, and only while the panel is open.
        protected virtual void Update()
        {
            RefreshCount();
        }

        private void RefreshCount()
        {
            if (_count == null || stream == null) return;
            string text = CountText;
            if (text == null)
            {
                _count.style.display = DisplayStyle.None;
                return;
            }
            if (_count.text != text) _count.text = text;
        }
    }
}
