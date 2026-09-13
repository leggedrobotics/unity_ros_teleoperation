using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using Unity.VisualScripting;


namespace RSL.Core
{
    #if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(SensorStream))]
    public abstract class SensorStreamEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SensorStream myScript = (SensorStream)target;
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
        protected ROSConnection _ros;

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

        /// <summary>
        /// Raised when the topic list is refreshed, with the options offered.
        /// </summary>
        // The seam the UI Toolkit viewer panels listen on. UpdateTopics used to
        // write straight into the uGUI dropdown, so there was no way for another
        // UI to learn what the topics were -- short of reading them back out of
        // the widget it was replacing.
        public event System.Action<List<string>> TopicsChanged;

        /// <summary>
        /// Raises TopicsChanged. A C# event can only be invoked from inside its
        /// declaring type, so a subclass with its own UpdateTopics override
        /// (ImageView, StereoStreamer -- their topic filtering differs enough
        /// from the base that they replace it rather than extend it) calls this
        /// instead of `TopicsChanged?.Invoke(...)` directly.
        /// </summary>
        protected void RaiseTopicsChanged(List<string> topics) => TopicsChanged?.Invoke(topics);

        public void RefreshTopics()
        {
            _ros?.GetTopicAndTypeList(UpdateTopics);
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
            // Null-guarded so a viewer with no uGUI dropdown at all still
            // refreshes. The ported viewers deactivate their old panel rather
            // than deleting it, so the reference survives there -- this is for
            // whatever is built without one.
            if (topicDropdown != null)
            {
                topicDropdown.ClearOptions();
                topicDropdown.AddOptions(options);
                topicDropdown.value = Mathf.Min(_lastSelected, options.Count - 1);
            }

            TopicsChanged?.Invoke(options);
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
