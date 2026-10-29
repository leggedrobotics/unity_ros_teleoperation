using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UvgRos;
using Unity.VisualScripting;


namespace RSL.Core
{
    #if UNITY_EDITOR
    using UnityEditor;
    // NOTE: editorForChildClasses:true (a 2nd `true` arg here) looks like it
    // should make this the fallback editor for any SensorStream subclass
    // without its own -- in practice it did NOT take effect for GridMapStream
    // (confirmed: SensorStreamEditor.OnInspectorGUI never ran for it, per the
    // Editor log, while it does run for MarkerStream/ServiceStream, which
    // explicitly inherit this class below). So every subclass needs its own
    // explicit `[CustomEditor(typeof(X))] class XEditor : SensorStreamEditor {}`
    // (see GridMapStream.cs, PathStream.cs, StampedPoseStream.cs,
    // CameraOverlay.cs for empty-body examples that just inherit this as-is).
    [CustomEditor(typeof(SensorStream))]
    public class SensorStreamEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SensorStream myScript = (SensorStream)target;
            if (GUILayout.Button("Refresh Topics"))
            {
                myScript.RefreshTopics();
            }
            // For a topic that won't show up in the dropdown (e.g. a
            // synthetic test route with no real ROS graph registration
            // behind it) -- edit topicName above directly, then use this.
            if (GUILayout.Button("Subscribe to topicName"))
            {
                myScript.Subscribe();
            }
            if (GUILayout.Button("Clear"))
            {
                myScript.Clear();
            }
        }
    }
    #endif

    public abstract class SensorStream : MonoBehaviour
    {
        public Dropdown topicDropdown;

        public string topicName;
        protected int _trackingState = 0; 
        protected int _lastSelected = 0;
        protected UvgRosConnection _ros;

        /// <summary>
        /// Message type for this sensor, needs to be set to use the automatic topic refresh
        /// </summary>
        protected string _msgType = "";

        /// <summary>
        /// Reference back to the manager that spawned this sensor
        /// </summary>
        public SensorManager manager;

        /// <summary>
        /// Sets what tracking mode we should be in, changes based on the sensor
        /// </summary>
        public abstract void ToggleTrack(int mode);

        /// <summary>
        /// Sets up the sensor stream, subscribing to topics, etc.
        /// </summary>
        public abstract void OnTopicChange(string newTopic);

        // Matches Python's --lazy-refresh-interval cadence (node.py) -- a
        // topic that appears later (the robot's driver still booting) is
        // picked up within this window with no manual action, instead of
        // needing the dropdown reopened by hand.
        private const float AutoRefreshIntervalS = 5f;
        private bool _autoRefreshStarted;

        public void RefreshTopics()
        {
            _ros?.GetTopicAndTypeList(UpdateTopics);
            // Started lazily on first call (every subclass's own Start()
            // already calls RefreshTopics() once) rather than from this
            // base class's own Start(), which a subclass Start() that does
            // not call base.Start() would otherwise silently skip -- see
            // this method's own call sites for why that matters here.
            if (!_autoRefreshStarted)
            {
                _autoRefreshStarted = true;
                InvokeRepeating(nameof(AutoRefreshTopics), AutoRefreshIntervalS, AutoRefreshIntervalS);
            }
        }

        private void AutoRefreshTopics() => _ros?.GetTopicAndTypeList(UpdateTopics);

        /// <summary>
        /// Re-subscribes using whatever topic name is currently sitting in
        /// topicName -- e.g. after typing a new one into the Inspector's
        /// default-drawn field, which alone doesn't trigger anything (it's
        /// a plain field, not a dropdown onValueChanged). Wired to the base
        /// SensorStreamEditor's "Subscribe to topicName" button; several
        /// subclass editors (LidarStreamEditor, ImageViewEditor) also expose
        /// this same call directly.
        /// </summary>
        public void Subscribe()
        {
            OnTopicChange(topicName);
        }

        /// <summary>A route this client can actually decode -- the plain
        /// "chain"/"msgpack_list" framings UvgRosConnection.Subscribe&lt;T&gt;
        /// always supported, plus "encoded_video" now that
        /// UvgRosVideoRouteWorker decodes H.264/H.265 into a real
        /// sensor_msgs/Image (see UvgRosConnection.Subscribe&lt;T&gt;'s video
        /// branch -- ImageView.OnTopicChange is the caller that knows to
        /// retry with Subscribe&lt;ImageMsg&gt; for this framing specifically).
        /// Checking this ahead of time keeps a topic-refresh dropdown from
        /// offering a route that would only fail once actually
        /// selected -- still relevant for anything else this client cannot
        /// decode at all (a bare "cloudini"/"draco_mesh" PointCloud2 chain
        /// this host's native plugins don't cover, say).</summary>
        protected static bool IsViewable(UvgRos.TopicListEntry entry)
        {
            return entry.Framing == "chain" || entry.Framing == "msgpack_list" ||
                   entry.Framing == "encoded_video" || entry.Framing == "encoded_video_synced";
        }

        /// <summary>
        /// Updates the dropdown list of topics based on the available topics from ROS
        /// </summary>
        protected virtual void UpdateTopics(Dictionary<string, UvgRos.TopicListEntry> topics)
        {
            if (_msgType == "")
            {
                Debug.LogWarning("Message type not set for sensor, cannot update topics");
                return;
            }

            List<string> options = new List<string>();
            options.Add("None");
            foreach (var topic in topics)
            {
                if (topic.Value.MsgType == _msgType && IsViewable(topic.Value))
                {
                    options.Add(topic.Key);
                    Debug.Log($"Found topic {topic.Key} for {_msgType}");
                }
            }

            if (options.Count == 1)
            {
                Debug.LogWarning($"No topics available for {_msgType}");
            }
            topicDropdown.ClearOptions();
            topicDropdown.AddOptions(options);
            topicDropdown.value = Mathf.Min(_lastSelected, options.Count - 1);
        }

        /// <summary>
        /// Converts the state of this sensor into a string so that it can be reinitialized later
        /// </summary>
        public virtual string Serialize()
        {
            ISensorData data = new ISensorData();
            data.position = transform.position;
            data.rotation = transform.rotation;
            data.scale = transform.localScale;
            data.topicName = topicName;
            data.trackingState = _trackingState;

            return JsonUtility.ToJson(data);
        }

        /// <summary>
        /// Converts a string into the state of this sensor
        /// </summary>
        public virtual void Deserialize(string data)
        {
            try
            {
                ISensorData sensorData = JsonUtility.FromJson<ISensorData>(data);
                transform.position = sensorData.position;
                transform.rotation = sensorData.rotation;
                transform.localScale = sensorData.scale;
                _trackingState = sensorData.trackingState;
                // Do NOT set topicName here -- every OnTopicChange override
                // unsubscribes whatever topic is CURRENTLY in topicName
                // before subscribing the new one; setting it to the
                // incoming topic first leaves nothing for that step to
                // find, so the previous topic's route never gets released
                // (it looks like unsubscribing the new topic, a no-op,
                // instead of the old one).
                OnTopicChange(sensorData.topicName);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to deserialize sensor data: " + e.Message);
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// Clears the sensor stream, removing it from the manager
        /// </summary>
        public void Clear()
        {
            manager.Remove(gameObject);
        }
    }
}

