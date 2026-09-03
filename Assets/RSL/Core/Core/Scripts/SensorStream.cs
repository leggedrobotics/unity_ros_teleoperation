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

        public void RefreshTopics()
        {
            _ros?.GetTopicAndTypeList(UpdateTopics);
        }

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

        /// <summary>
        /// Updates the dropdown list of topics based on the available topics from ROS
        /// </summary>
        protected virtual void UpdateTopics(Dictionary<string, string> topics)
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
                if (topic.Value == _msgType)
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
                topicName = sensorData.topicName;
                _trackingState = sensorData.trackingState;
                OnTopicChange(topicName);
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

