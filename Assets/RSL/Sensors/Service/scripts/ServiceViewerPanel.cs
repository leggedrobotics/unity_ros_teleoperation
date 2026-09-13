// UI Toolkit controller for SensorServiceViewer.uxml -- the port of a spawned
// service button's uGUI TopMenu.
//
// Every control routes through the ServiceStream methods the uGUI panel's
// UnityEvents called, so both UIs drive the same code while the migration is in
// flight: Subscribe -> SubscribeToService, Clear -> SensorStream.Clear.
//
// The TRIGGER button is not here. It lives on the viewer root rather than in
// TopMenu -- it is the big face of the thing you press to call the service, not
// part of the settings panel -- and porting it would move it.
//
// The title bar is not here either: it carries UIPositionChanger over a
// BoxCollider and is how the viewer is grabbed and moved in the world.
using UnityEngine;
using UnityEngine.UIElements;
using RSL.Core.Menu;

namespace RSL.Sensors.Service
{
    public class ServiceViewerPanel : UIToolkitPanel
    {
        [Tooltip("The stream this panel drives. Assigned on the prefab, since a " +
                 "spawned viewer has exactly one and a scene search would find " +
                 "every other viewer's as well.")]
        public ServiceStream stream;

        private TextField _topic;

        protected override void Bind(VisualElement root)
        {
            // The panel is a child of the viewer, so the stream is up the
            // hierarchy -- fall back to that rather than requiring the
            // serialized reference to survive every prefab edit.
            if (stream == null) stream = GetComponentInParent<ServiceStream>();

            _topic = Require<TextField>(root, "ServiceTopic");
            var subscribe = Require<Button>(root, "Subscribe");
            var clear = Require<Button>(root, "Clear");

            if (!BindSection(stream != null, root)) return;

            if (_topic != null)
            {
                _topic.SetValueWithoutNotify(stream.topicName);
                // Commit on Enter as well as through Subscribe: the uGUI input
                // field had an onEndEdit that did the same.
                _topic.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) Subscribe();
                });
            }

            if (subscribe != null) subscribe.clicked += Subscribe;
            if (clear != null) clear.clicked += () => stream.Clear();
        }

        private void Subscribe()
        {
            string topic = _topic != null ? _topic.value?.Trim() : null;
            if (string.IsNullOrEmpty(topic))
            {
                // An empty topic would register a service on "", which fails
                // silently. Put the live value back rather than accept it.
                _topic?.SetValueWithoutNotify(stream.topicName);
                return;
            }
            // OnTopicChange, not a field write: it re-registers the ROS service
            // and updates the title bar, which an assignment would skip.
            stream.OnTopicChange(topic);
        }
    }
}
